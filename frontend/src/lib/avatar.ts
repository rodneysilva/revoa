// Fallback de avatar via DiceBear (iniciais) quando não há URL explícita.
export function avatarUrl(nome: string, explicit?: string): string {
  if (explicit) return explicit;
  const seed = encodeURIComponent((nome || "?").trim());
  return `https://api.dicebear.com/9.x/initials/svg?seed=${seed}&backgroundColor=10b981,0ea5e9,84cc16,ec4899,f59e0b,bc6c25&fontWeight=600`;
}
