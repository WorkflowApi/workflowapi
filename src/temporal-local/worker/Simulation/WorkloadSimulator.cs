using B2B.RiskService.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

namespace B2B.RiskService.Simulation;

/// <summary>
/// Continuously starts workflows to simulate local workload.
/// </summary>
public sealed class WorkloadSimulator : BackgroundService
{
    private readonly ITemporalClient client;
    private readonly ILogger<WorkloadSimulator> logger;
    private readonly bool enabled;
    private readonly TimeSpan interval;
    private readonly string taskQueue;

    /// <summary>
    /// Creates a new workload simulator.
    /// </summary>
    /// <param name="client">Temporal client.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="configuration">Configuration root.</param>
    public WorkloadSimulator(
        ITemporalClient client,
        ILogger<WorkloadSimulator> logger,
        IConfiguration configuration)
    {
        this.client = client;
        this.logger = logger;
        enabled = bool.TryParse(configuration["SIMULATOR_ENABLED"], out var parsedEnabled)
            ? parsedEnabled
            : true;
        interval = TimeSpan.FromSeconds(
            int.TryParse(configuration["SIMULATOR_INTERVAL_SECONDS"], out var parsedInterval)
                ? Math.Max(parsedInterval, 1)
                : 5);
        taskQueue = configuration["TEMPORAL_TASK_QUEUE"] ?? "risk-enrichment";
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!enabled)
        {
            logger.LogInformation("Workload simulator disabled via SIMULATOR_ENABLED=false.");
            return;
        }

        logger.LogInformation("Workload simulator enabled; starting one workflow every {IntervalSeconds} seconds.", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var request = MockDataGenerator.CreateRiskEnrichmentRequest();
                var workflowId = $"risk-sim-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}";
                var handle = await client.StartWorkflowAsync(
                    (RiskEnrichmentWorkflow workflow) => workflow.RunAsync(request),
                    new WorkflowOptions
                    {
                        Id = workflowId,
                        TaskQueue = taskQueue,
                    });

                logger.LogInformation(
                    "Started simulated workflow: WorkflowId={WorkflowId}, RunId={RunId}, CompanyId={CompanyId}, Country={Country}",
                    handle.Id,
                    handle.ResultRunId,
                    request.CompanyId,
                    request.Country);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to start simulated workflow.");
            }
        }
    }
}
