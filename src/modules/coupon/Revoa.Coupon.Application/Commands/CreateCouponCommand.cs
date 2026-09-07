using System.Numerics;
using MediatR;
using Revoa.Abstractions;
using Revoa.Coupon.Application.DTOs;
using Revoa.Coupon.Application.Services;
using Revoa.Coupon.Domain.Aggregates.CouponAggregate;
using Revoa.Coupon.Domain.Repositories;

namespace Revoa.Coupon.Application.Commands;

// Cria cupom on-chain (UF-29): admin define amount/maxUses/expiry (+ code opcional; se vazio, gera
// um aleatório). Assina como faucet (COUPON_ADMIN_ROLE). Gate Admin no controller; createdBy = claim email.
public sealed record CreateCouponCommand(
    long AmountRvm,
    int MaxUses,
    DateTime? Expiry,
    string? Code,
    string CreatedBy) : IRequest<Result<CouponDto>>;

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<CouponDto>>
{
    private readonly ICouponRepository _coupons;
    private readonly ICouponChainService _chain;

    public CreateCouponCommandHandler(ICouponRepository coupons, ICouponChainService chain)
    {
        _coupons = coupons;
        _chain = chain;
    }

    public async Task<Result<CouponDto>> Handle(CreateCouponCommand request, CancellationToken ct)
    {
        var code = string.IsNullOrWhiteSpace(request.Code) ? GenerateCode() : request.Code.Trim();

        long expiryUnix = 0;
        if (request.Expiry is { } expiry)
        {
            expiryUnix = new DateTimeOffset(DateTime.SpecifyKind(expiry, DateTimeKind.Utc)).ToUnixTimeSeconds();
        }

        try
        {
            await _chain.EnsureFaucetCouponAdminRoleAsync(ct);

            // RVM tem 18 decimais: o contrato minta unidades RAW. AmountRvm é a
            // quantidade legível (10 = 10 RVM) — sem essa conversão o mint sai em
            // poeira (10 wei). Espelha o faucet do Token (RvmConstants).
            var rawAmount = BigInteger.Multiply(request.AmountRvm, BigInteger.Pow(10, 18));

            var (codeHash, _) = await _chain.CreateCouponAsync(
                code, rawAmount, request.MaxUses, expiryUnix, ct);

            var coupon = CouponAggregate.Create(
                code, request.AmountRvm, request.MaxUses, request.Expiry, codeHash, request.CreatedBy);

            await _coupons.AddAsync(coupon, ct);

            return Result<CouponDto>.Ok(CouponDtoMapper.From(coupon));
        }
        catch (CouponChainException ex) when (ex.Error == CouponChainError.AlreadyExists)
        {
            return Result<CouponDto>.Fail("Cupom já existe.");
        }
        catch (CouponChainException ex)
        {
            return Result<CouponDto>.Fail("Falha ao criar cupom: " + ex.Message);
        }
        catch (DomainException ex)
        {
            return Result<CouponDto>.Fail(ex.Message);
        }
    }

    // Gera código alfanumérico maiúsculo de 8 chars (sem 0/O/1/I ambíguos). Shareable pelo admin.
    private static string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
        }
        return new string(chars);
    }
}
