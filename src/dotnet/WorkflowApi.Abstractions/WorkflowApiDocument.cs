namespace WorkflowApi.Abstractions;

public sealed record WorkflowApiDocument(
    string WorkflowApi,
    InfoDef? Info,
    HostDef Host,
    BindingsDef Bindings,
    IReadOnlyDictionary<string, WorkflowDef> Workflows,
    IReadOnlyDictionary<string, ActivityDef>? Activities,
    IReadOnlyDictionary<string, BridgeDef>? Bridges,
    IReadOnlyList<Diagnostic> Diagnostics);
