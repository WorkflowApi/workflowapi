using WorkflowApi.Abstractions;
using WorkflowApi.Temporal.Hosting;
using WorkflowApi.Temporal.Scanning;

namespace WorkflowApi.Temporal.Mapping;

/// <summary>
/// Joins scanner output with DI-registered worker options to produce a <see cref="WorkerMapping"/>,
/// including orphan diagnostics for types that were scanned but never registered, or registered
/// but never found by the scanner.
/// </summary>
public sealed class WorkerMappingBuilder
{
    public WorkerMapping Build(
        TemporalScanResult scanResult,
        IReadOnlyList<RegisteredWorker> registeredWorkers)
    {
        ArgumentNullException.ThrowIfNull(scanResult);
        ArgumentNullException.ThrowIfNull(registeredWorkers);

        // Index scanned types for O(1) lookup.
        // Workflows: one entry per class.
        var scannedWorkflowsByType = scanResult.Workflows
            .ToDictionary(w => w.WorkflowType);

        // Activities: many-per-class (one per [Activity] method).
        // Build a lookup from type → all ScannedActivity entries.
        var scannedActivitiesByType = scanResult.Activities
            .GroupBy(a => a.ActivityType)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build MappedWorkers — sorted by TaskQueue ordinal (spec requirement).
        var mappedWorkers = registeredWorkers
            .OrderBy(w => w.TaskQueue, StringComparer.Ordinal)
            .Select(worker =>
            {
                var workflows = worker.WorkflowTypes
                    .Select(t => scannedWorkflowsByType.GetValueOrDefault(t))
                    .Where(w => w is not null)
                    .Select(w => w!)
                    .OrderBy(w => w.Name, StringComparer.Ordinal)
                    .ToList();

                var activities = worker.ActivityTypes
                    .SelectMany(t => scannedActivitiesByType.TryGetValue(t, out var list) ? list : Enumerable.Empty<ScannedActivity>())
                    .OrderBy(a => a.Id, StringComparer.Ordinal)
                    .ToList();

                var registeredWorkflowNames = worker.WorkflowTypes
                    .Select(t => t.FullName)
                    .OfType<string>()
                    .ToHashSet(StringComparer.Ordinal);

                var registeredActivityNames = worker.ActivityTypes
                    .Select(t => t.FullName)
                    .OfType<string>()
                    .ToHashSet(StringComparer.Ordinal);

                return new MappedWorker(
                    worker.TaskQueue,
                    worker.Namespace,
                    workflows,
                    activities,
                    registeredWorkflowNames,
                    registeredActivityNames);
            })
            .ToList();

        // Compute diagnostics.
        var allRegisteredWorkflowTypes = registeredWorkers
            .SelectMany(w => w.WorkflowTypes)
            .ToHashSet();

        var allRegisteredActivityTypes = registeredWorkers
            .SelectMany(w => w.ActivityTypes)
            .ToHashSet();

        var diagnostics = new List<Diagnostic>();

        // WF001: scanned but not registered with any worker.
        foreach (var wf in scanResult.Workflows)
        {
            if (!allRegisteredWorkflowTypes.Contains(wf.WorkflowType))
            {
                var target = wf.WorkflowType.FullName ?? wf.WorkflowType.Name;
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Warning,
                    "WF001",
                    $"Workflow type '{target}' is declared with [Workflow] but not registered with any AddHostedTemporalWorker(...). It will never execute.",
                    target));
            }
        }

        // ACT001: scanned activity types not registered with any worker.
        foreach (var actType in scannedActivitiesByType.Keys)
        {
            if (!allRegisteredActivityTypes.Contains(actType))
            {
                var target = actType.FullName ?? actType.Name;
                diagnostics.Add(new Diagnostic(
                    DiagnosticSeverity.Warning,
                    "ACT001",
                    $"Activity type '{target}' is declared with [Activity] but not registered with any AddHostedTemporalWorker(...). It will never execute.",
                    target));
            }
        }

        // WF002: registered but not found by the scanner.
        foreach (var worker in registeredWorkers)
        {
            foreach (var t in worker.WorkflowTypes)
            {
                if (!scannedWorkflowsByType.ContainsKey(t))
                {
                    var target = t.FullName ?? t.Name;
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Warning,
                        "WF002",
                        $"Workflow type '{target}' is registered with worker '{worker.TaskQueue}' but no [Workflow] attribute was found by the scanner. It may have been excluded or live in an unscanned assembly.",
                        target));
                }
            }
        }

        // ACT002: registered activity type not found by the scanner.
        foreach (var worker in registeredWorkers)
        {
            foreach (var t in worker.ActivityTypes)
            {
                if (!scannedActivitiesByType.ContainsKey(t))
                {
                    var target = t.FullName ?? t.Name;
                    diagnostics.Add(new Diagnostic(
                        DiagnosticSeverity.Warning,
                        "ACT002",
                        $"Activity type '{target}' is registered with worker '{worker.TaskQueue}' but no [Activity] attribute was found by the scanner. It may have been excluded or live in an unscanned assembly.",
                        target));
                }
            }
        }

        // Deduplicate WF002/ACT002 (a type registered with multiple workers generates one diagnostic per registration — dedupe by Code+Target).
        var dedupedDiagnostics = diagnostics
            .DistinctBy(d => (d.Code, d.Target))
            .OrderBy(d => d.Code, StringComparer.Ordinal)
            .ThenBy(d => d.Target, StringComparer.Ordinal)
            .ToList();

        return new WorkerMapping(mappedWorkers, dedupedDiagnostics);
    }
}

/// <summary>
/// The result of mapping scanner output against registered worker options.
/// </summary>
public sealed record WorkerMapping(
    IReadOnlyList<MappedWorker> Workers,
    IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// A single registered Temporal worker with its intersection of scanned types.
/// </summary>
public sealed record MappedWorker(
    string TaskQueue,
    string? Namespace,
    IReadOnlyList<ScannedWorkflow> Workflows,
    IReadOnlyList<ScannedActivity> Activities,
    IReadOnlySet<string> RegisteredWorkflowTypeFullNames,
    IReadOnlySet<string> RegisteredActivityTypeFullNames);
