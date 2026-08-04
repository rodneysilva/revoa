// Cliente fetch tipado. Lê o token JWT do localStorage e envia como
// Authorization: Bearer. Usa paths relativos (proxy do Vite resolve CORS).

import type {
  Category,
  CreateListingBody,
  FeedItem,
  FeedParams,
  Listing,
  LoginResult,
  RegisterBody,
  RegisterResult,
  Trade,
  TradesParams,
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
  fieldErrors?: { field: string; message: string }[];
  constructor(
    message: string,
    status: number,
    fieldErrors?: { field: string; message: string }[]
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

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  let data: unknown = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text; // corpo não-JSON (ex.: id cru)
    }
  }

  if (!res.ok) {
    let message = `Erro ${res.status}`;
    let fieldErrors: { field: string; message: string }[] | undefined;
    if (data && typeof data === "object") {
      const obj = data as Record<string, unknown>;
      if (typeof obj.error === "string") message = obj.error;
      if (Array.isArray(obj.errors)) {
        fieldErrors = obj.errors as { field: string; message: string }[];
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

export const api = {
  feed: (p: FeedParams): Promise<FeedItem[]> =>
    apiGet<FeedItem[]>(
      `/api/listings/feed${qs({
        raio: p.raio,
        lat: p.lat,
        lng: p.lng,
        kind: p.kind,
        categoriaId: p.categoriaId,
        comunidadeId: p.comunidadeId,
        page: p.page,
      })}`
    ),
  listing: (id: string): Promise<Listing> =>
    apiGet<Listing>(`/api/listings/${encodeURIComponent(id)}`),
  categories: (): Promise<Category[]> => apiGet<Category[]>(`/api/categories`),
  createListing: (body: CreateListingBody): Promise<string> =>
    apiPost<string>(`/api/listings`, body),
  trades: (p: TradesParams): Promise<Trade[]> =>
    apiGet<Trade[]>(
      `/api/trades${qs({ buyerId: p.buyerId, sellerId: p.sellerId, page: p.page })}`
    ),
  trade: (id: string): Promise<Trade> =>
    apiGet<Trade>(`/api/trades/${encodeURIComponent(id)}`),
  register: (body: RegisterBody): Promise<RegisterResult> =>
    apiPost<RegisterResult>(`/api/auth/register`, body),
  verifyEmail: (uid: string, token: string): Promise<void> =>
    apiPost<void>(`/api/auth/verify-email`, { UserId: uid, Token: token }),
  verifyPhone: (uid: string, code: string): Promise<void> =>
    apiPost<void>(`/api/auth/verify-phone`, { UserId: uid, Code: code }),
  resendVerification: (email: string): Promise<RegisterResult> =>
    apiPost<RegisterResult>("/api/auth/resend-verification", { Email: email }),
  login: (email: string): Promise<LoginResult> =>
    apiPost<LoginResult>(`/api/auth/login`, { Email: email }),
};
