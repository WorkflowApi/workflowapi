namespace WorkflowApi.Abstractions;

public sealed record InfoDef(
    string Title,
    string Version,
    string? Summary,
    string? Description);
