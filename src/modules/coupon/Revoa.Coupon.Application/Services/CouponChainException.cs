namespace Revoa.Coupon.Application.Services;

// Tipos de falha on-chain oriundos do CouponRedeemer (mapeados pelos seletores de erro do contrato).
// O command converte cada tipo numa mensagem amigável ao usuário.
public enum CouponChainError
{
    AlreadyExists,
    NotFound,
    Expired,
    AlreadyUsed,
    MaxUsesReached,
    Revoked,
    ZeroAmount,
    InvalidExpiry,
    Unknown
}

// Exceção de orquestração on-chain: carrega o KIND do erro (seletores do CouponRedeemer) para que o
// command mapeie p/ mensagem amigável. Lançada pela impl Nethereum ao capturar revert de EstimateGas.
public class CouponChainException : Exception
{
    public CouponChainError Error { get; }

    public CouponChainException(CouponChainError error, string message)
        : base(message)
    {
        Error = error;
    }

    public CouponChainException(CouponChainError error, string message, Exception innerException)
        : base(message, innerException)
    {
        Error = error;
    }
}
