import { MODO_META } from "../lib/config";
import type { Mode } from "../api/types";

export function Badge({ modo, size = "sm" }: { modo: Mode; size?: "sm" | "md" }) {
  const meta = MODO_META[modo];
  const Icon = meta.icon;
  const pad = size === "md" ? "px-3 py-1.5 text-sm" : "px-2 py-0.5 text-xs";
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full font-semibold ${meta.bg} ${meta.text} ${pad}`}
    >
      <Icon aria-hidden className={size === "md" ? "w-4 h-4" : "w-3 h-3"} />
      {meta.badge}
    </span>
  );
}
