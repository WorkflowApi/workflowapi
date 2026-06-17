using System.Diagnostics.Metrics;

namespace B2B.RiskService.Metrics;

/// <summary>
/// Declares worker-level OpenTelemetry meters and counters.
/// </summary>
internal static class WorkerMetrics
{
    public const string MeterName = "WorkflowApi.Temporal.RiskWorker";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> ActivityExecutions =
        Meter.CreateCounter<long>(
            "temporal_activity_task_completed",
            description: "Total completed activity task executions");

    public static void RecordActivityExecution(string activityType) =>
        ActivityExecutions.Add(1, new KeyValuePair<string, object?>("activity_type", activityType));
}
