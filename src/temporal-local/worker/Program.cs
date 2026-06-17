using B2B.RiskService.Activities;
using B2B.RiskService.Simulation;
using B2B.RiskService.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Temporalio.Extensions.Hosting;

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

builder.Services.AddTemporalClient(options =>
{
    options.TargetHost = temporalAddress;
    options.Namespace = temporalNamespace;
});

builder.Services
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

// Placeholder added in Phase 4, fully enabled in Phase 5 via SIMULATOR_ENABLED.
builder.Services.AddHostedService<WorkloadSimulator>();

var host = builder.Build();
await host.RunAsync();
