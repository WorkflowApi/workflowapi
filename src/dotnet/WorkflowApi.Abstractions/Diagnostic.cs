namespace WorkflowApi.Abstractions;

public sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Target);
