import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ApiError, api } from "../api/client";
import { Badge } from "../components/Badge";
import { KIND_LABELS, MODO_META } from "../lib/config";
import { useAuth } from "../auth/AuthContext";
import type { Listing } from "../api/types";

export function ListingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const [listing, setListing] = useState<Listing | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeImg, setActiveImg] = useState(0);

  useEffect(() => {
    if (!id) return;
    let active = true;
    setLoading(true);
    setError(null);
    api
      .listing(id)
      .then((d) => {
        if (active) {
          setListing(d);
          setActiveImg(0);
        }
      })
      .catch((e) => {
        if (active)
          setError(e instanceof ApiError ? e.message : "Anúncio não encontrado.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [id]);

  if (loading)
    return <div className="app-container text-silver">Carregando…</div>;

  if (error || !listing)
    return (
      <div className="app-container text-center">
        <p className="text-silver mb-4">{error ?? "Anúncio não encontrado."}</p>
        <Link to="/feed" className="text-esmeralda hover:underline">
          ← Voltar ao feed
        </Link>
      </div>
    );

  const gratis = listing.PrecoRvm === 0;
  const isDonation = listing.Modo === "Doar" || listing.Modo === "Voluntariar";
  const images = listing.Imagens?.length ? listing.Imagens : [];

  return (
    <div className="app-container">
      <Link to="/feed" className="text-sm text-silver hover:text-cream mb-4 inline-block">
        ← Feed
      </Link>

      <div className="grid lg:grid-cols-2 gap-8 lg:gap-12">
        {/* Galeria */}
        <div>
          <div className="aspect-square bg-smoke rounded-xl border border-smoke overflow-hidden flex items-center justify-center text-7xl">
            {images[activeImg] ? (
              <img
                src={images[activeImg]}
                alt={listing.Titulo}
                className="w-full h-full object-cover"
              />
            ) : (
              <span aria-hidden>📦</span>
            )}
          </div>
          {images.length > 1 && (
            <div className="mt-3 flex gap-2 overflow-x-auto">
              {images.map((src, i) => (
                <button
                  key={i}
                  onClick={() => setActiveImg(i)}
                  className={`w-16 h-16 shrink-0 rounded-lg overflow-hidden border-2 ${
                    i === activeImg ? "border-esmeralda" : "border-smoke"
                  }`}
                  aria-label={`Imagem ${i + 1}`}
                >
                  <img src={src} alt="" className="w-full h-full object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Info */}
        <div>
          <div className="flex items-center gap-2 mb-3">
            <Badge modo={listing.Modo} size="md" />
            <span className="text-sm text-silver">{KIND_LABELS[listing.Kind]}</span>
            {listing.NftTokenId && (
              <span className="text-xs text-silver border border-smoke rounded-full px-2 py-0.5">
                NFT #{listing.NftTokenId}
              </span>
            )}
          </div>

          <h1 className="text-3xl font-bold text-cream">{listing.Titulo}</h1>
          <div className="mt-3 text-2xl">
            {gratis ? (
              <span className="rms text-lima">Grátis</span>
            ) : (
              <span className="rms text-cream">
                RM$ {listing.PrecoRvm.toLocaleString("pt-BR")}
              </span>
            )}
          </div>

          <p className="mt-4 text-cream/90 whitespace-pre-wrap">{listing.Descricao}</p>

          <dl className="mt-6 space-y-2 text-sm">
            {listing.Kind === "Product" && listing.ProductDetails && (
              <div className="flex gap-2">
                <dt className="text-silver w-28">Condição:</dt>
                <dd className="text-cream">
                  {listing.ProductDetails.Condition}
                  {listing.ProductDetails.Stock != null
                    ? ` · ${listing.ProductDetails.Stock} un.`
                    : ""}
                </dd>
              </div>
            )}
            {listing.Kind === "Service" && listing.ServiceDetails && (
              <>
                <div className="flex gap-2">
                  <dt className="text-silver w-28">Unidade:</dt>
                  <dd className="text-cream">{listing.ServiceDetails.UnitType}</dd>
                </div>
                {listing.ServiceDetails.Duration != null && (
                  <div className="flex gap-2">
                    <dt className="text-silver w-28">Duração:</dt>
                    <dd className="text-cream">{listing.ServiceDetails.Duration}</dd>
                  </div>
                )}
                {listing.ServiceDetails.VoucherExpiryDays != null && (
                  <div className="flex gap-2">
                    <dt className="text-silver w-28">Voucher:</dt>
                    <dd className="text-cream">
                      {listing.ServiceDetails.VoucherExpiryDays} dias
                    </dd>
                  </div>
                )}
              </>
            )}
            {(listing.Cidade || listing.Bairro) && (
              <div className="flex gap-2">
                <dt className="text-silver w-28">Local:</dt>
                <dd className="text-cream">
                  {[listing.Bairro, listing.Cidade].filter(Boolean).join(", ")}
                </dd>
              </div>
            )}
          </dl>

          {/* Vendedor */}
          <div className="mt-6 flex items-center gap-3 bg-smoke rounded-xl p-3">
            {listing.VendedorAvatarUrl ? (
              <img
                src={listing.VendedorAvatarUrl}
                alt=""
                className="w-10 h-10 rounded-full"
              />
            ) : (
              <div className="w-10 h-10 rounded-full bg-charcoal border border-smoke" />
            )}
            <div>
              <div className="text-cream font-medium">{listing.VendedorNome}</div>
              <div className="text-xs text-silver">
                {MODO_META[listing.Modo].emoji} {MODO_META[listing.Modo].label}
              </div>
            </div>
          </div>

          {/* Comparativo de preço — placeholder Fase 3 */}
          <div className="mt-6 border border-dashed border-smoke rounded-xl p-4 text-sm text-silver">
            <span className="text-amber">⌖</span> Comparativo de preços em breve (Fase 3).
          </div>

          {/* CTA */}
          <div className="mt-6">
            {!user ? (
              <Link
                to="/login"
                className="block text-center bg-brand text-ink font-semibold px-6 py-3 rounded-xl"
              >
                Entre para oferecer ou pedir
              </Link>
            ) : (
              <button
                disabled
                className="block w-full text-center bg-charcoal text-silver border border-smoke px-6 py-3 rounded-xl cursor-not-allowed"
              >
                {isDonation
                  ? "Pedir"
                  : listing.Kind === "Service"
                  ? "Contratar"
                  : "Comprar/Trocar"}{" "}
                · integração de troca via tracker (em breve)
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
