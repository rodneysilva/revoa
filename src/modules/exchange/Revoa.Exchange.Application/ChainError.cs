namespace Revoa.Exchange.Application;

// Traduz erros on-chain genéricos (custom errors sem mensagem) em dica acionável p/ o usuário.
// "Smart contract error" = revert por custom error (Nethereum não decodifica o seletor); mapeia
// para um contexto que o usuário entende.
public static class ChainError
{
    public static string Friendly(Exception ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message ?? string.Empty);

        if (msg.Contains("insufficient", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("gas", StringComparison.OrdinalIgnoreCase))
        {
            return "Falha on-chain: saldo insuficiente de ETH (gas). Tente novamente.";
        }

        // Custom error revert (sem mensagem decodificável) — contexto de troca/doação.
        if (msg.Contains("smart contract", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("revert", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("execution", StringComparison.OrdinalIgnoreCase))
        {
            return "Falha on-chain: a operação reverteu (a troca pode já ter sido concluída, cancelada ou estar inválida). Recarregue o tracker.";
        }

        return "Falha on-chain: " + msg;
    }
}
