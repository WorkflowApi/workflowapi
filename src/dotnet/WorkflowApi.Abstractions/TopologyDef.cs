namespace WorkflowApi.Abstractions;

public sealed record TopologyDef(
    IReadOnlyDictionary<string, TopologyNodeDef> Nodes,
    IReadOnlyList<TopologyEdgeDef> Edges);
