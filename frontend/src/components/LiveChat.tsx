import { useEffect, useRef, useState, type FormEvent } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { buildCommunityHub } from "../api/signalr";
import { getToken } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { Avatar } from "./Avatar";
import { timeAgo } from "../lib/time";
import type { ChatMessage } from "../api/types";

type ConnState = "connecting" | "online" | "reconnecting" | "error";

function errMsg(e: unknown): string {
  return e instanceof Error ? e.message : "Erro na conversa ao vivo.";
}

export function LiveChat({
  communityId,
  isMember,
}: {
  communityId: string;
  isMember: boolean;
}) {
  const { user } = useAuth();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [text, setText] = useState("");
  const [state, setState] = useState<ConnState>("connecting");
  const [error, setError] = useState<string | null>(null);
  const connRef = useRef<HubConnection | null>(null);
  const bottomRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!isMember) return;
    let active = true;
    const conn = buildCommunityHub(communityId, getToken());
    connRef.current = conn;

    conn.on("ReceiveMessage", (dto: ChatMessage) => {
      setMessages((prev) => [...prev, dto]);
    });
    conn.onreconnecting(() => {
      if (active) setState("reconnecting");
    });
    conn.onreconnected(() => {
      if (active) setState("online");
    });
    conn.onclose(() => {
      if (active) setState("error");
    });

    setState("connecting");
    conn
      .start()
      .then(() => {
        if (active) setState("online");
      })
      .catch((e) => {
        if (active) {
          setState("error");
          setError(errMsg(e));
        }
      });

    return () => {
      active = false;
      conn.stop().catch(() => {});
      connRef.current = null;
    };
  }, [communityId, isMember]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  async function send(e: FormEvent) {
    e.preventDefault();
    const c = text.trim();
    if (!c || !connRef.current) return;
    setText("");
    setError(null);
    try {
      await connRef.current.invoke("SendMessage", communityId, c);
    } catch (e) {
      setError(errMsg(e));
      setText(c);
    }
  }

  const canSend = isMember && !!user?.verified && state === "online";
  const dotCls =
    state === "online" ? "bg-esmeralda" : state === "error" ? "bg-rosa" : "bg-amber";
  const stateLabel =
    state === "online"
      ? "Ao vivo"
      : state === "error"
      ? "Desconectado"
      : state === "reconnecting"
      ? "Reconectando…"
      : "Conectando…";

  return (
    <div className="flex flex-col h-[26rem] bg-charcoal border border-smoke rounded-xl overflow-hidden">
      <div className="flex items-center gap-2 px-4 py-2.5 border-b border-smoke">
        {isMember && (
          <>
            <span
              className={`w-2 h-2 rounded-full ${dotCls} ${
                state !== "online" && state !== "error" ? "animate-pulse" : ""
              }`}
            />
            <span className="text-sm text-silver">{stateLabel}</span>
          </>
        )}
        <span className={`text-sm text-cream font-semibold ${isMember ? "ml-auto text-xs text-silver font-normal" : ""}`}>
          {isMember ? `${messages.length} nesta sessão` : "Conversa da comunidade"}
        </span>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-3 space-y-3">
        {messages.length === 0 ? (
          <p className="text-sm text-silver text-center mt-8">
            As mensagens enviadas agora aparecem aqui. Comece a conversa! 👋
          </p>
        ) : (
          messages.map((m) => (
            <div key={m.Id} className="flex gap-2.5">
              <Avatar name={m.AuthorName} src={m.AutorAvatarUrl} size={28} />
              <div className="min-w-0">
                <div className="flex items-baseline gap-2">
                  <span className="text-sm font-semibold text-cream">{m.AuthorName}</span>
                  {user && m.AutorId === user.userId && (
                    <span className="text-[10px] text-esmeralda">você</span>
                  )}
                  <span className="text-[10px] text-silver">{timeAgo(m.CreatedAt)}</span>
                </div>
                <p className="text-sm text-cream/90 whitespace-pre-wrap break-words">
                  {m.Content}
                </p>
              </div>
            </div>
          ))
        )}
        <div ref={bottomRef} />
      </div>

      {error && (
        <div className="px-4 py-1.5 text-xs text-rosa border-t border-smoke bg-rosa/5">
          {error}
        </div>
      )}

      {!isMember ? (
        <div className="px-4 py-3 border-t border-smoke text-sm text-silver text-center">
          Entre na comunidade pra conversar. 🤝
        </div>
      ) : !user?.verified ? (
        <div className="px-4 py-3 border-t border-smoke text-sm text-silver text-center">
          Confirme e-mail e telefone para conversar.
        </div>
      ) : (
        <form onSubmit={send} className="flex gap-2 p-3 border-t border-smoke">
          <input
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder="Mensagem…"
            disabled={state !== "online"}
            className="flex-1 bg-smoke text-cream rounded-lg border border-smoke focus:border-esmeralda px-3 py-2 outline-none text-sm disabled:opacity-60"
          />
          <button
            type="submit"
            disabled={!canSend || !text.trim()}
            className="bg-brand text-ink text-sm font-semibold px-4 rounded-lg disabled:opacity-60"
          >
            Enviar
          </button>
        </form>
      )}
    </div>
  );
}
