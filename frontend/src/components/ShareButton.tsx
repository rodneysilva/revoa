import { useState } from "react";
import { Check, Share2 } from "lucide-react";

// Compartilhar (DS transversal): Web Share API nativa com fallback para
// clipboard ("Link copiado!"). Recebe a URL pronta ou usa a página atual.
export function ShareButton({
  url,
  label = "Compartilhar",
  className,
}: {
  url?: string;
  label?: string;
  className?: string;
}) {
  const [copied, setCopied] = useState(false);

  async function share() {
    const target = url ?? window.location.href;
    if (navigator.share) {
      try {
        await navigator.share({ url: target });
        return;
      } catch {
        /* usuário cancelou o sheet nativo — não faz nada */
      }
    }
    try {
      await navigator.clipboard.writeText(target);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      /* clipboard bloqueado pelo browser — silêncio */
    }
  }

  return (
    <button
      type="button"
      onClick={share}
      className={className ?? "inline-flex items-center gap-1.5 text-silver hover:text-esmeralda"}
    >
      {copied ? (
        <>
          <Check aria-hidden className="w-3.5 h-3.5" />
          Link copiado!
        </>
      ) : (
        <>
          <Share2 aria-hidden className="w-3.5 h-3.5" />
          {label}
        </>
      )}
    </button>
  );
}
