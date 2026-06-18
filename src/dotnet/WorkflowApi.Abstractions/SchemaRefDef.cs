namespace WorkflowApi.Abstractions;

/// <summary>Serializes as <c>$ref</c> in JSON/YAML output (handled by serializer in Task 4/5).</summary>
public sealed record SchemaRefDef(string Ref);
