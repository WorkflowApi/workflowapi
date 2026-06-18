namespace WorkflowApi.Abstractions;

public sealed record BridgeDef(
    string Id,
    string? DisplayName,
    string Kind,
    string? Summary,
    IReadOnlyList<string> Operations);
