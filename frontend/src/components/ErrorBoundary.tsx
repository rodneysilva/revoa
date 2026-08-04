import { Component, type ErrorInfo, type ReactNode } from "react";

// Captura erros de render para não virar "tela preta". Mostra um fallback com botão de recarregar.
// Não captura erros em event handlers/async (apenas no ciclo de render dos filhos).
type Props = { children: ReactNode };
type State = { hasError: boolean; message?: string };

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, message: error.message };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // eslint-disable-next-line no-console
    console.error("ErrorBoundary capturou:", error, info.componentStack);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex flex-col items-center justify-center gap-4 p-6 text-center bg-ink text-cream">
          <div className="text-5xl" aria-hidden>
            🕊️
          </div>
          <h1 className="text-xl font-bold wordmark">revoa.me</h1>
          <p className="text-silver max-w-md">
            Algo deu errado ao renderizar esta tela. Tente recarregar.
          </p>
          {this.state.message ? (
            <pre className="text-xs text-left text-silver bg-charcoal rounded-lg p-3 max-w-md overflow-auto">
              {this.state.message}
            </pre>
          ) : null}
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="bg-brand text-ink font-semibold rounded-lg px-5 py-2.5 hover:opacity-90"
          >
            Recarregar
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
