using VerifyXunit;
using WorkflowApi.Abstractions;
using WorkflowApi.AspNetCore.Serialization;

namespace WorkflowApi.AspNetCore.Tests.Serialization;

public class WorkflowApiJsonWriterTests
{
    [Fact]
    public void Writes_minimal_document()
    {
        var doc = new WorkflowApiDocument(
            WorkflowApi: "0.1.0",
            Info: null,
            Host: new HostDef("my-service", "My Service"),
            Bindings: new BindingsDef(null),
            Workflows: new Dictionary<string, WorkflowDef>
            {
                ["MyWorkflow"] = new(
                    Name: "MyWorkflow",
                    DisplayName: null,
                    Run: new OperationDef("runMyWorkflow", null, null, null, null),
                    Signals: new Dictionary<string, OperationDef>(),
                    Queries: new Dictionary<string, OperationDef>(),
                    Updates: new Dictionary<string, OperationDef>(),
                    Bindings: null),
            },
            Activities: null,
            Bridges: null,
            Diagnostics: []);

        var json = Write(doc);

        const string expected = """
            {
              "workflowApi": "0.1.0",
              "host": {
                "id": "my-service",
                "name": "My Service"
              },
              "workflows": {
                "MyWorkflow": {
                  "name": "MyWorkflow",
                  "run": {
                    "operationId": "runMyWorkflow"
                  }
                }
              }
            }
            """;

        Assert.Equal(expected.ReplaceLineEndings("\n"), json.ReplaceLineEndings("\n"));
    }

    [Fact]
    public Task Writes_full_document_matches_snapshot()
    {
        var doc = CreateFullDocument();
        var json = Write(doc);

        return Verifier.Verify(json);
    }

    [Fact]
    public void Output_is_deterministic()
    {
        var doc = CreateFullDocument();

        var first = Write(doc);
        var second = Write(doc);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Omits_null_and_empty()
    {
        var doc = new WorkflowApiDocument(
            WorkflowApi: "0.1.0",
            Info: null,
            Host: new HostDef("worker", null),
            Bindings: new BindingsDef(null),
            Workflows: new Dictionary<string, WorkflowDef>
            {
                ["WorkflowB"] = new(
                    Name: "WorkflowB",
                    DisplayName: null,
                    Run: new OperationDef("runB", null, null, null, null),
                    Signals: new Dictionary<string, OperationDef>(),
                    Queries: new Dictionary<string, OperationDef>(),
                    Updates: new Dictionary<string, OperationDef>(),
                    Bindings: null),
            },
            Activities: new Dictionary<string, ActivityDef>(),
            Bridges: new Dictionary<string, BridgeDef>(),
            Diagnostics: []);

        var json = Write(doc);

        Assert.DoesNotContain("\"info\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"bindings\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"activities\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"bridges\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"diagnostics\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"displayName\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"summary\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"$ref\"", json, StringComparison.Ordinal);
    }

    private static WorkflowApiDocument CreateFullDocument()
    {
        return new WorkflowApiDocument(
            WorkflowApi: "0.1.0",
            Info: new InfoDef(
                Title: "Workflow API",
                Version: "1.2.3",
                Summary: "Document summary",
                Description: "Document description"),
            Host: new HostDef("worker-host", "Worker Host"),
            Bindings: new BindingsDef(
                new TemporalBindingDef(
                    Namespace: "payments",
                    TaskQueue: "payments-queue",
                    WorkerHost: "payments-worker",
                    Sdk: "dotnet")),
            Workflows: new Dictionary<string, WorkflowDef>
            {
                ["zetaWorkflow"] = new(
                    Name: "ZetaWorkflow",
                    DisplayName: "Zeta Workflow",
                    Run: new OperationDef(
                        OperationId: "runZeta",
                        Summary: "Start zeta workflow",
                        Input: new SchemaRefDef("#/components/schemas/ZetaInput"),
                        Output: new SchemaRefDef("#/components/schemas/ZetaOutput"),
                        Bindings: new BindingsDef(
                            new TemporalBindingDef("payments", "zeta-queue", null, "dotnet"))),
                    Signals: new Dictionary<string, OperationDef>
                    {
                        ["resume"] = new(
                            "resumeZeta",
                            "Resume zeta workflow",
                            new SchemaRefDef("#/components/schemas/ResumeInput"),
                            null,
                            null),
                        ["cancel"] = new(
                            "cancelZeta",
                            "Cancel zeta workflow",
                            new SchemaRefDef("#/components/schemas/CancelInput"),
                            null,
                            null),
                    },
                    Queries: new Dictionary<string, OperationDef>
                    {
                        ["status"] = new(
                            "statusZeta",
                            "Get zeta status",
                            null,
                            new SchemaRefDef("#/components/schemas/StatusOutput"),
                            null),
                    },
                    Updates: new Dictionary<string, OperationDef>
                    {
                        ["priority"] = new(
                            "updateZetaPriority",
                            "Update zeta priority",
                            new SchemaRefDef("#/components/schemas/PriorityInput"),
                            new SchemaRefDef("#/components/schemas/PriorityOutput"),
                            null),
                    },
                    Bindings: new BindingsDef(
                        new TemporalBindingDef("payments", "workflow-queue", "workflow-host", "dotnet"))),
                ["alphaWorkflow"] = new(
                    Name: "AlphaWorkflow",
                    DisplayName: null,
                    Run: new OperationDef(
                        OperationId: "runAlpha",
                        Summary: null,
                        Input: null,
                        Output: null,
                        Bindings: null),
                    Signals: new Dictionary<string, OperationDef>(),
                    Queries: new Dictionary<string, OperationDef>(),
                    Updates: new Dictionary<string, OperationDef>(),
                    Bindings: null),
            },
            Activities: new Dictionary<string, ActivityDef>
            {
                ["sendEmail"] = new("send-email", "Send Email", "Send an email message", "notifications", "public"),
                ["archive"] = new("archive", null, null, null, null),
            },
            Bridges: new Dictionary<string, BridgeDef>
            {
                ["orders"] = new("orders", "Orders Bridge", "nexus", "Bridge summary", ["run", "status"]),
                ["billing"] = new("billing", null, "http", null, ["quote"]),
            },
            Diagnostics:
            [
                new Diagnostic(DiagnosticSeverity.Warning, "WFA001", "A warning", "workflows.zetaWorkflow"),
                new Diagnostic(DiagnosticSeverity.Error, "WFA002", "An error", null),
            ]);
    }

    private static string Write(WorkflowApiDocument document) =>
        WorkflowApiJsonWriter.Write(document);
}
