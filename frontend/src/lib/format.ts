export function brlEstimate(rvm: number, rate: number): string | null {
  if (!rate || !Number.isFinite(rate) || rvm <= 0) return null;
  const value = rvm * rate;
  const hasCents = !Number.isInteger(value);
  const formatted = new Intl.NumberFormat("pt-BR", {
    minimumFractionDigits: hasCents ? 2 : 0,
    maximumFractionDigits: hasCents ? 2 : 0,
  }).format(value);
  return `R$ ${formatted}`;
}
