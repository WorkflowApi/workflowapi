namespace WorkflowApi.Abstractions;

public sealed record TopologyNodeDef(
    string Kind,
    string? DisplayName,
    string? Summary,
    string? ActivityRef,
    string? WorkflowRef);
