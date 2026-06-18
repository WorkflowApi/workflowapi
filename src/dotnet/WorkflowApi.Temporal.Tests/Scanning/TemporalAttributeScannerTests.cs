using System.Reflection;
using Temporalio.Activities;
using Temporalio.Workflows;
using WorkflowApi.Temporal.Scanning;

namespace WorkflowApi.Temporal.Tests.Scanning;

public sealed class TemporalAttributeScannerTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Fixture types
    // ──────────────────────────────────────────────────────────────────────────

    [Workflow]
    public sealed class SimpleWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    [Workflow]
    public sealed class FullWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;

        [WorkflowSignal("MySignal")]
        public Task SignalAsync() => Task.CompletedTask;

        [WorkflowQuery("MyQuery")]
        public string Query() => string.Empty;

        [WorkflowUpdate("MyUpdate")]
        public Task UpdateAsync() => Task.CompletedTask;
    }

    [Workflow]
    public sealed class NoNameWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    [Workflow("CustomName")]
    public sealed class ExplicitNameWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    [Workflow]
    public sealed class AsyncSuffixWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;

        [WorkflowSignal]
        public Task CancelAsync() => Task.CompletedTask;
    }

    public sealed class ActivityHost
    {
        [Activity("EnrichDnb")]
        public Task<string> RunAsync() => Task.FromResult(string.Empty);
    }

    [Workflow]
    public sealed class NoRunWorkflow { }

    public sealed class AsyncActivityFixture
    {
        [Activity]
        public Task DoWorkAsync() => Task.CompletedTask;
    }

    public static class StaticActivityFixture
    {
        [Activity("StaticPing")]
        public static Task PingAsync() => Task.CompletedTask;
    }

    // BadNameWorkflow is intentionally NOT scanned by the assembly-level tests;
    // the bad-name test calls Scan(IEnumerable<Type>) directly with just this type.
    [Workflow("risk/enrichment")]
    public sealed class BadNameWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Finds_simple_workflow()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(SimpleWorkflow)]);

        var wf = result.Workflows.First(w => w.WorkflowType == typeof(SimpleWorkflow));
        Assert.NotNull(wf.Run);
        Assert.Empty(wf.Signals);
        Assert.Empty(wf.Queries);
        Assert.Empty(wf.Updates);
    }

    [Fact]
    public void Finds_workflow_with_signal_query_update()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(FullWorkflow)]);

        var wf = result.Workflows.First(w => w.WorkflowType == typeof(FullWorkflow));
        Assert.NotNull(wf.Run);
        Assert.Single(wf.Signals);
        Assert.Single(wf.Queries);
        Assert.Single(wf.Updates);
    }

    [Fact]
    public void Workflow_name_falls_back_to_type_name_when_attribute_name_omitted()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(NoNameWorkflow)]);

        var wf = result.Workflows.First(w => w.WorkflowType == typeof(NoNameWorkflow));
        Assert.Equal("NoNameWorkflow", wf.Name);
    }

    [Fact]
    public void Workflow_uses_explicit_name_from_attribute()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(ExplicitNameWorkflow)]);

        var wf = result.Workflows.First(w => w.WorkflowType == typeof(ExplicitNameWorkflow));
        Assert.Equal("CustomName", wf.Name);
    }

    [Fact]
    public void Operation_id_falls_back_to_method_name_stripping_async_suffix()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(AsyncSuffixWorkflow)]);

        var wf = result.Workflows.First(w => w.WorkflowType == typeof(AsyncSuffixWorkflow));
        var signal = Assert.Single(wf.Signals);
        Assert.Equal("Cancel", signal.OperationId);
        Assert.Equal("Run", wf.Run!.OperationId);
    }

    [Fact]
    public void Finds_activities()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(ActivityHost)]);

        var act = result.Activities.First(a => a.ActivityType == typeof(ActivityHost));
        Assert.Equal("EnrichDnb", act.Id);
    }

    [Fact]
    public void Returns_empty_result_for_assembly_with_no_temporal_types()
    {
        var result = new TemporalAttributeScanner().Scan(
            [typeof(string).Assembly]);

        Assert.Empty(result.Workflows);
        Assert.Empty(result.Activities);
    }

    [Fact]
    public void Scans_real_risk_worker_assembly()
    {
        // RiskWorker.csproj is referenced as a ProjectReference, so its types are available.
        var workerAssembly = typeof(B2B.RiskService.Workflows.RiskEnrichmentWorkflow).Assembly;

        var result = new TemporalAttributeScanner().Scan([workerAssembly]);

        // At least the three known workflows exist
        var workflowTypeNames = result.Workflows.Select(w => w.WorkflowType.Name).ToHashSet();
        Assert.Contains("RiskEnrichmentWorkflow", workflowTypeNames);
        Assert.Contains("CalculateRiskWorkflow", workflowTypeNames);
        Assert.Contains("ExternalChecksWorkflow", workflowTypeNames);

        // At least the known EnrichDnbActivity exists
        var activityIds = result.Activities.Select(a => a.Id).ToHashSet();
        Assert.Contains("EnrichDnbActivity", activityIds);

        // Shape checks: at least 3 workflows and several activities
        Assert.True(result.Workflows.Count >= 3, $"Expected ≥3 workflows, got {result.Workflows.Count}");
        Assert.True(result.Activities.Count >= 3, $"Expected ≥3 activities, got {result.Activities.Count}");
    }

    [Fact]
    public void Workflow_with_no_WorkflowRun_has_null_Run()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(NoRunWorkflow)]);

        var wf = result.Workflows.First(w => w.WorkflowType == typeof(NoRunWorkflow));
        Assert.Null(wf.Run);
    }

    [Fact]
    public void Activity_async_suffix_stripped_when_no_name_on_attribute()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(AsyncActivityFixture)]);

        var act = Assert.Single(result.Activities);
        Assert.Equal("DoWork", act.Id);
    }

    [Fact]
    public void Static_activity_method_is_discovered()
    {
        var result = new TemporalAttributeScanner().Scan([typeof(StaticActivityFixture)]);

        var act = Assert.Single(result.Activities);
        Assert.Equal("StaticPing", act.Id);
    }

    [Fact]
    public void Rejects_yaml_unsafe_workflow_name()
    {
        var scanner = new TemporalAttributeScanner();
        var ex = Assert.Throws<InvalidOperationException>(
            () => scanner.Scan([typeof(BadNameWorkflow)]));
        Assert.Contains("/", ex.Message);
    }
}
