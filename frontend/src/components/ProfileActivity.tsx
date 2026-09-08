import { useEffect, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { Bookmark, House, MessageCircle, Package, Sprout, Star } from "lucide-react";
import { api } from "../api/client";
import { CommunityCard } from "./CommunityCard";
import { EmptyState } from "./EmptyState";
import { ListingCard } from "./ListingCard";
import { Avatar } from "./Avatar";
import type { Community, FeedItem, Reputation, Review, SocialFeedItem } from "../api/types";

// Seções do perfil (anúncios/comunidades/atividade/reputação) — compartilhadas
// entre o /profile (dono, em abas) e o /users/:id (público, empilhado).
// `embedded` = sem título/hint (a aba do /profile já dá o contexto), grades
// estendidas até o 4K (3xl/4xl) e estados vazios compactos.
// Cada seção carrega sozinha e degrada em silêncio: perfil vazio não é erro.

function Section({
  title,
  hint,
  children,
}: {
  title: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <section className="mt-6">
      <h2 className="text-cream font-semibold mb-1">{title}</h2>
      {hint && <p className="text-xs text-silver mb-3">{hint}</p>}
      {children}
    </section>
  );
}

export function ProfileListings({
  userId,
  self,
  embedded,
}: {
  userId: string;
  self?: boolean;
  embedded?: boolean;
}) {
  const [items, setItems] = useState<FeedItem[] | null>(null);

  useEffect(() => {
    let active = true;
    api
      .feed({ sellerIds: userId, page: 1 })
      .then((r) => active && setItems(r))
      .catch(() => active && setItems([]));
    return () => {
      active = false;
    };
  }, [userId]);

  if (items === null) return null;

  const content = items.length === 0 ? (
    <EmptyState
      compact={embedded}
      icon={<Package className={embedded ? "w-5 h-5" : "w-8 h-8"} />}
      title={self ? "Você ainda não publicou nada" : "Nenhum anúncio publicado"}
      hint={self ? "Anuncie um item ou serviço — leva menos de um minuto." : undefined}
      action={
        self ? (
          <Link to="/listings/new" className="bg-brand text-ink font-semibold px-5 py-2 rounded-xl inline-block">
            Anunciar
          </Link>
        ) : undefined
      }
    />
  ) : (
    <div
      className={
        embedded
          ? "grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 3xl:grid-cols-7 4xl:grid-cols-8 gap-3"
          : "grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-3"
      }
    >
      {items.map((i) => (
        <ListingCard key={i.Id} item={i} />
      ))}
    </div>
  );

  return embedded ? content : <Section title="Anúncios">{content}</Section>;
}

// Anúncios salvos (bookmark privado — só no perfil próprio).
export function ProfileSaved({ embedded }: { embedded?: boolean }) {
  const [items, setItems] = useState<FeedItem[] | null>(null);

  useEffect(() => {
    let active = true;
    api
      .savedListings()
      .then((r) => active && setItems(r))
      .catch(() => active && setItems([]));
    return () => {
      active = false;
    };
  }, []);

  if (items === null) return null;

  const content = items.length === 0 ? (
    <EmptyState
      compact={embedded}
      icon={<Bookmark className={embedded ? "w-5 h-5" : "w-8 h-8"} />}
      title="Nenhum anúncio salvo"
      hint="Use o botão Salvar em um anúncio para achá-lo aqui."
      action={
        <Link to="/feed" className="bg-brand text-ink font-semibold px-5 py-2 rounded-xl inline-block">
          Explorar o feed
        </Link>
      }
    />
  ) : (
    <div
      className={
        embedded
          ? "grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 3xl:grid-cols-7 4xl:grid-cols-8 gap-3"
          : "grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6 gap-3"
      }
    >
      {items.map((i) => (
        <ListingCard key={i.Id} item={i} />
      ))}
    </div>
  );

  return embedded ? content : <Section title="Salvos" hint="Anúncios que você guardou para depois">{content}</Section>;
}

export function ProfileCommunities({ userId, embedded }: { userId: string; embedded?: boolean }) {
  const [items, setItems] = useState<Community[] | null>(null);

  useEffect(() => {
    let active = true;
    api
      .userCommunities(userId)
      .then((r) => active && setItems(r))
      .catch(() => active && setItems([]));
    return () => {
      active = false;
    };
  }, [userId]);

  if (items === null) return null;

  const content = items.length === 0 ? (
    <EmptyState
      compact={embedded}
      icon={<House className={embedded ? "w-5 h-5" : "w-8 h-8"} />}
      title="Ainda não participa de comunidades"
      hint="Comunidades são onde a troca hiperlocal acontece."
      action={
        embedded ? (
          <Link to="/community" className="bg-brand text-ink font-semibold px-5 py-2 rounded-xl inline-block">
            Descobrir comunidades
          </Link>
        ) : undefined
      }
    />
  ) : (
    <div
      className={
        embedded
          ? "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4 4xl:grid-cols-5 gap-3"
          : "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3"
      }
    >
      {items.map((c) => (
        <CommunityCard key={c.Id} c={c} />
      ))}
    </div>
  );

  return embedded ? content : <Section title="Comunidades">{content}</Section>;
}

export function ProfileActivity({ userId, embedded }: { userId: string; embedded?: boolean }) {
  const [items, setItems] = useState<SocialFeedItem[] | null>(null);

  useEffect(() => {
    let active = true;
    api
      .userPosts(userId)
      .then((r) => active && setItems(r))
      .catch(() => active && setItems([]));
    return () => {
      active = false;
    };
  }, [userId]);

  if (items === null) return null;

  const content = items.length === 0 ? (
    <EmptyState
      compact={embedded}
      icon={<MessageCircle className={embedded ? "w-5 h-5" : "w-8 h-8"} />}
      title="Nenhuma atividade recente"
      hint="Poste em uma comunidade para movimentar seu perfil."
    />
  ) : (
    <ul
      className={
        embedded
          ? "grid grid-cols-1 gap-2 md:grid-cols-2 2xl:grid-cols-3 4xl:grid-cols-4"
          : "grid grid-cols-1 gap-2 md:grid-cols-2 2xl:grid-cols-3"
      }
    >
      {items.map(({ Post, Community }) => (
        <li key={Post.Id}>
          <Link
            to={`/community/${Community.Id}`}
            className="block bg-charcoal rounded-xl border border-smoke p-3 hover:border-esmeralda transition"
          >
            <p className="text-cream text-sm line-clamp-3">{Post.Content}</p>
            <p className="mt-1 text-xs text-silver">
              em {Community.Name} ·{" "}
              {new Date(Post.CreatedAt).toLocaleDateString("pt-BR", {
                day: "numeric",
                month: "short",
              })}
            </p>
          </Link>
        </li>
      ))}
    </ul>
  );

  return embedded ? content : <Section title="Atividade" hint="Posts em comunidades abertas">{content}</Section>;
}

// Estrelas de avaliação — também usadas na faixa de resumo do /profile.
export function Stars({ n }: { n: number }) {
  const v = Math.max(0, Math.min(5, n));
  return (
    <span className="inline-flex items-center gap-0.5 text-amber" aria-label={`${v} de 5 estrelas`}>
      {Array.from({ length: 5 }, (_, i) => (
        <Star
          key={i}
          aria-hidden
          className={`w-3.5 h-3.5 ${i < v ? "fill-current" : "text-smoke"}`}
        />
      ))}
    </span>
  );
}

// Reputação + últimas avaliações recebidas (auto-carregável).
export function ProfileReputation({ userId }: { userId: string }) {
  const [reputation, setReputation] = useState<Reputation | null>(null);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    setLoading(true);
    Promise.all([
      api.reputation(userId).catch(() => null),
      api.userReviews(userId).catch(() => [] as Review[]),
    ])
      .then(([rep, revs]) => {
        if (!active) return;
        setReputation(rep);
        setReviews(revs);
      })
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, [userId]);

  if (loading) return null;

  // A API agora devolve 200 com zeros para quem nunca trocou (não mais 404) —
  // reputação "existente" é a que tem algum histórico atrás.
  const hasRep =
    reputation != null &&
    (reputation.Points > 0 ||
      reputation.ReviewsCount > 0 ||
      reputation.DonationsCount > 0 ||
      reputation.VolunteerCount > 0);
  if (!hasRep && reviews.length === 0) {
    return (
      <Section title="Reputação">
        <EmptyState
          icon={<Sprout className="w-8 h-8" />}
          title="Reputação em construção"
          hint="Surge conforme a pessoa troca, doa e ajuda."
        />
      </Section>
    );
  }

  const reviewsAvg =
    reviews.length > 0
      ? reviews.reduce((s, r) => s + r.Rating, 0) / reviews.length
      : reputation!.AvgRating;
  const reviewsCount = hasRep ? reputation!.ReviewsCount : reviews.length;

  return (
    <Section title="Reputação">
      <div className="bg-charcoal rounded-2xl border border-smoke p-5">
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
          {hasRep && (
            <div>
              <div className="text-xs text-silver">Nível</div>
              <div className="text-esmeralda font-bold">{reputation!.Level}</div>
              <div className="text-xs text-silver/70">{reputation!.Points} pts</div>
            </div>
          )}
          <div>
            <div className="text-xs text-silver">Avaliações</div>
            <div className="flex items-center gap-1">
              <Stars n={Math.round(reviewsAvg)} />
            </div>
            <div className="text-xs text-silver/70">
              {reviewsCount > 0 ? `${reviewsCount} avaliaç${reviewsCount === 1 ? "ão" : "ões"}` : "—"}
            </div>
          </div>
          {hasRep && (
            <div>
              <div className="text-xs text-silver">Doações</div>
              <div className="text-terracota font-bold">{reputation!.DonationsCount}</div>
            </div>
          )}
          {hasRep && reputation!.VolunteerCount > 0 && (
            <div>
              <div className="text-xs text-silver">Voluntariado</div>
              <div className="text-lima font-bold">{reputation!.VolunteerCount}</div>
            </div>
          )}
        </div>

        {reviews.length > 0 && (
          <ul className="mt-4 pt-4 border-t border-smoke space-y-3">
            {reviews.slice(0, 3).map((r) => (
              <li key={r.Id} className="flex items-start gap-3">
                <Avatar name={r.ReviewerName} size={28} />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-cream text-sm">{r.ReviewerName}</span>
                    <Stars n={r.Rating} />
                  </div>
                  {r.Comment && <p className="text-sm text-silver truncate">{r.Comment}</p>}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </Section>
  );
}
