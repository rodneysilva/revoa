using Revoa.Abstractions;

namespace Revoa.Coupon.Domain.Aggregates.CouponAggregate;

// Estado do cupom: ativo (resgatável) ou revogado (admin invalidou).
public enum CouponStatus
{
    Active,
    Revoked
}

// Cupom/convite on-chain (UF-29, coleção Coupons isolada). Espelha o CouponRedeemer: o codeHash é o
// keccak256 do código (mesmo que o contrato usa com abi.encodePacked), guardado off-chain só p/ correlação.
// O estado autoritativo (maxUses/usedCount/expiry/revoked) vive ON-CHAIN; este doc é projeção admin
// (lista/revogação). Bump de Version é responsabilidade do repositório (UpdateAsync), nunca do Revoke().
//
// Nome da classe: "CouponAggregate" em vez de "Coupon" para evitar colisão com o segmento de namespace
// "Coupon" (Revoa.Coupon.*) — em C#, unqualified "Coupon" resolveria para o namespace (CS0576). O conceito
// de domínio permanece "cupom"; este é apenas o identificador do aggregate root.
public class CouponAggregate : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public long AmountRvm { get; private set; }
    public int MaxUses { get; private set; }
    public DateTime? Expiry { get; private set; }
    public CouponStatus Status { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private CouponAggregate() { }

    // Factory: valida code não-vazio e amount>0. Status=Active. codeHash é recebido (computado no
    // handler/infra — keccak do código, hex sem 0x, espelhando o on-chain). CreatedAt=UtcNow. Version=1.
    public static CouponAggregate Create(
        string code,
        long amountRvm,
        int maxUses,
        DateTime? expiry,
        string codeHash,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Código do cupom é obrigatório.");
        }

        if (amountRvm <= 0)
        {
            throw new DomainException("Quantidade de RVM deve ser maior que zero.");
        }

        if (maxUses < 0)
        {
            throw new DomainException("Número máximo de usos não pode ser negativo.");
        }

        return new CouponAggregate
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            AmountRvm = amountRvm,
            MaxUses = maxUses,
            Expiry = expiry,
            Status = CouponStatus.Active,
            CodeHash = codeHash,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "Admin" : createdBy,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    // Revoga o cupom (Active→Revoked): apenas marca o status — o revoke on-chain já invalidou o resgate.
    // NÃO IncrementVersion (bump fica no repositório). Erro se já revogado.
    public void Revoke()
    {
        if (Status == CouponStatus.Revoked)
        {
            throw new DomainException("Cupom já está revogado.");
        }

        Status = CouponStatus.Revoked;
    }
}
