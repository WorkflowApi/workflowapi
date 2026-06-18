using B2B.RiskService.Activities;
using B2B.RiskService.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Activities;
using Temporalio.Extensions.Hosting;
using Temporalio.Workflows;
using WorkflowApi.Abstractions;
using WorkflowApi.Temporal.Hosting;
using WorkflowApi.Temporal.Mapping;
using WorkflowApi.Temporal.Scanning;

namespace WorkflowApi.Temporal.Tests.Mapping;

public sealed class WorkerMappingBuilderTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Fixture types
    // ──────────────────────────────────────────────────────────────────────────

    [Workflow]
    public sealed class WorkflowA
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    [Workflow]
    public sealed class WorkflowB
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    public sealed class ActivityA
    {
        [Activity]
        public Task DoWorkAsync() => Task.CompletedTask;
    }

    public sealed class ActivityB
    {
        [Activity]
        public Task ProcessAsync() => Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static TemporalScanResult Scan(params Type[] types) =>
        new TemporalAttributeScanner().Scan(types);

    private static WorkerMapping Build(TemporalScanResult scan, IReadOnlyList<RegisteredWorker> workers) =>
        new WorkerMappingBuilder().Build(scan, workers);

    // ──────────────────────────────────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Happy_path_single_worker_with_scanned_workflow_and_activity()
    {
        var scan = Scan(typeof(WorkflowA), typeof(ActivityA));
        var workers = new[]
        {
            new RegisteredWorker("q", null,
                [typeof(WorkflowA)],
                [typeof(ActivityA)])
        };

        var mapping = Build(scan, workers);

        var worker = Assert.Single(mapping.Workers);
        Assert.Equal("q", worker.TaskQueue);
        Assert.Single(worker.Workflows);
        Assert.Single(worker.Activities);
        Assert.Empty(mapping.Diagnostics);
    }

    [Fact]
    public void Orphan_scanned_workflow_emits_WF001()
    {
        var scan = Scan(typeof(WorkflowA), typeof(WorkflowB));
        var workers = new[]
        {
            new RegisteredWorker("q", null,
                [typeof(WorkflowA)],
                [])
        };

        var mapping = Build(scan, workers);

        var worker = Assert.Single(mapping.Workers);
        Assert.Single(worker.Workflows);

        var diag = Assert.Single(mapping.Diagnostics);
        Assert.Equal("WF001", diag.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal(typeof(WorkflowB).FullName, diag.Target);
    }

    [Fact]
    public void Orphan_scanned_activity_emits_ACT001()
    {
        var scan = Scan(typeof(WorkflowA), typeof(ActivityA), typeof(ActivityB));
        var workers = new[]
        {
            new RegisteredWorker("q", null,
                [typeof(WorkflowA)],
                [typeof(ActivityA)])
        };

        var mapping = Build(scan, workers);

        var diag = Assert.Single(mapping.Diagnostics);
        Assert.Equal("ACT001", diag.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal(typeof(ActivityB).FullName, diag.Target);
    }

    [Fact]
    public void Registered_but_unscanned_workflow_emits_WF002()
    {
        // Empty scan result — scanner saw nothing.
        var scan = Scan();
        var workers = new[]
        {
            new RegisteredWorker("q", null,
                [typeof(WorkflowA)],
                [])
        };

        var mapping = Build(scan, workers);

        var worker = Assert.Single(mapping.Workers);
        Assert.Empty(worker.Workflows);

        var diag = Assert.Single(mapping.Diagnostics);
        Assert.Equal("WF002", diag.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal(typeof(WorkflowA).FullName, diag.Target);
    }

    [Fact]
    public void Registered_but_unscanned_activity_emits_ACT002()
    {
        // Empty scan result — scanner saw nothing.
        var scan = Scan();
        var workers = new[]
        {
            new RegisteredWorker("q", null,
                [],
                [typeof(ActivityA)])
        };

        var mapping = Build(scan, workers);

        var worker = Assert.Single(mapping.Workers);
        Assert.Empty(worker.Activities);

        var diag = Assert.Single(mapping.Diagnostics);
        Assert.Equal("ACT002", diag.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        Assert.Equal(typeof(ActivityA).FullName, diag.Target);
    }

    [Fact]
    public void Two_workers_partition_workflows_correctly()
    {
        var scan = Scan(typeof(WorkflowA), typeof(WorkflowB));
        var workers = new[]
        {
            new RegisteredWorker("q1", null, [typeof(WorkflowA)], []),
            new RegisteredWorker("q2", null, [typeof(WorkflowB)], [])
        };

        var mapping = Build(scan, workers);

        Assert.Equal(2, mapping.Workers.Count);
        // Sorted by task queue
        Assert.Equal("q1", mapping.Workers[0].TaskQueue);
        Assert.Equal("q2", mapping.Workers[1].TaskQueue);

        Assert.Single(mapping.Workers[0].Workflows);
        Assert.Equal(typeof(WorkflowA), mapping.Workers[0].Workflows[0].WorkflowType);

        Assert.Single(mapping.Workers[1].Workflows);
        Assert.Equal(typeof(WorkflowB), mapping.Workers[1].Workflows[0].WorkflowType);

        Assert.Empty(mapping.Diagnostics);
    }

    [Fact]
    public void Workflow_registered_with_two_workers_appears_in_both()
    {
        var scan = Scan(typeof(WorkflowA));
        var workers = new[]
        {
            new RegisteredWorker("q1", null, [typeof(WorkflowA)], []),
            new RegisteredWorker("q2", null, [typeof(WorkflowA)], [])
        };

        var mapping = Build(scan, workers);

        Assert.Equal(2, mapping.Workers.Count);
        Assert.Single(mapping.Workers[0].Workflows);
        Assert.Equal(typeof(WorkflowA), mapping.Workers[0].Workflows[0].WorkflowType);
        Assert.Single(mapping.Workers[1].Workflows);
        Assert.Equal(typeof(WorkflowA), mapping.Workers[1].Workflows[0].WorkflowType);

        // Legitimately multi-registered — no diagnostics.
        Assert.Empty(mapping.Diagnostics);
    }

    [Fact]
    public void Full_RiskWorker_integration()
    {
        // Scanner: scan the real worker assembly.
        var workerAssembly = typeof(RiskEnrichmentWorkflow).Assembly;
        var scan = new TemporalAttributeScanner().Scan([workerAssembly]);

        // DI: mirror Program.cs exactly.
        var services = new ServiceCollection();
        services.AddTemporalClient(o =>
        {
            o.TargetHost = "localhost:7233";
            o.Namespace = "B2B.RiskService";
        });
        services
            .AddHostedTemporalWorker("risk-enrichment")
            .AddWorkflow<RiskEnrichmentWorkflow>()
            .AddWorkflow<CalculateRiskWorkflow>()
            .AddWorkflow<ExternalChecksWorkflow>()
            .AddScopedActivities<IdentifyCompanyActivity>()
            .AddScopedActivities<EnrichDnbActivity>()
            .AddScopedActivities<ScoreCompanyRiskActivity>()
            .AddScopedActivities<AggregateRiskScoreActivity>()
            .AddScopedActivities<PublishRiskResultActivity>()
            .AddScopedActivities<GeneratePdfActivity>()
            .AddScopedActivities<FetchPaymentHistoryActivity>()
            .AddScopedActivities<FetchCreditLimitActivity>()
            .AddScopedActivities<NormaliseRiskSignalsActivity>()
            .AddScopedActivities<SanctionsCheckActivity>()
            .AddScopedActivities<PoliticallyExposedPersonCheckActivity>()
            .AddScopedActivities<AdverseMediaCheckActivity>();

        var registeredWorkers = new TemporalWorkerOptionsReader().Read(services);

        var mapping = new WorkerMappingBuilder().Build(scan, registeredWorkers);

        var worker = Assert.Single(mapping.Workers);
        Assert.Equal("risk-enrichment", worker.TaskQueue);
        Assert.Equal("B2B.RiskService", worker.Namespace);
        Assert.Equal(3, worker.Workflows.Count);
        Assert.Equal(12, worker.Activities.Count);
        Assert.Empty(mapping.Diagnostics);
    }

    [Fact]
    public void Throws_on_null_scanResult()
    {
        var builder = new WorkerMappingBuilder();
        var workers = Array.Empty<RegisteredWorker>();

        Assert.Throws<ArgumentNullException>(() => builder.Build(null!, workers));
    }

    [Fact]
    public void Throws_on_null_registeredWorkers()
    {
        var builder = new WorkerMappingBuilder();
        var scan = Scan();

        Assert.Throws<ArgumentNullException>(() => builder.Build(scan, null!));
    }
}
