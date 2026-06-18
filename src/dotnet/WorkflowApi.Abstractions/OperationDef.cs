namespace WorkflowApi.Abstractions;

public sealed record OperationDef(
    string OperationId,
    string? Summary,
    SchemaRefDef? Input,
    SchemaRefDef? Output,
    BindingsDef? Bindings);
