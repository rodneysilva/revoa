import { useEffect, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client";
import { CommunityCard } from "./CommunityCard";
import { EmptyState } from "./EmptyState";
import { ListingCard } from "./ListingCard";
import { Avatar } from "./Avatar";
import type { Community, FeedItem, Reputation, Review, SocialFeedItem } from "../api/types";

// Seções do perfil (anúncios/comunidades/atividade/reputação) — compartilhadas
// entre o /perfil (dono) e o /users/:id (público). Cada seção carrega sozinha
// e degrada em silêncio: perfil vazio não é erro.

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

export function ProfileListings({ userId, self }: { userId: string; self?: boolean }) {
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

  return (
    <Section title="Anúncios">
      {items.length === 0 ? (
        <EmptyState
          icon="📦"
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
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
          {items.map((i) => (
            <ListingCard key={i.Id} item={i} />
          ))}
        </div>
      )}
    </Section>
  );
}

export function ProfileCommunities({ userId }: { userId: string }) {
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

  return (
    <Section title="Comunidades">
      {items.length === 0 ? (
        <EmptyState
          icon="🏘️"
          title="Ainda não participa de comunidades"
          hint="Comunidades são onde a troca hiperlocal acontece."
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {items.map((c) => (
            <CommunityCard key={c.Id} c={c} />
          ))}
        </div>
      )}
    </Section>
  );
}

export function ProfileActivity({ userId }: { userId: string }) {
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

  return (
    <Section title="Atividade" hint="Posts em comunidades abertas">
      {items.length === 0 ? (
        <EmptyState icon="💬" title="Nenhuma atividade recente" />
      ) : (
        <ul className="space-y-2">
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
      )}
    </Section>
  );
}

function Stars({ n }: { n: number }) {
  const v = Math.max(0, Math.min(5, n));
  return (
    <span className="text-amber text-sm tracking-tight" aria-label={`${v} de 5 estrelas`}>
      {"★".repeat(v)}
      <span className="text-smoke">{"★".repeat(5 - v)}</span>
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

  const hasRep = reputation != null;
  if (!hasRep && reviews.length === 0) {
    return (
      <Section title="Reputação">
        <EmptyState
          icon="🌱"
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
