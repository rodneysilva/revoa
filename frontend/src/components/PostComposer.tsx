import { useState } from "react";
import { Avatar } from "./Avatar";

// Composer de post (DS): pílula com avatar + "No que você está pensando?" que
// expande em textarea + Publicar. É o topo da coluna de conversas — o espaço
// de escrever vem antes da lista, não no fim da página.
export function PostComposer({
  authorName,
  authorAvatar,
  placeholder,
  onSubmit,
  disabled,
  disabledHint,
}: {
  authorName: string;
  authorAvatar?: string;
  placeholder?: string;
  onSubmit: (conteudo: string) => Promise<void>;
  disabled?: boolean;
  disabledHint?: string;
}) {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const [sending, setSending] = useState(false);

  async function submit() {
    const conteudo = text.trim();
    if (!conteudo || sending) return;
    setSending(true);
    try {
      await onSubmit(conteudo);
      setText("");
      setOpen(false);
    } finally {
      setSending(false);
    }
  }

  if (disabled) {
    return (
      <div className="rounded-xl bg-charcoal/60 border border-smoke px-4 py-3 text-sm text-silver">
        {disabledHint ?? "Entre para participar das conversas."}
      </div>
    );
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="w-full flex items-center gap-3 rounded-xl bg-charcoal/60 border border-smoke px-4 py-3 text-left hover:border-esmeralda/60 transition"
      >
        <Avatar name={authorName} src={authorAvatar} size={36} />
        <span className="flex-1 text-sm text-silver truncate">
          {placeholder ?? `No que você está pensando, ${authorName.split(" ")[0]}?`}
        </span>
        <span className="bg-brand text-ink text-sm font-semibold px-3 py-1.5 rounded-lg whitespace-nowrap">
          Publicar
        </span>
      </button>
    );
  }

  return (
    <div className="rounded-xl bg-charcoal/60 border border-smoke px-4 py-3">
      <div className="flex gap-3">
        <Avatar name={authorName} src={authorAvatar} size={36} />
        <div className="flex-1 min-w-0">
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            rows={3}
            autoFocus
            placeholder="Compartilhe com a comunidade…"
            className="w-full bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm resize-y"
          />
          <div className="mt-2 flex gap-2 justify-end">
            <button
              type="button"
              onClick={() => setOpen(false)}
              className="text-silver hover:text-cream text-sm px-3 py-1.5"
            >
              Cancelar
            </button>
            <button
              type="button"
              onClick={submit}
              disabled={sending || !text.trim()}
              className="bg-brand text-ink text-sm font-semibold px-4 py-1.5 rounded-lg disabled:opacity-60"
            >
              {sending ? "Publicando…" : "Publicar"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
