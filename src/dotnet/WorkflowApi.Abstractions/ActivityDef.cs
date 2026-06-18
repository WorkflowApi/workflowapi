namespace WorkflowApi.Abstractions;

public sealed record ActivityDef(
    string Id,
    string? Title,
    string? Summary,
    string? Group,
    string? Visibility);
