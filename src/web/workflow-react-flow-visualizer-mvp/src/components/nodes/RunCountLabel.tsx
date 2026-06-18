import { formatCompactNumber } from "../../utils/format-number";

interface RunCountLabelProps {
  count?: number;
  isLoading: boolean;
  isUnavailable: boolean;
  className?: string;
}

export function RunCountLabel({
  count,
  isLoading,
  isUnavailable,
  className,
}: RunCountLabelProps) {
  if (isUnavailable) return null;

  if (isLoading) {
    return <p className={className}>... runs</p>;
  }

  if (count === undefined) return null;

  return <p className={className}>{formatCompactNumber(count)} runs</p>;
}
