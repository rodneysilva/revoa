import type { ReactNode } from "react";

// Estado vazio padrão (DS transversal): ícone + mensagem + dica + ação opcional.
// Substitui as cópias dispersas de "nada por aqui" — mesma caixa em todas as telas.
export function EmptyState({
  icon,
  title,
  hint,
  action,
}: {
  icon?: string;
  title: string;
  hint?: string;
  action?: ReactNode;
}) {
  return (
    <div className="bg-charcoal rounded-xl border border-smoke p-8 text-center">
      {icon && (
        <div className="text-3xl mb-2" aria-hidden>
          {icon}
        </div>
      )}
      <p className="text-silver">{title}</p>
      {hint && <p className="mt-1 text-sm text-silver/70">{hint}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
