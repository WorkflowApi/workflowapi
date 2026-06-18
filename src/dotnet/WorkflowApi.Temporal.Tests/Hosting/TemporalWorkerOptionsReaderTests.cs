using B2B.RiskService.Activities;
using B2B.RiskService.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Activities;
using Temporalio.Extensions.Hosting;
using Temporalio.Workflows;
using WorkflowApi.Temporal.Hosting;

namespace WorkflowApi.Temporal.Tests.Hosting;

public sealed class TemporalWorkerOptionsReaderTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Fixture types
    // ──────────────────────────────────────────────────────────────────────────

    [Workflow]
    public sealed class FixtureWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    [Workflow]
    public sealed class AnotherFixtureWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    [Workflow]
    public sealed class QueueBWorkflow
    {
        [WorkflowRun]
        public Task RunAsync() => Task.CompletedTask;
    }

    public sealed class FixtureActivities
    {
        [Activity]
        public Task DoWorkAsync() => Task.CompletedTask;
    }

    public sealed class QueueBActivities
    {
        [Activity]
        public Task ProcessAsync() => Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reads_single_worker_with_workflows_and_activities()
    {
        var services = new ServiceCollection();
        services.AddTemporalClient(o =>
        {
            o.Namespace = "test-ns";
            o.TargetHost = "localhost:7233";
        });
        services
            .AddHostedTemporalWorker("queue-a")
            .AddWorkflow<FixtureWorkflow>()
            .AddScopedActivities<FixtureActivities>();

        var reader = new TemporalWorkerOptionsReader();
        var workers = reader.Read(services);

        var worker = Assert.Single(workers);
        Assert.Equal("queue-a", worker.TaskQueue);
        Assert.Equal("test-ns", worker.Namespace);
        Assert.Contains(typeof(FixtureWorkflow), worker.WorkflowTypes);
        Assert.Contains(typeof(FixtureActivities), worker.ActivityTypes);
    }

    [Fact]
    public void Reads_two_workers_with_distinct_task_queues()
    {
        var services = new ServiceCollection();
        services
            .AddHostedTemporalWorker("queue-a")
            .AddWorkflow<FixtureWorkflow>();
        services
            .AddHostedTemporalWorker("queue-b")
            .AddWorkflow<QueueBWorkflow>()
            .AddScopedActivities<QueueBActivities>();

        var reader = new TemporalWorkerOptionsReader();
        var workers = reader.Read(services);

        Assert.Equal(2, workers.Count);

        // Sorted by task queue (ordinal): queue-a before queue-b
        Assert.Equal("queue-a", workers[0].TaskQueue);
        Assert.Equal("queue-b", workers[1].TaskQueue);

        Assert.Contains(typeof(FixtureWorkflow), workers[0].WorkflowTypes);
        Assert.Contains(typeof(QueueBWorkflow), workers[1].WorkflowTypes);
        Assert.Contains(typeof(QueueBActivities), workers[1].ActivityTypes);
    }

    [Fact]
    public void Worker_with_no_explicit_temporal_client_uses_sdk_default_namespace()
    {
        var services = new ServiceCollection();
        services
            .AddHostedTemporalWorker("queue-a")
            .AddWorkflow<FixtureWorkflow>();

        var reader = new TemporalWorkerOptionsReader();
        var workers = reader.Read(services);

        var worker = Assert.Single(workers);
        Assert.Equal("default", worker.Namespace);
    }

    [Fact]
    public void Reads_real_risk_worker_registration()
    {
        var services = new ServiceCollection();
        services.AddTemporalClient(o =>
        {
            o.Namespace = "B2B.RiskService";
            o.TargetHost = "localhost:7233";
        });
        services
            .AddHostedTemporalWorker("risk-enrichment")
            .AddWorkflow<RiskEnrichmentWorkflow>()
            .AddWorkflow<CalculateRiskWorkflow>()
            .AddWorkflow<ExternalChecksWorkflow>()
            .AddScopedActivities<IdentifyCompanyActivity>()
            .AddScopedActivities<EnrichDnbActivity>()
            .AddScopedActivities<ScoreCompanyRiskActivity>();

        var reader = new TemporalWorkerOptionsReader();
        var workers = reader.Read(services);

        var worker = Assert.Single(workers);
        Assert.Equal("risk-enrichment", worker.TaskQueue);
        Assert.Equal("B2B.RiskService", worker.Namespace);

        Assert.Equal(3, worker.WorkflowTypes.Count);
        Assert.Contains(typeof(RiskEnrichmentWorkflow), worker.WorkflowTypes);
        Assert.Contains(typeof(CalculateRiskWorkflow), worker.WorkflowTypes);
        Assert.Contains(typeof(ExternalChecksWorkflow), worker.WorkflowTypes);

        Assert.True(worker.ActivityTypes.Count >= 3,
            $"Expected ≥3 activity types, got {worker.ActivityTypes.Count}");
        Assert.Contains(typeof(IdentifyCompanyActivity), worker.ActivityTypes);
        Assert.Contains(typeof(EnrichDnbActivity), worker.ActivityTypes);
        Assert.Contains(typeof(ScoreCompanyRiskActivity), worker.ActivityTypes);
    }

    [Fact]
    public void Empty_services_returns_empty_list()
    {
        var reader = new TemporalWorkerOptionsReader();
        var result = reader.Read(new ServiceCollection());

        Assert.Empty(result);
    }

    [Fact]
    public void Throws_on_null()
    {
        var reader = new TemporalWorkerOptionsReader();
        Assert.Throws<ArgumentNullException>(() => reader.Read(null!));
    }
}
