// §15 Diagnostics panel — collapsible, non-blocking.
// Default: expanded when any warning or error is present, collapsed when all info.
import { useState } from "react";
import { Info, TriangleAlert, OctagonAlert } from "lucide-react";
import type { WorkflowGraphDiagnostic } from "../workflowapi/graphTypes";

export interface DiagnosticsPanelProps {
  diagnostics: WorkflowGraphDiagnostic[];
}

export default function DiagnosticsPanel({
  diagnostics,
}: DiagnosticsPanelProps): JSX.Element | null {
  const hasUrgent = diagnostics.some(
    (d) => d.severity === "warning" || d.severity === "error"
  );
  const [expanded, setExpanded] = useState(hasUrgent);

  if (diagnostics.length === 0) return null;

  const errors = diagnostics.filter((d) => d.severity === "error").length;
  const warnings = diagnostics.filter((d) => d.severity === "warning").length;
  const infos = diagnostics.filter((d) => d.severity === "info").length;

  return (
    <div className="border-t bg-slate-50">
      <button
        type="button"
        className="flex w-full items-center gap-2 px-4 py-2 text-sm text-slate-700 hover:bg-slate-100"
        onClick={() => setExpanded((e) => !e)}
      >
        <span className="font-medium">Diagnostics</span>
        {errors > 0 && (
          <span className="rounded bg-red-100 px-1.5 py-0.5 text-xs font-semibold text-red-700">
            {errors} error{errors !== 1 ? "s" : ""}
          </span>
        )}
        {warnings > 0 && (
          <span className="rounded bg-amber-100 px-1.5 py-0.5 text-xs font-semibold text-amber-700">
            {warnings} warning{warnings !== 1 ? "s" : ""}
          </span>
        )}
        {infos > 0 && (
          <span className="rounded bg-blue-100 px-1.5 py-0.5 text-xs font-semibold text-blue-700">
            {infos} info
          </span>
        )}
        <span className="ml-auto text-slate-400">{expanded ? "▲" : "▼"}</span>
      </button>

      {expanded && (
        <ul className="max-h-48 overflow-auto divide-y divide-slate-100">
          {diagnostics.map((d, i) => (
            <li key={i} className="flex items-start gap-2 px-4 py-2 text-sm">
              {d.severity === "error" && (
                <OctagonAlert className="mt-0.5 h-4 w-4 shrink-0 text-red-600" />
              )}
              {d.severity === "warning" && (
                <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
              )}
              {d.severity === "info" && (
                <Info className="mt-0.5 h-4 w-4 shrink-0 text-blue-600" />
              )}
              <div className="min-w-0 flex-1">
                <p className="text-slate-700 break-words">{d.message}</p>
                {d.sourcePath && (
                  <p className="mt-0.5 font-mono text-xs text-slate-400 break-all">
                    at {d.sourcePath}
                  </p>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
