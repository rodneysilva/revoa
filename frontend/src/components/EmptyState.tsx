import type { ReactNode } from "react";

// Estado vazio padrão (DS transversal): ícone + mensagem + dica + ação opcional.
// Substitui as cópias dispersas de "nada por aqui" — mesma caixa em todas as telas.
// `icon` é um ícone lucide (ex.: <ShoppingBag className="w-8 h-8" />).
// `compact` = linha fina (ícone à esquerda, ação à direita) para abas/seções
// densas, onde um card central de p-8 dominaria a tela.
export function EmptyState({
  icon,
  title,
  hint,
  action,
  compact,
}: {
  icon?: ReactNode;
  title: string;
  hint?: string;
  action?: ReactNode;
  compact?: boolean;
}) {
  if (compact) {
    return (
      <div className="bg-charcoal rounded-xl border border-smoke px-4 py-3 flex flex-wrap items-center gap-3">
        {icon && (
          <span className="text-silver/70 shrink-0" aria-hidden>
            {icon}
          </span>
        )}
        <p className="text-sm text-silver">
          {title}
          {hint && <span className="text-silver/60"> — {hint}</span>}
        </p>
        {action && <div className="ml-auto">{action}</div>}
      </div>
    );
  }

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
