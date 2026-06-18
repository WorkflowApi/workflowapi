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

    /// <summary>
    /// Counter for terminal workflow task executions (Completed, Failed, Cancelled).
    /// Exposed as <c>temporal_workflow_task_completed_total{workflow_type="..."}</c> in Prometheus.
    /// </summary>
    public static readonly Counter<long> WorkflowExecutions =
        Meter.CreateCounter<long>(
            "temporal_workflow_task_completed",
            description: "Total terminal workflow task executions (Completed, Failed, Cancelled)");

    /// <summary>
    /// Increments the <see cref="WorkflowExecutions"/> counter for the given workflow type.
    /// Call this from the <c>finally</c> block of a workflow's <c>RunAsync</c> method to
    /// capture all terminal outcomes (Completed, Failed, Cancelled).
    /// </summary>
    /// <param name="workflowType">
    /// The workflow class name (e.g. <c>nameof(RiskEnrichmentWorkflow)</c>).
    /// Must not include namespace prefixes.
    /// </param>
    public static void RecordWorkflowExecution(string workflowType) =>
        WorkflowExecutions.Add(1, new KeyValuePair<string, object?>("workflow_type", workflowType));
}
