import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { clearToken, getToken, setToken } from "../api/client";

// "Verified" = email_verified && phone_verified (regra do gate).
export interface AuthUser {
  userId: string;
  nome: string;
  email: string;
  verified: boolean;
}

interface AuthState {
  user: AuthUser | null;
  token: string | null;
  login: (token: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

interface JwtClaims {
  sub?: string;
  name?: string;
  email?: string;
  email_verified?: string | boolean;
  phone_verified?: string | boolean;
}

// Decodifica o payload do JWT (base64url → JSON) sem validar assinatura.
function decodeJwt(token: string): JwtClaims | null {
  try {
    const parts = token.split(".");
    if (parts.length < 2) return null;
    const b64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const pad = b64.length % 4 ? "=".repeat(4 - (b64.length % 4)) : "";
    const binary = atob(b64 + pad);
    const json = decodeURIComponent(
      binary
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join("")
    );
    return JSON.parse(json) as JwtClaims;
  } catch {
    return null;
  }
}

function toBool(v: string | boolean | undefined): boolean {
  if (typeof v === "boolean") return v;
  if (typeof v === "string") return v.toLowerCase() === "true";
  return false;
}

function userFromToken(token: string): AuthUser | null {
  const claims = decodeJwt(token);
  if (!claims) return null;
  return {
    userId: claims.sub ?? "",
    nome: claims.name ?? "",
    email: claims.email ?? "",
    verified: toBool(claims.email_verified) && toBool(claims.phone_verified),
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setTokenState] = useState<string | null>(() => getToken());
  const [user, setUser] = useState<AuthUser | null>(() => {
    const t = getToken();
    return t ? userFromToken(t) : null;
  });

  // Re-deriva o user sempre que o token muda (login/logout).
  useEffect(() => {
    if (!token) {
      setUser(null);
      return;
    }
    setUser(userFromToken(token));
  }, [token]);

  const value = useMemo<AuthState>(
    () => ({
      user,
      token,
      login: (t: string) => {
        setToken(t);
        setTokenState(t);
      },
      logout: () => {
        clearToken();
        setTokenState(null);
        setUser(null);
      },
    }),
    [user, token]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth deve ser usado dentro de <AuthProvider>");
  return ctx;
}
