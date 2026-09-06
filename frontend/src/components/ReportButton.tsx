import { useState } from "react";
import { ApiError, api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import type { ReportReason, ReportTarget } from "../api/types";

// Denunciar conteúdo (UF-24) — versão compacta para threads de post/comentário.
// Só aparece para usuários verificados (mesmo gate do backend); a partir daqui
// a moderação cuida do resto.
export function ReportButton({
  targetType,
  targetId,
  ownContent,
}: {
  targetType: ReportTarget;
  targetId: string;
  ownContent?: boolean;
}) {
  const { user } = useAuth();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState<ReportReason>("Spam");
  const [details, setDetails] = useState("");
  const [sending, setSending] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [sent, setSent] = useState(false);

  if (!user || !user.verified || ownContent || sent) return null;

  async function submit() {
    setSending(true);
    setErr(null);
    try {
      await api.createReport(targetType, targetId, reason, details.trim() || undefined);
      setSent(true);
    } catch (e) {
      setErr(e instanceof ApiError ? e.message : "Não foi possível enviar a denúncia.");
    } finally {
      setSending(false);
    }
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        title="Denunciar"
        aria-label="Denunciar"
        className="text-silver/70 hover:text-rosa"
      >
        ⚑
      </button>
    );
  }

  return (
    <div className="mt-2 border border-smoke rounded-lg p-2.5 space-y-2 bg-smoke/40">
      <div className="flex items-center justify-between">
        <span className="text-xs text-silver">Denunciar conteúdo</span>
        <button
          type="button"
          onClick={() => {
            setOpen(false);
            setDetails("");
            setErr(null);
          }}
          className="text-silver hover:text-cream text-xs"
          aria-label="Fechar"
        >
          ✕
        </button>
      </div>
      <select
        value={reason}
        onChange={(e) => setReason(e.target.value as ReportReason)}
        aria-label="Motivo da denúncia"
        className="w-full bg-smoke text-cream rounded-md border border-smoke focus:border-rosa px-2 py-1.5 outline-none text-xs"
      >
        <option value="Spam">Spam</option>
        <option value="Inappropriate">Inadequado</option>
        <option value="Scam">Golpe</option>
        <option value="Other">Outro</option>
      </select>
      {err && <p className="text-rosa text-xs">{err}</p>}
      <div className="flex gap-2">
        <button
          type="button"
          onClick={submit}
          disabled={sending}
          className="bg-rosa/90 text-ink text-xs font-semibold px-3 py-1.5 rounded-md disabled:opacity-60"
        >
          {sending ? "Enviando…" : "Denunciar"}
        </button>
      </div>
    </div>
  );
}
