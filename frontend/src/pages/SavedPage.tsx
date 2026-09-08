import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Bookmark, MessageCircle, ShoppingBag } from "lucide-react";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { EmptyState } from "../components/EmptyState";
import { ListingCard } from "../components/ListingCard";
import { PostCard } from "../components/PostCard";
import type { FeedItem, Post } from "../api/types";

// 🔖 Meus salvos: posts e anúncios que o usuário guardou para depois.
// Privada por natureza (as APIs leem o UserId do token).
export function SavedPage() {
  const { user } = useAuth();
  const [posts, setPosts] = useState<Post[] | null>(null);
  const [listings, setListings] = useState<FeedItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    Promise.all([api.savedPosts(), api.savedListings()])
      .then(([p, l]) => {
        if (!active) return;
        setPosts(p);
        setListings(l);
      })
      .catch(() => {
        if (active) {
          setError("Não foi possível carregar seus salvos agora.");
          setPosts([]);
          setListings([]);
        }
      });
    return () => {
      active = false;
    };
  }, []);

  function removePost(postId: string) {
    setPosts((prev) => (prev ?? []).filter((p) => p.Id !== postId));
  }

  const loading = posts === null || listings === null;
  const vazio = !loading && (posts?.length ?? 0) + (listings?.length ?? 0) === 0;

  return (
    <div className="app-container">
      <div className="flex flex-col sm:flex-row sm:items-end gap-3 mb-6">
        <div>
          <h1 className="flex items-center gap-2 text-2xl sm:text-3xl font-bold text-cream">
            <Bookmark aria-hidden className="w-6 h-6" /> Salvos
          </h1>
          <p className="text-sm text-silver">
            Posts e anúncios que você guardou para depois — só você vê esta lista.
          </p>
        </div>
        <Link
          to="/feed"
          className="text-sm text-esmeralda hover:underline sm:ml-auto sm:mt-1"
        >
          ← Voltar ao feed
        </Link>
      </div>

      {error && (
        <div className="bg-smoke border border-smoke text-silver rounded-xl p-4 text-sm mb-6">
          {error}
        </div>
      )}

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-28 bg-smoke rounded-xl animate-pulse" />
          ))}
        </div>
      ) : vazio ? (
        <EmptyState
          icon={<Bookmark className="w-8 h-8" />}
          title="Nada salvo ainda."
          hint="Use Salvar em posts e anúncios para achá-los aqui depois."
          action={
            <Link
              to="/feed"
              className="inline-block bg-brand text-ink font-semibold px-5 py-2.5 rounded-xl"
            >
              Explorar o feed
            </Link>
          }
        />
      ) : (
        <div className="space-y-10">
          {(posts?.length ?? 0) > 0 && (
            <section>
              <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-3">
                <span className="inline-flex items-center gap-1.5">
                  <MessageCircle aria-hidden className="w-3.5 h-3.5" /> Posts salvos ({posts!.length})
                </span>
              </h2>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 items-start">
                {posts!.map((p) => (
                  <PostCard
                    key={p.Id}
                    post={p}
                    currentUserId={user?.userId}
                    communityUrl={`/community/${p.CommunityId}#conversas`}
                    onUnsave={() => removePost(p.Id)}
                  />
                ))}
              </div>
            </section>
          )}

          {(listings?.length ?? 0) > 0 && (
            <section>
              <h2 className="text-xs font-bold uppercase tracking-wider text-silver mb-3">
                <span className="inline-flex items-center gap-1.5">
                  <ShoppingBag aria-hidden className="w-3.5 h-3.5" /> Anúncios salvos ({listings!.length})
                </span>
              </h2>
              <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6">
                {listings!.map((item) => (
                  <ListingCard key={item.Id} item={item} />
                ))}
              </div>
            </section>
          )}
        </div>
      )}
    </div>
  );
}
