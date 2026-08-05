using System.Globalization;
using System.Numerics;
using Revoa.Abstractions;

namespace Revoa.Demurrage.Domain.Aggregates.DemurrageRunAggregate;

// Registro de UMA execução do demurrage (UF-27, Fase 3). Append-only: um documento por execução
// (coleção DemurrageRuns, índice por RunAt desc). Captura os parâmetros aplicados (RateBps/Floor),
// o resultado (carteiras afetadas/isentas, total queimado) e quem disparou. O demurrage queima
// RateBps% do saldo de cada carteira ACIMA do piso (piso de isenção); saldos ≤ piso são isentos.
//
// TotalBurnedRaw é guardado como string decimal (raw 18d): BigInteger não tem serializer nativo no
// driver MongoDB e estoura long (1 RVM = 10^18 > long.MaxValue). Bump de Version é responsabilidade
// do repositório, nunca do aggregate (aqui é append-only, então fica em 1).
public class DemurrageRun : AggregateRoot
{
    public DateTime RunAt { get; private set; }

    // Taxa aplicada nesta execução (basis points: 50 = 0,5%).
    public int RateBps { get; private set; }

    // Piso de isenção em RVM (inteiro) aplicado nesta execução.
    public long FloorRvm { get; private set; }

    // Carteiras que tiveram saldo queimado (> piso).
    public int AccountsAffected { get; private set; }

    // Total queimado em raw 18d (string decimal — ver nota da classe).
    public string TotalBurnedRaw { get; private set; } = "0";

    // Carteiras isentas (≤ piso) ou que falharam na leitura/queima.
    public int Skipped { get; private set; }

    public string ExecutedBy { get; private set; } = string.Empty;

    // True se foi apenas simulação (preview). Execuções reais gravam Preview=false.
    public bool Preview { get; private set; }

    private DemurrageRun() { }

    // Factory: valida parâmetros. RunAt=UtcNow. NÃO IncrementVersion (append-only; repo só insert).
    public static DemurrageRun Create(
        int rateBps,
        long floorRvm,
        int accountsAffected,
        BigInteger totalBurnedRaw,
        int skipped,
        string executedBy,
        bool preview)
    {
        if (rateBps < 0 || rateBps > 10000)
        {
            throw new DomainException("RateBps deve estar entre 0 e 10000 (0%–100%).");
        }

        if (floorRvm < 0)
        {
            throw new DomainException("FloorRvm não pode ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(executedBy))
        {
            throw new DomainException("ExecutedBy é obrigatório.");
        }

        return new DemurrageRun
        {
            Id = Guid.NewGuid(),
            RunAt = DateTime.UtcNow,
            RateBps = rateBps,
            FloorRvm = floorRvm,
            AccountsAffected = accountsAffected,
            TotalBurnedRaw = totalBurnedRaw.ToString(CultureInfo.InvariantCulture),
            Skipped = skipped,
            ExecutedBy = executedBy,
            Preview = preview,
            Version = 1
        };
    }
}
