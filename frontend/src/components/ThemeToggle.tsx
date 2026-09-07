import { Moon, Sun } from "lucide-react";
import { useTheme } from "../lib/useTheme";

// Botão toggle de tema (light/dark). Compacto, cabe no header.
export function ThemeToggle() {
  const { theme, toggle } = useTheme();
  const Icon = theme === "dark" ? Sun : Moon;
  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={theme === "dark" ? "Mudar para tema claro" : "Mudar para tema escuro"}
      title={theme === "dark" ? "Tema claro" : "Tema escuro"}
      className="inline-flex items-center justify-center w-9 h-9 rounded-lg text-cream hover:bg-smoke transition"
    >
      <Icon aria-hidden className="w-4 h-4" />
    </button>
  );
}
