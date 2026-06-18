import type { WorkflowGraphNode } from "../types/workflow-graph-model";
import { useMetrics } from "../contexts/MetricsContext";

interface NodeDetailPanelProps {
  node: WorkflowGraphNode | undefined;
  onClose: () => void;
}

function getRawString(node: WorkflowGraphNode, key: string): string | undefined {
  if (!node.raw || typeof node.raw !== "object") return undefined;
  const value = (node.raw as Record<string, unknown>)[key];
  return typeof value === "string" ? value : undefined;
}

function firstDefinedString(...values: Array<string | undefined>): string | undefined {
  return values.find((value): value is string => typeof value === "string" && value.length > 0);
}

function DetailRow({ label, value }: { label: string; value: string }): JSX.Element {
  if (value.length <= 80) {
    return (
      <div className="flex flex-col gap-1 py-2">
        <span className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">{label}</span>
        <span className="break-all text-sm text-slate-800">{value}</span>
      </div>
    );
  }

  return (
    <div className="py-2">
      <span className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">{label}</span>
      <details className="mt-1 rounded border border-slate-200 bg-slate-50 p-2">
        <summary className="cursor-pointer text-sm text-slate-700">Show value</summary>
        <p className="mt-2 break-all text-sm text-slate-800">{value}</p>
      </details>
    </div>
  );
}

export function NodeDetailPanel({ node, onClose }: NodeDetailPanelProps) {
  const { counts, workflowCounts, isLoading, isUnavailable } = useMetrics();

  if (!node) {
    return null;
  }

  const activityType =
    node.kind === "activity"
      ? firstDefinedString(getRawString(node, "activityType"), getRawString(node, "activityRef"))
      : undefined;
  const workflowRef =
    node.kind === "childWorkflow"
      ? firstDefinedString(getRawString(node, "workflowRef"), getRawString(node, "workflowName"))
      : undefined;
  const workflowMetricRef =
    node.kind === "bridge" || node.kind === "subflow"
      ? getRawString(node, "workflowName")
      : workflowRef;
  const bridgeRef = node.kind === "bridge" ? getRawString(node, "bridgeRef") : undefined;

  const metricCount =
    typeof activityType === "string"
      ? counts[activityType]
      : typeof workflowMetricRef === "string"
        ? workflowCounts[workflowMetricRef]
        : undefined;

  const hasMetrics = !isUnavailable && (isLoading || metricCount !== undefined);

  return (
    <section
      role="complementary"
      aria-label="Node Details"
      className="h-full overflow-y-auto p-4"
    >
      <div className="mb-4 flex items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold text-slate-900">{node.label}</h2>
          <span className="mt-1 inline-flex rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
            {node.kind}
          </span>
        </div>
        <button
          type="button"
          aria-label="Close details"
          onClick={onClose}
          className="rounded border border-slate-300 px-2 py-1 text-sm text-slate-700 hover:bg-slate-50"
        >
          ×
        </button>
      </div>

      {node.description ? (
        <div className="mb-4 rounded border border-slate-200 bg-slate-50 p-3">
          <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">Summary</h3>
          <p className="text-sm text-slate-700">{node.description}</p>
        </div>
      ) : null}

      <div className="rounded border border-slate-200 p-3">
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">Details</h3>
        <div className="divide-y divide-slate-200">
          <DetailRow label="Type" value={node.kind} />
          {activityType ? <DetailRow label="Activity type" value={activityType} /> : null}
          {workflowRef ? <DetailRow label="Workflow ref" value={workflowRef} /> : null}
          {bridgeRef ? <DetailRow label="Bridge ref" value={bridgeRef} /> : null}
          <DetailRow label="Node ID" value={node.id} />
        </div>
      </div>

      {hasMetrics ? (
        <div className="mt-4 rounded border border-slate-200 p-3">
          <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">Runs</h3>
          {isLoading ? (
            <p className="text-sm text-slate-600">Loading...</p>
          ) : (
            <p className="text-sm text-slate-800">{metricCount?.toLocaleString("en-US")}</p>
          )}
        </div>
      ) : null}
    </section>
  );
}
