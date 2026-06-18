using System.Reflection;
using Temporalio.Activities;
using Temporalio.Workflows;

namespace WorkflowApi.Temporal.Scanning;

/// <summary>
/// Scans assemblies for types decorated with Temporalio workflow and activity attributes.
/// </summary>
public sealed class TemporalAttributeScanner
{
    /// <summary>
    /// Scans the given assemblies and returns all discovered workflows and activities.
    /// </summary>
    public TemporalScanResult Scan(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return Scan(assemblies.SelectMany(a =>
        {
            try { return a.GetExportedTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null).Cast<Type>(); }
            catch { return []; }
        }));
    }

    /// <summary>
    /// Scans the given types directly and returns all discovered workflows and activities.
    /// </summary>
    public TemporalScanResult Scan(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        var workflows = new List<ScannedWorkflow>();
        var activities = new List<ScannedActivity>();

        foreach (var type in types)
        {
            try
            {
                var workflowAttr = type.GetCustomAttribute<WorkflowAttribute>();
                if (workflowAttr is not null)
                    workflows.Add(BuildScannedWorkflow(type, workflowAttr));

                // Activities: scan all public types for methods with [Activity]
                var activityMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(m => (Method: m, Attr: m.GetCustomAttribute<ActivityAttribute>()))
                    .Where(x => x.Attr is not null);

                foreach (var (method, attr) in activityMethods)
                {
                    var id = ValidateId(
                        NotEmpty(attr!.Name) ?? StripAsync(method.Name),
                        "Activity",
                        $"{type.FullName}.{method.Name}");
                    activities.Add(new ScannedActivity(type, method, id));
                }
            }
            catch (InvalidOperationException) { throw; }
            catch { continue; }
        }

        return new TemporalScanResult(
            workflows.OrderBy(w => w.WorkflowType.FullName, StringComparer.Ordinal).ToList(),
            activities.OrderBy(a => a.Id, StringComparer.Ordinal).ToList());
    }

    private static ScannedWorkflow BuildScannedWorkflow(Type type, WorkflowAttribute attr)
    {
        var name = ValidateId(NotEmpty(attr.Name) ?? type.Name, "Workflow", type.FullName ?? type.Name);

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        ScannedOperation? run = null;
        var signals = new List<ScannedOperation>();
        var queries = new List<ScannedOperation>();
        var updates = new List<ScannedOperation>();

        // Sort methods alphabetically for deterministic first-pick of [WorkflowRun]
        foreach (var method in methods.OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            if (method.GetCustomAttribute<WorkflowRunAttribute>() is not null)
            {
                if (run is null)
                {
                    var opId = ValidateId(StripAsync(method.Name), "Operation", $"{type.FullName}.{method.Name}");
                    run = new ScannedOperation(method, opId);
                }
            }
            else if (method.GetCustomAttribute<WorkflowSignalAttribute>() is { } signalAttr)
            {
                var opId = ValidateId(NotEmpty(signalAttr.Name) ?? StripAsync(method.Name), "Operation", $"{type.FullName}.{method.Name}");
                signals.Add(new ScannedOperation(method, opId));
            }
            else if (method.GetCustomAttribute<WorkflowQueryAttribute>() is { } queryAttr)
            {
                var opId = ValidateId(NotEmpty(queryAttr.Name) ?? StripAsync(method.Name), "Operation", $"{type.FullName}.{method.Name}");
                queries.Add(new ScannedOperation(method, opId));
            }
            else if (method.GetCustomAttribute<WorkflowUpdateAttribute>() is { } updateAttr)
            {
                var opId = ValidateId(NotEmpty(updateAttr.Name) ?? StripAsync(method.Name), "Operation", $"{type.FullName}.{method.Name}");
                updates.Add(new ScannedOperation(method, opId));
            }
        }

        return new ScannedWorkflow(
            type,
            name,
            run,
            signals.OrderBy(s => s.OperationId, StringComparer.Ordinal).ToList(),
            queries.OrderBy(q => q.OperationId, StringComparer.Ordinal).ToList(),
            updates.OrderBy(u => u.OperationId, StringComparer.Ordinal).ToList());
    }

    private static string ValidateId(string id, string kind, string contextDisplay)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException($"{kind} on {contextDisplay} has an empty name.");
        foreach (var c in id)
        {
            if (char.IsWhiteSpace(c) || c is '/' or '$' or ':' or '#' or '?' or '&')
                throw new InvalidOperationException(
                    $"{kind} on {contextDisplay} has invalid name '{id}': contains reserved character '{c}'. " +
                    $"Names must be safe for use as YAML/JSON map keys and `operationId` values.");
        }
        return id;
    }

    private static string StripAsync(string name) =>
        name.EndsWith("Async", StringComparison.Ordinal) && name.Length > 5
            ? name[..^5]
            : name;

    private static string? NotEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}

/// <summary>
/// Result of scanning assemblies for Temporal types.
/// </summary>
public sealed record TemporalScanResult(
    IReadOnlyList<ScannedWorkflow> Workflows,
    IReadOnlyList<ScannedActivity> Activities);

/// <summary>
/// A workflow type discovered via <see cref="WorkflowAttribute"/>.
/// </summary>
public sealed record ScannedWorkflow(
    Type WorkflowType,
    string Name,
    ScannedOperation? Run,
    IReadOnlyList<ScannedOperation> Signals,
    IReadOnlyList<ScannedOperation> Queries,
    IReadOnlyList<ScannedOperation> Updates);

/// <summary>
/// An activity method discovered via <see cref="ActivityAttribute"/>.
/// </summary>
public sealed record ScannedActivity(
    Type ActivityType,
    MethodInfo Method,
    string Id);

/// <summary>
/// A workflow operation (run/signal/query/update) method.
/// </summary>
public sealed record ScannedOperation(
    MethodInfo Method,
    string OperationId);
