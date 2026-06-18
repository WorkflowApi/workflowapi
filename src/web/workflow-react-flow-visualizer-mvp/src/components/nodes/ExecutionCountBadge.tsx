import { formatCompactNumber } from "../../utils/format-number";

interface ExecutionCountBadgeProps {
  count?: number;
  isLoading: boolean;
  isUnavailable: boolean;
}

export function ExecutionCountBadge({
  count,
  isLoading,
  isUnavailable,
}: ExecutionCountBadgeProps) {
  if (isLoading) {
    return (
      <span
        aria-label="Run count is loading"
        className="inline-flex min-w-8 items-center justify-center rounded-full bg-slate-200 px-2 py-0.5 text-[10px] font-semibold text-slate-600 animate-pulse"
      >
        …
      </span>
    );
  }

  if (isUnavailable || count === undefined) {
    return (
      <span
        aria-label="Run count unavailable"
        className="inline-flex min-w-8 items-center justify-center rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-semibold text-slate-500"
      >
        –
      </span>
    );
  }

  return (
    <span
      title={count.toLocaleString("en-US")}
      aria-label={`Run count: ${count.toLocaleString("en-US")}`}
      className="inline-flex min-w-8 items-center justify-center rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold text-emerald-700"
    >
      {formatCompactNumber(count)}
    </span>
  );
}
