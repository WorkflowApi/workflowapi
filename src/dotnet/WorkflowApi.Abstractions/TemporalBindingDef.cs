namespace WorkflowApi.Abstractions;

public sealed record TemporalBindingDef(
    string Namespace,
    string TaskQueue,
    string? WorkerHost,
    string? Sdk);
