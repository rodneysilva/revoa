import { useTheme } from "../lib/useTheme";

// Botão toggle de tema (light/dark). Compacto, cabe no header.
export function ThemeToggle() {
  const { theme, toggle } = useTheme();
  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={theme === "dark" ? "Mudar para tema claro" : "Mudar para tema escuro"}
      title={theme === "dark" ? "Tema claro" : "Tema escuro"}
      className="inline-flex items-center justify-center w-9 h-9 rounded-lg text-cream hover:bg-smoke transition"
    >
      {theme === "dark" ? "☀️" : "🌙"}
    </button>
  );
}
