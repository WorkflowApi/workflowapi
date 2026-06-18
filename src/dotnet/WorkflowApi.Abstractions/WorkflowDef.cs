namespace WorkflowApi.Abstractions;

public sealed record WorkflowDef(
    string Name,
    string? DisplayName,
    OperationDef Run,
    IReadOnlyDictionary<string, OperationDef> Signals,
    IReadOnlyDictionary<string, OperationDef> Queries,
    IReadOnlyDictionary<string, OperationDef> Updates,
    BindingsDef? Bindings,
    string? Summary = null,
    TopologyDef? Topology = null);
