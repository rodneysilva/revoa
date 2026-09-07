import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { BadgeCheck, Search } from "lucide-react";
import { api } from "../api/client";
import { Avatar } from "../components/Avatar";
import { EmptyState } from "../components/EmptyState";
import {
  ProfileActivity,
  ProfileCommunities,
  ProfileListings,
  ProfileReputation,
} from "../components/ProfileActivity";
import type { PublicProfile } from "../api/types";

// Perfil público (/users/:id): quem é, o que anuncia, onde participa e o que
// postou em comunidades abertas. Contato (e-mail/telefone) nunca vem do backend.
export function UserPage() {
  const { id } = useParams<{ id: string }>();
  const [profile, setProfile] = useState<PublicProfile | null>(null);
  const [missing, setMissing] = useState(false);

  useEffect(() => {
    if (!id) return;
    let active = true;
    setProfile(null);
    setMissing(false);
    api
      .publicProfile(id)
      .then((p) => active && setProfile(p))
      .catch(() => active && setMissing(true));
    return () => {
      active = false;
    };
  }, [id]);

  if (missing) {
    return (
      <div className="app-container">
        <div className="app-read">
          <EmptyState
            icon={<Search className="w-8 h-8" />}
            title="Perfil não encontrado"
            hint="O link pode estar quebrado ou a conta não existe mais."
            action={
              <Link to="/feed" className="bg-brand text-ink font-semibold px-5 py-2 rounded-xl inline-block">
                Ir para o feed
              </Link>
            }
          />
        </div>
      </div>
    );
  }

  if (!profile || !id) {
    return (
      <div className="app-container">
        <div className="app-read">
          <div className="h-24 animate-pulse bg-charcoal rounded-2xl" />
        </div>
      </div>
    );
  }

  const membroDesde = profile.MemberSince
    ? new Date(profile.MemberSince).toLocaleDateString("pt-BR", {
        month: "long",
        year: "numeric",
      })
    : null;

  return (
    <div className="app-container">
      <header className="flex items-center gap-4 mb-6">
        <Avatar name={profile.Name || "?"} size={64} />
        <div>
          <h1 className="flex flex-wrap items-center gap-2 text-2xl font-bold text-cream">
            {profile.Name}
            {profile.Verified && (
              <span
                className="inline-flex items-center gap-1 text-esmeralda text-sm font-semibold"
                title="E-mail e telefone confirmados"
              >
                <BadgeCheck aria-hidden className="w-4 h-4" />
                Verificado
              </span>
            )}
          </h1>
          {membroDesde && (
            <p className="text-sm text-silver">Membro desde {membroDesde}</p>
          )}
        </div>
      </header>

      <ProfileListings userId={id} />
      <ProfileCommunities userId={id} />
      <ProfileActivity userId={id} />
      <ProfileReputation userId={id} />
    </div>
  );
}
