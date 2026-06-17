import type { TimeRange } from "../services/metrics-client";

const OPTIONS: Array<{ value: TimeRange; label: string }> = [
  { value: "1h", label: "Letzte Stunde" },
  { value: "1d", label: "Letzter Tag" },
  { value: "7d", label: "Letzte Woche" },
  { value: "30d", label: "Letzter Monat" },
];

interface TimeRangeSelectorProps {
  value: TimeRange;
  onChange: (nextValue: TimeRange) => void;
}

export function TimeRangeSelector({ value, onChange }: TimeRangeSelectorProps) {
  return (
    <div className="inline-flex flex-wrap gap-1 rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
      {OPTIONS.map((option) => {
        const selected = option.value === value;
        return (
          <button
            key={option.value}
            type="button"
            onClick={() => onChange(option.value)}
            className={`rounded-md px-3 py-1.5 text-xs font-medium transition ${
              selected
                ? "bg-blue-600 text-white"
                : "bg-transparent text-slate-700 hover:bg-slate-100"
            }`}
            aria-pressed={selected}
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
