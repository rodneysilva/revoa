// Cliente fetch tipado. Lê o token JWT do localStorage e envia como
// Authorization: Bearer. Usa paths relativos (proxy do Vite resolve CORS).

import type {
  AdminParameter,
  AppNotification,
  BrlRate,
  Category,
  ChatMessage,
  Comment,
  Community,
  CommunityFeedParams,
  Coupon,
  CreateCommunityBody,
  CreateCouponBody,
  CreateListingBody,
  DemurragePreview,
  DemurrageRun,
  FeedItem,
  FeedParams,
  HelpRequest,
  Listing,
  LoginResult,
  Membership,
  MyCommunity,
  SocialFeedItem,
  Post,
  PriceReference,
  RegisterBody,
  RegisterResult,
  Reputation,
  Report,
  ReportReason,
  ReportStatus,
  ReportTarget,
  ResolutionAction,
  Review,
  SeedCatalogResult,
  Trade,
  TradesParams,
  WalletBalance,
} from "./types";

const TOKEN_KEY = "revoa.token";

export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}
export function setToken(token: string): void {
  try {
    localStorage.setItem(TOKEN_KEY, token);
  } catch {
    /* ignore */
  }
}
export function clearToken(): void {
  try {
    localStorage.removeItem(TOKEN_KEY);
  } catch {
    /* ignore */
  }
}

export class ApiError extends Error {
  status: number;
  fieldErrors?: { Field: string; Message: string }[];
  constructor(
    message: string,
    status: number,
    fieldErrors?: { Field: string; Message: string }[]
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

type QueryValue = string | number | undefined | null;

function qs(params: Record<string, QueryValue>): string {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null || v === "") continue;
    sp.append(k, String(v));
  }
  const s = sp.toString();
  return s ? `?${s}` : "";
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  let res: Response;
  try {
    res = await fetch(path, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(
      "Falha de rede — backend offline? Rode o servidor .NET na porta 8000.",
      0
    );
  }

  return parseResponse<T>(res);
}

// POST multipart (upload de imagem). SEM Content-Type manual — o browser precisa
// gerar o boundary do multipart sozinho. Mesmo envelope de erro do request().
export async function postForm<T>(path: string, form: FormData): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {};
  if (token) headers["Authorization"] = `Bearer ${token}`;

  let res: Response;
  try {
    res = await fetch(path, { method: "POST", headers, body: form });
  } catch {
    throw new ApiError(
      "Falha de rede — backend offline? Rode o servidor .NET na porta 8000.",
      0
    );
  }

  return parseResponse<T>(res);
}

// Converte a resposta em T ou lança ApiError (envelope do backend: ApiError
// { Error, Errors? [{ Field, Message }] } em PascalCase).
async function parseResponse<T>(res: Response): Promise<T> {
  if (res.status === 204) return undefined as T;

  const text = await res.text();
  let data: unknown = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text; // corpo não-JSON (texto puro)
    }
  }

  if (!res.ok) {
    let message = `Erro ${res.status}`;
    let fieldErrors: { Field: string; Message: string }[] | undefined;
    if (data && typeof data === "object") {
      const obj = data as Record<string, unknown>;
      if (typeof obj.Error === "string") message = obj.Error;
      if (Array.isArray(obj.Errors)) {
        fieldErrors = obj.Errors as { Field: string; Message: string }[];
      }
    } else if (typeof data === "string" && data) {
      message = data;
    }
    throw new ApiError(message, res.status, fieldErrors);
  }

  return data as T;
}

export const apiGet = <T>(path: string): Promise<T> => request<T>("GET", path);
export const apiPost = <T>(path: string, body?: unknown): Promise<T> =>
  request<T>("POST", path, body);
export const apiPut = <T>(path: string, body?: unknown): Promise<T> =>
  request<T>("PUT", path, body);

export const api = {
  feed: (p: FeedParams): Promise<FeedItem[]> =>
    apiGet<FeedItem[]>(
      `/api/listings/feed${qs({
        radius: p.radius,
        lat: p.lat,
        lng: p.lng,
        kind: p.kind,
        categoryId: p.categoryId,
        communityId: p.communityId,
        page: p.page,
        mode: p.mode,
        priceMin: p.priceMin,
        priceMax: p.priceMax,
        donationOnly: p.donationOnly ? "true" : undefined,
        sort: p.sort,
        q: p.q,
        sellerIds: p.sellerIds,
      })}`
    ),
  listing: (id: string): Promise<Listing> =>
    apiGet<Listing>(`/api/listings/${encodeURIComponent(id)}`),
  listingComments: (listingId: string, parentId?: string): Promise<Comment[]> =>
    apiGet<Comment[]>(
      `/api/listings/${encodeURIComponent(listingId)}/comments${qs({ parentId })}`
    ),
  createComment: (listingId: string, parentId: string | null, conteudo: string): Promise<string> =>
    apiPost<{ Id: string }>(`/api/listings/${encodeURIComponent(listingId)}/comments`, {
      ParentId: parentId,
      Content: conteudo,
    }).then((r) => r.Id),
  categories: (): Promise<Category[]> => apiGet<Category[]>(`/api/categories`),
  createListing: (body: CreateListingBody): Promise<string> =>
    apiPost<{ Id: string }>(`/api/listings`, body).then((r) => r.Id),
  // Upload de imagem do anúncio (multipart /api/media) → URL relativa pronta
  // para <img src> (a API serve o objeto por stream; o bucket fica privado).
  uploadImage: (file: File): Promise<string> => {
    const form = new FormData();
    form.append("file", file);
    return postForm<{ Url: string }>("/api/media", form).then((r) => r.Url);
  },
  tradeHistory: (p: TradesParams): Promise<Trade[]> =>
    apiGet<Trade[]>(
      `/api/trades${qs({ buyerId: p.buyerId, sellerId: p.sellerId, page: p.page })}`
    ),
  trade: (id: string): Promise<Trade> =>
    apiGet<Trade>(`/api/trades/${encodeURIComponent(id)}`),
  purchase: (listingId: string): Promise<string> =>
    apiPost<{ Id: string }>(`/api/trades/purchase`, { ListingId: listingId }).then((r) => r.Id),
  redeem: (tradeId: string): Promise<void> =>
    apiPost<void>(`/api/trades/${encodeURIComponent(tradeId)}/redeem`),
  release: (tradeId: string): Promise<void> =>
    apiPost<void>(`/api/trades/${encodeURIComponent(tradeId)}/release`),
  dispute: (tradeId: string): Promise<void> =>
    apiPost<void>(`/api/trades/${encodeURIComponent(tradeId)}/dispute`),
  cancelTrade: (tradeId: string): Promise<void> =>
    apiPost<void>(`/api/trades/${encodeURIComponent(tradeId)}/cancel`),
  resolveTrade: (tradeId: string, releaseToSeller: boolean): Promise<void> =>
    apiPost<void>(`/api/trades/${encodeURIComponent(tradeId)}/resolve`, {
      ReleaseToSeller: releaseToSeller,
    }),
  requestHelp: (listingId: string, mensagem: string): Promise<string> =>
    apiPost<{ Id: string }>(`/api/help`, { ListingId: listingId, Message: mensagem }).then((r) => r.Id),
  helpQueue: (listingId: string): Promise<HelpRequest[]> =>
    apiGet<HelpRequest[]>(
      `/api/help${qs({ listingId })}`
    ),
  selectRecipient: (helpRequestId: string): Promise<string> =>
    apiPost<{ Id: string }>(`/api/help/${encodeURIComponent(helpRequestId)}/select`).then((r) => r.Id),
  createReview: (
    tradeId: string,
    rating: number,
    comment?: string
  ): Promise<string> =>
    apiPost<{ Id: string }>(`/api/trades/${encodeURIComponent(tradeId)}/reviews`, {
      Rating: rating,
      Comment: comment,
    }).then((r) => r.Id),
  userReviews: (userId: string, limit = 20): Promise<Review[]> =>
    apiGet<Review[]>(
      `/api/users/${encodeURIComponent(userId)}/reviews${qs({ limit })}`
    ),
  reputation: (userId: string): Promise<Reputation> =>
    apiGet<Reputation>(`/api/users/${encodeURIComponent(userId)}/reputation`),
  register: (body: RegisterBody): Promise<RegisterResult> =>
    apiPost<RegisterResult>(`/api/auth/register`, body),
  verifyEmail: (uid: string, token: string): Promise<void> =>
    apiPost<void>(`/api/auth/verify-email`, { UserId: uid, Token: token }),
  verifyPhone: (uid: string, code: string): Promise<void> =>
    apiPost<void>(`/api/auth/verify-phone`, { UserId: uid, Code: code }),
  resendVerification: (email: string): Promise<RegisterResult> =>
    apiPost<RegisterResult>("/api/auth/resend-verification", { Email: email }),
  loginRequest: (email: string): Promise<{ Message: string }> =>
    apiPost<{ Message: string }>("/api/auth/login/request", { Email: email }),
  loginConfirm: (email: string, code: string): Promise<LoginResult> =>
    apiPost<LoginResult>("/api/auth/login/confirm", { Email: email, Code: code }),
  login: (email: string): Promise<LoginResult> =>
    apiPost<LoginResult>(`/api/auth/login`, { Email: email }),

  communities: (p: CommunityFeedParams = {}): Promise<Community[]> =>
    apiGet<Community[]>(
      `/api/communities${qs({
        radius: p.radius,
        lat: p.lat,
        lng: p.lng,
        axis: p.axis,
        page: p.page,
      })}`
    ),
  community: (id: string): Promise<Community> =>
    apiGet<Community>(`/api/communities/${encodeURIComponent(id)}`),
  // Comunidades do usuário autenticado (401 se anônimo).
  myCommunities: (): Promise<MyCommunity[]> =>
    apiGet<MyCommunity[]>("/api/communities/mine"),
  // Feed social: posts recentes das minhas comunidades em uma chamada (rail
  // "Da sua comunidade" — antes era myCommunities + N× communityPosts no cliente).
  socialFeed: (): Promise<SocialFeedItem[]> =>
    apiGet<SocialFeedItem[]>("/api/communities/mine/posts"),
  createCommunity: (body: CreateCommunityBody): Promise<string> =>
    apiPost<{ Id: string }>(`/api/communities`, body).then((r) => r.Id),
  joinCommunity: (id: string, password?: string): Promise<{ Id: string }> =>
    apiPost<{ Id: string }>(
      `/api/communities/${encodeURIComponent(id)}/join`,
      password ? { Password: password } : {}
    ),
  leaveCommunity: (id: string): Promise<void> =>
    apiPost<void>(`/api/communities/${encodeURIComponent(id)}/leave`),
  communityPosts: (
    id: string,
    parentId?: string,
    page?: number
  ): Promise<Post[]> =>
    apiGet<Post[]>(
      `/api/communities/${encodeURIComponent(id)}/posts${qs({ parentId, page })}`
    ),
  createPost: (
    communityId: string,
    parentId: string | undefined,
    conteudo: string
  ): Promise<string> =>
    apiPost<{ Id: string }>(
      `/api/communities/${encodeURIComponent(communityId)}/posts`,
      { ParentId: parentId, Content: conteudo }
    ).then((r) => r.Id),
  communityMembers: (id: string): Promise<Membership[]> =>
    apiGet<Membership[]>(`/api/communities/${encodeURIComponent(id)}/members`),
  // Histórico do chat ao vivo (cronológico, mais antigas primeiro) — o hub
  // persiste 90 dias; o LiveChat monta o histórico e o SignalR acrescenta ao vivo.
  communityChat: (id: string, limit = 50): Promise<ChatMessage[]> =>
    apiGet<ChatMessage[]>(
      `/api/communities/${encodeURIComponent(id)}/chat${qs({ limit })}`
    ),

  // Carteira do usuário logado (VISUAL_IDENTITY §8 — chip RM$ no header).
  walletBalance: (): Promise<WalletBalance> =>
    apiGet<WalletBalance>("/api/wallet/balance"),

  // Moderação (UF-24/25)
  createReport: (
    targetType: ReportTarget,
    targetId: string,
    reason: ReportReason,
    details?: string
  ): Promise<string> =>
    apiPost<{ Id: string }>(`/api/reports`, {
      TargetType: targetType,
      TargetId: targetId,
      Reason: reason,
      Details: details,
    }).then((r) => r.Id),
  reports: (status?: ReportStatus, page = 1): Promise<Report[]> =>
    apiGet<Report[]>(
      `/api/reports${qs({ status, page })}`
    ),
  resolveReport: (
    id: string,
    action: ResolutionAction,
    note?: string
  ): Promise<void> =>
    apiPost<void>(`/api/reports/${encodeURIComponent(id)}/resolve`, {
      Action: action,
      Note: note,
    }),

  // Notificações pessoais (UF-32; ownership pelo token do usuário logado)
  notifications: (p?: {
    unreadOnly?: boolean;
    page?: number;
  }): Promise<AppNotification[]> =>
    apiGet<AppNotification[]>(
      `/api/notifications${qs({
        unreadOnly: p?.unreadOnly ? "true" : undefined,
        page: p?.page,
      })}`
    ),
  unreadCount: (): Promise<number> =>
    apiGet<number>(`/api/notifications/unread-count`),
  markNotificationRead: (id: string): Promise<void> =>
    apiPost<void>(`/api/notifications/${encodeURIComponent(id)}/read`),

  // Cupom on-chain (UF-29)
  coupons: (page = 1): Promise<Coupon[]> =>
    apiGet<Coupon[]>(`/api/coupons${qs({ page })}`),
  createCoupon: (body: CreateCouponBody): Promise<Coupon> =>
    apiPost<Coupon>(`/api/coupons`, body),
  revokeCoupon: (id: string): Promise<void> =>
    apiPost<void>(`/api/coupons/${encodeURIComponent(id)}/revoke`),
  redeemCoupon: (code: string): Promise<void> =>
    apiPost<void>(`/api/coupons/redeem`, { Code: code }),

  // Admin unificado (UF-30): parâmetros runtime + atalhos para as demais áreas admin.
  adminParameters: (): Promise<AdminParameter[]> =>
    apiGet<AdminParameter[]>(`/api/admin/parameters`),
  setAdminParameter: (key: string, value: number | boolean | string): Promise<void> =>
    apiPut<void>(`/api/admin/parameters/${encodeURIComponent(key)}`, { Value: value }),
  refreshPricing: (): Promise<{ Updated: number }> =>
    apiPost<{ Updated: number }>(`/api/pricing/refresh`),
  brlRate: (): Promise<BrlRate> => apiGet<BrlRate>(`/api/pricing/rate`),
  pricing: (): Promise<PriceReference[]> => apiGet<PriceReference[]>(`/api/pricing`),
  pricingByCategory: (categoryId: string): Promise<PriceReference> =>
    apiGet<PriceReference>(`/api/pricing/categories/${encodeURIComponent(categoryId)}`),
  demurragePreview: (): Promise<DemurragePreview> =>
    apiPost<DemurragePreview>(`/api/demurrage/preview`),
  demurrageRun: (): Promise<DemurrageRun> => apiPost<DemurrageRun>(`/api/demurrage/run`),
  demurrageRuns: (limit = 20): Promise<DemurrageRun[]> =>
    apiGet<DemurrageRun[]>(`/api/demurrage/runs${qs({ limit })}`),
  seedCatalog: (): Promise<SeedCatalogResult> =>
    apiPost<SeedCatalogResult>(`/api/dev/seed-catalog`),
};
