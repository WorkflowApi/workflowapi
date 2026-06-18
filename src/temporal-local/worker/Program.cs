using B2B.RiskService.Activities;
using B2B.RiskService.Metrics;
using B2B.RiskService.Simulation;
using B2B.RiskService.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Temporalio.Extensions.Hosting;

if (args.Length >= 2 && args[0] == "--workflowapi-dump")
{
    var dumpDir = args[1];
    var services = new ServiceCollection();
    ConfigureServices(services);
    var result = new WorkflowApi.Temporal.Cli.DumpCommand().Execute(services, dumpDir);
    Console.WriteLine($"Wrote {result.Documents.Count} documents to {dumpDir}");
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
});

var temporalAddress = builder.Configuration["TEMPORAL_ADDRESS"] ?? "temporal:7233";
var temporalNamespace = builder.Configuration["TEMPORAL_NAMESPACE"] ?? "B2B.RiskService";
var temporalTaskQueue = builder.Configuration["TEMPORAL_TASK_QUEUE"] ?? "risk-enrichment";

ConfigureServices(builder.Services, temporalAddress, temporalNamespace, temporalTaskQueue);

// Placeholder added in Phase 4, fully enabled in Phase 5 via SIMULATOR_ENABLED.
builder.Services.AddHostedService<WorkloadSimulator>();

var host = builder.Build();
await host.RunAsync();

// ─────────────────────────────────────────────────────────────────────────────
// Shared DI registrations used by both the normal host path and the dump path.
// The dump path calls with defaults; the host path forwards configuration values.
// ─────────────────────────────────────────────────────────────────────────────
static void ConfigureServices(
    IServiceCollection services,
    string temporalAddress = "temporal:7233",
    string temporalNamespace = "B2B.RiskService",
    string temporalTaskQueue = "risk-enrichment")
{
    const int metricsPort = 9090;

    services.AddTemporalClient(options =>
    {
        options.TargetHost = temporalAddress;
        options.Namespace = temporalNamespace;
    });

    services
        .AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics
                .AddMeter(WorkerMetrics.MeterName)
                .AddPrometheusHttpListener(options =>
                {
                    options.Host = "risk-worker";
                    options.Port = metricsPort;
                });
        });

    services
        .AddHostedTemporalWorker(temporalTaskQueue)
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
}
