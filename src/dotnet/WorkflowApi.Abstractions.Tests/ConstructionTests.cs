using Xunit;

namespace WorkflowApi.Abstractions.Tests;

public class ConstructionTests
{
    [Fact]
    public void FullyPopulatedWorkflowApiDocument_ConstructsSuccessfully()
    {
        // Build Info
        var info = new InfoDef(
            Title: "Sample Workflow API",
            Version: "1.0.0",
            Summary: "A sample workflow API",
            Description: "This is a sample workflow API for testing");

        // Build Host
        var host = new HostDef(
            Id: "temporal-host-1",
            Name: "Temporal Host");

        // Build Temporal Binding
        var temporalBinding = new TemporalBindingDef(
            Namespace: "default",
            TaskQueue: "my-task-queue",
            WorkerHost: "localhost",
            Sdk: "go");

        // Build Bindings
        var bindings = new BindingsDef(Temporal: temporalBinding);

        // Build operation schemas
        var inputSchema = new SchemaRefDef("RequestSchema");
        var outputSchema = new SchemaRefDef("ResponseSchema");

        // Build Run operation
        var runOp = new OperationDef(
            OperationId: "run",
            Summary: "Run the workflow",
            Input: inputSchema,
            Output: outputSchema,
            Bindings: null);

        // Build a Signal operation
        var signalOp = new OperationDef(
            OperationId: "signal",
            Summary: "Send a signal",
            Input: inputSchema,
            Output: null,
            Bindings: null);

        // Build a Query operation
        var queryOp = new OperationDef(
            OperationId: "query",
            Summary: "Query the workflow state",
            Input: null,
            Output: outputSchema,
            Bindings: null);

        // Build an Update operation
        var updateOp = new OperationDef(
            OperationId: "update",
            Summary: "Update the workflow",
            Input: inputSchema,
            Output: outputSchema,
            Bindings: null);

        // Build Workflow
        var workflow = new WorkflowDef(
            Name: "MyWorkflow",
            DisplayName: "My Sample Workflow",
            Run: runOp,
            Signals: new Dictionary<string, OperationDef> { { "signalName", signalOp } },
            Queries: new Dictionary<string, OperationDef> { { "queryName", queryOp } },
            Updates: new Dictionary<string, OperationDef> { { "updateName", updateOp } },
            Bindings: null);

        // Build collections
        var workflows = new Dictionary<string, WorkflowDef> { { "MyWorkflow", workflow } };
        var activities = new Dictionary<string, ActivityDef>();
        var bridges = new Dictionary<string, BridgeDef>();
        var diagnostics = new List<Diagnostic>();

        // Construct the document
        var doc = new WorkflowApiDocument(
            WorkflowApi: "1.0",
            Info: info,
            Host: host,
            Bindings: bindings,
            Workflows: workflows,
            Activities: activities,
            Bridges: bridges,
            Diagnostics: diagnostics);

        // Assertions
        Assert.NotNull(doc);
        Assert.Equal("1.0", doc.WorkflowApi);
        Assert.NotNull(doc.Info);
        Assert.Equal("Sample Workflow API", doc.Info.Title);
        Assert.Equal("1.0.0", doc.Info.Version);
        Assert.Equal("temporal-host-1", doc.Host.Id);
        Assert.Equal("Temporal Host", doc.Host.Name);
        Assert.NotNull(doc.Bindings);
        Assert.NotNull(doc.Bindings.Temporal);
        Assert.Equal("default", doc.Bindings.Temporal.Namespace);
        Assert.Equal("my-task-queue", doc.Bindings.Temporal.TaskQueue);
        Assert.Single(doc.Workflows);
        Assert.True(doc.Workflows.ContainsKey("MyWorkflow"));
        var retrievedWorkflow = doc.Workflows["MyWorkflow"];
        Assert.Equal("MyWorkflow", retrievedWorkflow.Name);
        Assert.Equal("My Sample Workflow", retrievedWorkflow.DisplayName);
        Assert.NotNull(retrievedWorkflow.Run);
        Assert.Equal("run", retrievedWorkflow.Run.OperationId);
        Assert.Single(retrievedWorkflow.Signals);
        Assert.Single(retrievedWorkflow.Queries);
        Assert.Single(retrievedWorkflow.Updates);
        Assert.NotNull(doc.Activities);
        Assert.Empty(doc.Activities);
        Assert.NotNull(doc.Bridges);
        Assert.Empty(doc.Bridges);
        Assert.NotNull(doc.Diagnostics);
        Assert.Empty(doc.Diagnostics);
    }
}
