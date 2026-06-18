import { formatCompactNumber } from "../../utils/format-number";

interface RunCountLabelProps {
  count?: number;
  p95Seconds?: number;
  isLoading: boolean;
  isUnavailable: boolean;
  className?: string;
}

function formatLatency(seconds: number): string {
  const ms = seconds * 1000;
  return ms < 10_000 ? `${Math.round(ms)} ms` : `${seconds.toFixed(1)} s`;
}

export function RunCountLabel({
  count,
  p95Seconds,
  isLoading,
  isUnavailable,
  className,
}: RunCountLabelProps) {
  if (isUnavailable) return null;

  if (isLoading) {
    return <p className={className}>… runs</p>;
  }

  if (count === undefined && p95Seconds === undefined) return null;

  return (
    <p className={className}>
      {count !== undefined ? `${formatCompactNumber(count)} runs` : null}
      {count !== undefined && p95Seconds !== undefined ? " · " : null}
      {p95Seconds !== undefined ? `p95: ${formatLatency(p95Seconds)}` : null}
    </p>
  );
}
