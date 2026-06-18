using System.Diagnostics.Metrics;

namespace B2B.RiskService.Metrics;

/// <summary>
/// Declares worker-level OpenTelemetry meters, counters, and histograms.
/// </summary>
internal static class WorkerMetrics
{
    public const string MeterName = "WorkflowApi.Temporal.RiskWorker";
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> ActivityExecutions =
        Meter.CreateCounter<long>(
            "temporal_activity_task_completed",
            description: "Total completed activity task executions");

    /// <summary>
    /// Schedule-to-close latency histogram per activity type (seconds).
    /// Exposed as <c>temporal_activity_schedule_to_close_latency_bucket{activity_type="..."}</c>.
    /// </summary>
    public static readonly Histogram<double> ActivityLatency =
        Meter.CreateHistogram<double>(
            "temporal_activity_schedule_to_close_latency",
            unit: "s",
            description: "Activity schedule-to-close execution latency in seconds");

    /// <summary>
    /// Increments the execution counter and records latency for the given activity type.
    /// </summary>
    /// <param name="activityType">The activity class name.</param>
    /// <param name="elapsedSeconds">Wall-clock execution time in seconds.</param>
    public static void RecordActivityExecution(string activityType, double elapsedSeconds)
    {
        var tag = new KeyValuePair<string, object?>("activity_type", activityType);
        ActivityExecutions.Add(1, tag);
        ActivityLatency.Record(elapsedSeconds, tag);
    }

    /// <summary>
    /// Counter for terminal workflow task executions (Completed, Failed, Cancelled).
    /// Exposed as <c>temporal_workflow_task_completed_total{workflow_type="..."}</c> in Prometheus.
    /// </summary>
    public static readonly Counter<long> WorkflowExecutions =
        Meter.CreateCounter<long>(
            "temporal_workflow_task_completed",
            description: "Total terminal workflow task executions (Completed, Failed, Cancelled)");

    /// <summary>
    /// End-to-end workflow latency histogram per workflow type (seconds).
    /// Exposed as <c>temporal_workflow_e2e_latency_bucket{workflow_type="..."}</c>.
    /// </summary>
    public static readonly Histogram<double> WorkflowLatency =
        Meter.CreateHistogram<double>(
            "temporal_workflow_e2e_latency",
            unit: "s",
            description: "Workflow end-to-end execution latency in seconds");

    /// <summary>
    /// Increments the workflow execution counter.
    /// </summary>
    public static void RecordWorkflowExecution(string workflowType) =>
        WorkflowExecutions.Add(1, new KeyValuePair<string, object?>("workflow_type", workflowType));

    /// <summary>
    /// Records the end-to-end latency for the given workflow type.
    /// </summary>
    /// <param name="workflowType">The workflow class name.</param>
    /// <param name="elapsedSeconds">Wall-clock duration in seconds.</param>
    public static void RecordWorkflowLatency(string workflowType, double elapsedSeconds) =>
        WorkflowLatency.Record(elapsedSeconds, new KeyValuePair<string, object?>("workflow_type", workflowType));
}
