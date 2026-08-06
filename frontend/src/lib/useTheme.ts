import { useEffect, useState } from "react";

export type Theme = "light" | "dark";

// Lê o tema atual do <html> (definido pelo script anti-flash no <head> ou por toggle anterior).
function readTheme(): Theme {
  if (typeof document === "undefined") return "light";
  return document.documentElement.classList.contains("dark") ? "dark" : "light";
}

// Hook: tema do app com persistência (localStorage) + sincroniza a classe .dark no <html>.
// Padrão: segue o prefers-color-scheme do sistema na 1ª visita.
export function useTheme() {
  const [theme, setTheme] = useState<Theme>(readTheme);

  useEffect(() => {
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const handler = () => {
      // Só reage ao sistema se o usuário NÃO escolheu manualmente.
      if (!localStorage.getItem("theme")) {
        setTheme(readTheme());
      }
    };
    mq.addEventListener("change", handler);
    return () => mq.removeEventListener("change", handler);
  }, []);

  const toggle = () => {
    const next: Theme = theme === "dark" ? "light" : "dark";
    setTheme(next);
    localStorage.setItem("theme", next);
    document.documentElement.classList.toggle("dark", next === "dark");
  };

  return { theme, toggle };
}
