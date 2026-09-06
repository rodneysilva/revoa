// Gate client-side por role do JWT (claims "role", emitidas pelo backend: role do aggregate +
// "Admin" quando o e-mail está no allowlist Admin:Emails). Mostra link/página; o gate REAL
// continua sendo as policies do backend ("Admin"/"Arbitrator"), que rejeitam com 403.
import type { AuthUser } from "../auth/AuthContext";

export function isAdminUser(user?: AuthUser | null): boolean {
  return !!user && user.roles.includes("Admin");
}
