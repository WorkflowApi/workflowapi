using WorkflowApi.Abstractions;
using WorkflowApi.Temporal.Mapping;
using WorkflowApi.Temporal.Scanning;
using WorkflowApi.Temporal.Topology;

namespace WorkflowApi.Temporal.Documents;

/// <summary>
/// Converts a <see cref="WorkerMapping"/> into one <see cref="WorkflowApiDocument"/> per registered worker.
/// </summary>
public sealed class DocumentFactory
{
    private readonly WorkflowTopologyExtractor _topology = new();

    public IReadOnlyList<WorkflowApiDocument> Build(WorkerMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        return mapping.Workers
            .Select(worker => BuildDocument(worker, mapping.Diagnostics))
            .ToList();
    }

    private WorkflowApiDocument BuildDocument(
        MappedWorker worker,
        IReadOnlyList<Diagnostic> allDiagnostics)
    {
        var ownedFullNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in worker.RegisteredWorkflowTypeFullNames) ownedFullNames.Add(n);
        foreach (var n in worker.RegisteredActivityTypeFullNames) ownedFullNames.Add(n);

        var diagnostics = allDiagnostics
            .Where(d => d.Target is not null && ownedFullNames.Contains(d.Target))
            .ToList();

        var workflows = worker.Workflows
            .OrderBy(w => w.Name, StringComparer.Ordinal)
            .ToDictionary(
                w => w.Name,
                BuildWorkflowDef,
                StringComparer.Ordinal);

        IReadOnlyDictionary<string, ActivityDef>? activities = worker.Activities.Count > 0
            ? worker.Activities
                .OrderBy(a => a.Id, StringComparer.Ordinal)
                .ToDictionary(a => a.Id, a => new ActivityDef(a.Id, Title: DisplayNameUtil.Humanise(a.Id), null, null, null), StringComparer.Ordinal)
            : null;

        var info = new InfoDef(
            Title: $"{worker.TaskQueue} Workflow API",
            Version: "0.1.0",
            Summary: $"Generated from .NET Temporal worker registered on task queue '{worker.TaskQueue}'.",
            Description: null);

        return new WorkflowApiDocument(
            WorkflowApi: "0.1.0",
            Info: info,
            Host: new HostDef(Id: worker.TaskQueue, Name: worker.TaskQueue),
            Bindings: new BindingsDef(
                Temporal: new TemporalBindingDef(
                    Namespace: worker.Namespace ?? "default",
                    TaskQueue: worker.TaskQueue,
                    WorkerHost: null,
                    Sdk: "temporal-dotnet")),
            Workflows: workflows,
            Activities: activities,
            Bridges: null,
            Diagnostics: diagnostics.Count > 0 ? diagnostics : []);
    }

    private WorkflowDef BuildWorkflowDef(ScannedWorkflow sw)
    {
        if (sw.Run is null)
            throw new InvalidOperationException($"Workflow '{sw.Name}' has no [WorkflowRun] method.");

        var run = new OperationDef(sw.Run.OperationId, null, null, null, null);

        var signals = sw.Signals
            .OrderBy(op => op.OperationId, StringComparer.Ordinal)
            .ToDictionary(op => op.OperationId, op => new OperationDef(op.OperationId, null, null, null, null), StringComparer.Ordinal);

        var queries = sw.Queries
            .OrderBy(op => op.OperationId, StringComparer.Ordinal)
            .ToDictionary(op => op.OperationId, op => new OperationDef(op.OperationId, null, null, null, null), StringComparer.Ordinal);

        var updates = sw.Updates
            .OrderBy(op => op.OperationId, StringComparer.Ordinal)
            .ToDictionary(op => op.OperationId, op => new OperationDef(op.OperationId, null, null, null, null), StringComparer.Ordinal);

        var topology = _topology.Extract(sw.WorkflowType);

        return new WorkflowDef(
            Name: sw.Name,
            DisplayName: DisplayNameUtil.Humanise(sw.Name),
            Summary: null,
            Run: run,
            Signals: signals,
            Queries: queries,
            Updates: updates,
            Topology: topology,
            Bindings: null);
    }
}
