using System.Text.Json;
using B2B.RiskService.Activities;
using B2B.RiskService.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Extensions.Hosting;
using WorkflowApi.Temporal.Cli;

namespace WorkflowApi.Temporal.Tests.Cli;

public sealed class DumpCommandTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Guard tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Throws_on_null_services()
    {
        var cmd = new DumpCommand();
        Assert.Throws<ArgumentNullException>(() => cmd.Execute(null!, "some-dir"));
    }

    [Fact]
    public void Throws_on_null_outputDirectory()
    {
        var cmd = new DumpCommand();
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => cmd.Execute(services, null!));
    }

    [Fact]
    public void Throws_on_empty_outputDirectory()
    {
        var cmd = new DumpCommand();
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => cmd.Execute(services, "   "));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Directory creation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Creates_output_directory_when_missing()
    {
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            Assert.False(Directory.Exists(outDir));
            new DumpCommand().Execute(new ServiceCollection(), outDir);
            Assert.True(Directory.Exists(outDir));
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Integration: full RiskWorker pipeline
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Writes_yaml_per_worker_and_index_json()
    {
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var services = BuildRiskWorkerServices();
            var result = new DumpCommand().Execute(services, outDir);

            // One document emitted
            Assert.Single(result.Documents);

            // YAML file exists and has expected content
            var yamlFiles = Directory.GetFiles(outDir, "*.yaml");
            Assert.Single(yamlFiles);
            var yaml = File.ReadAllText(yamlFiles[0]);
            Assert.Contains("namespace: B2B.RiskService", yaml);
            Assert.Contains("taskQueue: risk-enrichment", yaml);

            // index.json exists and parses correctly
            var indexPath = Path.Combine(outDir, "index.json");
            Assert.True(File.Exists(indexPath));

            using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
            var root = doc.RootElement;

            var documents = root.GetProperty("documents");
            Assert.Equal(1, documents.GetArrayLength());

            var first = documents[0];
            Assert.Equal(3, first.GetProperty("workflowCount").GetInt32());
            Assert.Equal(12, first.GetProperty("activityCount").GetInt32());

            var diagnostics = root.GetProperty("diagnostics");
            Assert.Equal(0, diagnostics.GetArrayLength());

            // DumpResult consistency
            Assert.Equal(3, result.Documents[0].WorkflowCount);
            Assert.Equal(12, result.Documents[0].ActivityCount);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Empty services → empty index
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Empty_services_writes_empty_index_with_no_yaml()
    {
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var result = new DumpCommand().Execute(new ServiceCollection(), outDir);

            Assert.Empty(result.Documents);
            Assert.Empty(Directory.GetFiles(outDir, "*.yaml"));

            var indexPath = Path.Combine(outDir, "index.json");
            Assert.True(File.Exists(indexPath));

            using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
            var root = doc.RootElement;
            Assert.Equal(0, root.GetProperty("documents").GetArrayLength());
            Assert.Equal(0, root.GetProperty("diagnostics").GetArrayLength());
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Orphan diagnostics surface in index.json
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Index_includes_orphan_diagnostics()
    {
        // Register only ONE of the three RiskWorker workflows. The scanner scans the
        // RiskWorker assembly (derived from registered types) and finds ALL three.
        // The two unregistered workflows produce WF001 diagnostics in index.json.
        var outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            var services = new ServiceCollection();
            services.AddTemporalClient(o =>
            {
                o.TargetHost = "localhost:7233";
                o.Namespace = "B2B.RiskService";
            });
            services
                .AddHostedTemporalWorker("risk-enrichment")
                .AddWorkflow<RiskEnrichmentWorkflow>(); // CalculateRiskWorkflow and ExternalChecksWorkflow omitted → WF001

            var result = new DumpCommand().Execute(services, outDir);

            var indexPath = Path.Combine(outDir, "index.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
            var diags = doc.RootElement.GetProperty("diagnostics");
            Assert.True(diags.GetArrayLength() > 0, "Expected at least one diagnostic in index.json");

            var codes = Enumerable.Range(0, diags.GetArrayLength())
                .Select(i => diags[i].GetProperty("code").GetString())
                .ToList();
            Assert.Contains("WF001", codes);
        }
        finally
        {
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static IServiceCollection BuildRiskWorkerServices()
    {
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
        return services;
    }
}

