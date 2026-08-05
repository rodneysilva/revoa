// Tempo relativo em pt-BR: "há 5 min", "há 2 h", "ontem", data.
export function timeAgo(date: string | Date): string {
  const d = typeof date === "string" ? new Date(date) : date;
  if (isNaN(d.getTime())) return "";
  const diff = Math.max(0, Math.floor((Date.now() - d.getTime()) / 1000));
  if (diff < 60) return "há poucos segundos";
  const min = Math.floor(diff / 60);
  if (min < 60) return `há ${min} min`;
  const h = Math.floor(min / 60);
  if (h < 24) return `há ${h} h`;
  const dias = Math.floor(h / 24);
  if (dias === 1) return "ontem";
  if (dias < 7) return `há ${dias} dias`;
  const semanas = Math.floor(dias / 7);
  if (semanas === 1) return "há 1 semana";
  if (semanas < 5) return `há ${semanas} semanas`;
  return d.toLocaleDateString("pt-BR");
}