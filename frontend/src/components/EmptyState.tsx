import type { ReactNode } from "react";

// Estado vazio padrão (DS transversal): ícone + mensagem + dica + ação opcional.
// Substitui as cópias dispersas de "nada por aqui" — mesma caixa em todas as telas.
// `icon` é um ícone lucide (ex.: <ShoppingBag className="w-8 h-8" />).
export function EmptyState({
  icon,
  title,
  hint,
  action,
}: {
  icon?: ReactNode;
  title: string;
  hint?: string;
  action?: ReactNode;
}) {
  return (
    <div className="bg-charcoal rounded-xl border border-smoke p-8 text-center">
      {icon && (
        <div className="mb-2 flex justify-center text-silver/70" aria-hidden>
          {icon}
        </div>
      )}
      <p className="text-silver">{title}</p>
      {hint && <p className="mt-1 text-sm text-silver/70">{hint}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
