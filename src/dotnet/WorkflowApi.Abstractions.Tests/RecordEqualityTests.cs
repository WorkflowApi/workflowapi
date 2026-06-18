using Xunit;

namespace WorkflowApi.Abstractions.Tests;

public class RecordEqualityTests
{
    [Fact]
    public void InfoDef_EqualRecords_AreEqual()
    {
        var info1 = new InfoDef("MyAPI", "1.0.0", "Summary", "Description");
        var info2 = new InfoDef("MyAPI", "1.0.0", "Summary", "Description");

        Assert.Equal(info1, info2);
        Assert.True(info1 == info2);
    }

    [Fact]
    public void InfoDef_DifferentRecords_AreNotEqual()
    {
        var info1 = new InfoDef("MyAPI", "1.0.0", "Summary", "Description");
        var info2 = new InfoDef("MyAPI", "2.0.0", "Summary", "Description");

        Assert.NotEqual(info1, info2);
        Assert.False(info1 == info2);
    }

    [Fact]
    public void InfoDef_WithExpression_CreatesNewInstanceWithChangedField()
    {
        var info1 = new InfoDef("MyAPI", "1.0.0", "Summary", "Description");
        var info2 = info1 with { Version = "2.0.0" };

        Assert.NotEqual(info1, info2);
        Assert.Equal("1.0.0", info1.Version);
        Assert.Equal("2.0.0", info2.Version);
        Assert.Equal("MyAPI", info2.Title);
        Assert.Equal("Summary", info2.Summary);
        Assert.Equal("Description", info2.Description);
    }

    [Fact]
    public void OperationDef_EqualRecords_AreEqual()
    {
        var input = new SchemaRefDef("InputSchema");
        var output = new SchemaRefDef("OutputSchema");
        var op1 = new OperationDef("opId", "Summary", input, output, null);
        var op2 = new OperationDef("opId", "Summary", input, output, null);

        Assert.Equal(op1, op2);
        Assert.True(op1 == op2);
    }

    [Fact]
    public void OperationDef_DifferentRecords_AreNotEqual()
    {
        var input = new SchemaRefDef("InputSchema");
        var output = new SchemaRefDef("OutputSchema");
        var op1 = new OperationDef("opId1", "Summary", input, output, null);
        var op2 = new OperationDef("opId2", "Summary", input, output, null);

        Assert.NotEqual(op1, op2);
        Assert.False(op1 == op2);
    }

    [Fact]
    public void OperationDef_WithExpression_CreatesNewInstanceWithChangedField()
    {
        var input = new SchemaRefDef("InputSchema");
        var output = new SchemaRefDef("OutputSchema");
        var op1 = new OperationDef("opId", "Summary", input, output, null);
        var op2 = op1 with { Summary = "NewSummary" };

        Assert.NotEqual(op1, op2);
        Assert.Equal("Summary", op1.Summary);
        Assert.Equal("NewSummary", op2.Summary);
        Assert.Equal("opId", op2.OperationId);
        Assert.Equal(input, op2.Input);
        Assert.Equal(output, op2.Output);
    }

    [Fact]
    public void WorkflowApiDocument_EqualRecords_AreEqual()
    {
        var info = new InfoDef("API", "1.0", null, null);
        var host = new HostDef("host-1", "HostName");
        var bindings = new BindingsDef(null);
        var workflows = new Dictionary<string, WorkflowDef>();
        var doc1 = new WorkflowApiDocument("1.0", info, host, bindings, workflows, null, null, []);
        var doc2 = new WorkflowApiDocument("1.0", info, host, bindings, workflows, null, null, []);

        Assert.Equal(doc1, doc2);
        Assert.True(doc1 == doc2);
    }

    [Fact]
    public void WorkflowApiDocument_DifferentRecords_AreNotEqual()
    {
        var info = new InfoDef("API", "1.0", null, null);
        var host1 = new HostDef("host-1", "HostName");
        var host2 = new HostDef("host-2", "HostName");
        var bindings = new BindingsDef(null);
        var workflows = new Dictionary<string, WorkflowDef>();
        var doc1 = new WorkflowApiDocument("1.0", info, host1, bindings, workflows, null, null, []);
        var doc2 = new WorkflowApiDocument("1.0", info, host2, bindings, workflows, null, null, []);

        Assert.NotEqual(doc1, doc2);
        Assert.False(doc1 == doc2);
    }

    [Fact]
    public void WorkflowApiDocument_WithExpression_CreatesNewInstanceWithChangedField()
    {
        var info1 = new InfoDef("API", "1.0", null, null);
        var info2 = new InfoDef("API", "2.0", null, null);
        var host = new HostDef("host-1", "HostName");
        var bindings = new BindingsDef(null);
        var workflows = new Dictionary<string, WorkflowDef>();
        var doc1 = new WorkflowApiDocument("1.0", info1, host, bindings, workflows, null, null, []);
        var doc2 = doc1 with { Info = info2 };

        Assert.NotEqual(doc1, doc2);
        Assert.Equal(info1, doc1.Info);
        Assert.Equal(info2, doc2.Info);
        Assert.Equal("1.0", doc2.WorkflowApi);
        Assert.Equal(host, doc2.Host);
        Assert.Equal(bindings, doc2.Bindings);
    }
}
