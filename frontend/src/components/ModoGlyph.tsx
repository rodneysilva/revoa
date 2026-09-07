import { MODO_META } from "../lib/config";
import type { Mode } from "../api/types";

// Ícone do modo do anúncio (Trocar/Repassar/Doar/Voluntariar) — lucide,
// herda a cor do texto circundante.
export function ModoGlyph({
  mode,
  className = "w-4 h-4",
}: {
  mode: Mode;
  className?: string;
}) {
  const Icon = MODO_META[mode].icon;
  return <Icon aria-hidden className={className} />;
}
