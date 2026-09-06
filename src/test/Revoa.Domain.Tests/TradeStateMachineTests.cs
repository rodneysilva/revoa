using FluentAssertions;
using Revoa.Abstractions;
using Revoa.Exchange.Domain.Aggregates.TradeAggregate;
using Xunit;

namespace Revoa.Domain.Tests;

// Máquina de estados do Trade — espelha o EscrowVault on-chain e é o agregado que
// move dinheiro. Create() nasce Funded (compra/doação já financiada no command handler);
// estados terminais: Released, Refunded, Cancelled.
public class TradeStateMachineTests
{
    private static Trade NewTrade(
        TradeMode mode = TradeMode.Trade,
        TradeKind kind = TradeKind.Product,
        long totalRvm = 10,
        long tokenId = 1,
        string sellerNome = "Marina",
        string buyerNome = "João") =>
        Trade.Create(
            Guid.NewGuid(),
            mode,
            kind,
            Guid.NewGuid(),
            "0xSellerWallet",
            sellerNome,
            null,
            Guid.NewGuid(),
            "0xBuyerWallet",
            buyerNome,
            null,
            totalRvm,
            "0xAssetContract",
            tokenId,
            onChainTradeId: 7,
            fundedAt: DateTime.UtcNow,
            lastTxHash: "0xTx1");

    // --- Create: invariáveis ---

    [Fact]
    public void Create_ComDadosValidos_NasceFunded()
    {
        var trade = NewTrade();

        trade.State.Should().Be(TradeState.Funded);
        trade.Version.Should().Be(1);
        trade.FundedAt.Should().NotBeNull();
        trade.OnChainTradeId.Should().Be(7);
        trade.IsDonation.Should().BeFalse();
    }

    [Fact]
    public void Create_NomeEmBranco_UsaPlaceholder()
    {
        var trade = NewTrade(sellerNome: " ", buyerNome: "");

        trade.SellerName.Should().Be("Usuário");
        trade.BuyerName.Should().Be("Usuário");
    }

    [Fact]
    public void Create_TotalNegativo_LancaDomainException()
    {
        var act = () => NewTrade(totalRvm: -1);
        act.Should().Throw<DomainException>().WithMessage("*negativo*");
    }

    [Theory]
    [InlineData(TradeMode.Donate)]
    [InlineData(TradeMode.Volunteer)]
    public void Create_DoacaoComTotalDiferenteDeZero_LancaDomainException(TradeMode mode)
    {
        var act = () => NewTrade(mode: mode, totalRvm: 5);
        act.Should().Throw<DomainException>().WithMessage("*total 0 RVM*");
    }

    [Fact]
    public void Create_DoacaoComTotalZero_EhValida()
    {
        var trade = NewTrade(mode: TradeMode.Donate, totalRvm: 0);

        trade.IsDonation.Should().BeTrue();
        trade.State.Should().Be(TradeState.Funded);
    }

    [Fact]
    public void Create_ProdutoSemTokenId_LancaDomainException()
    {
        var act = () => NewTrade(kind: TradeKind.Product, tokenId: 0);
        act.Should().Throw<DomainException>().WithMessage("*tokenId*");
    }

    [Fact]
    public void Create_MesmoVendedorEComprador_LancaDomainException()
    {
        var id = Guid.NewGuid();
        var act = () => Trade.Create(
            Guid.NewGuid(), TradeMode.Trade, TradeKind.Product,
            id, "0xS", "Marina", null,
            id, "0xB", "Marina", null,
            10, "0xA", 1, 7, DateTime.UtcNow, null);
        act.Should().Throw<DomainException>().WithMessage("*usuários diferentes*");
    }

    [Theory]
    [InlineData("", "0xB")]   // sem AssetContract
    [InlineData("0xA", "")]   // sem carteira do comprador
    public void Create_ContratosObrigatoriosAusentes_LancamDomainException(string contract, string buyerWallet)
    {
        var act = () => Trade.Create(
            Guid.NewGuid(), TradeMode.Trade, TradeKind.Product,
            Guid.NewGuid(), "0xS", "Marina", null,
            Guid.NewGuid(), buyerWallet, "João", null,
            10, contract, 1, 7, DateTime.UtcNow, null);
        act.Should().Throw<DomainException>();
    }

    // --- Redeem (serviço) ---

    [Fact]
    public void MarkRedeemed_ServicoFunded_MarcaVoucher()
    {
        var trade = NewTrade(kind: TradeKind.Service, tokenId: 0);

        trade.MarkRedeemed("0xTx2");

        trade.VoucherRedeemed.Should().BeTrue();
        trade.State.Should().Be(TradeState.Funded); // redeem não muda estado
        trade.LastTxHash.Should().Be("0xTx2");
    }

    [Fact]
    public void MarkRedeemed_Produto_LancaDomainException()
    {
        var trade = NewTrade(kind: TradeKind.Product);
        var act = () => trade.MarkRedeemed(null);
        act.Should().Throw<DomainException>().WithMessage("*apenas a serviços*");
    }

    [Fact]
    public void MarkRedeemed_DuasVezes_LancaDomainException()
    {
        var trade = NewTrade(kind: TradeKind.Service, tokenId: 0);
        trade.MarkRedeemed(null);

        var act = () => trade.MarkRedeemed(null);
        act.Should().Throw<DomainException>().WithMessage("*já foi redeemado*");
    }

    [Fact]
    public void MarkRedeemed_AposLiberacao_LancaDomainException()
    {
        var trade = NewTrade(kind: TradeKind.Service, tokenId: 0);
        trade.MarkLiberada(null);

        var act = () => trade.MarkRedeemed(null);
        act.Should().Throw<DomainException>().WithMessage("*financiada*");
    }

    // --- Disputa ---

    [Fact]
    public void MarkDisputada_DeFunded_VaiParaDisputedERegistraAutor()
    {
        var trade = NewTrade();

        trade.MarkDisputada("joao@revoa.dev");

        trade.State.Should().Be(TradeState.Disputed);
        trade.DisputeOpenedBy.Should().Be("joao@revoa.dev");
    }

    // --- Liberação / reembolso / cancelamento ---

    [Theory]
    [InlineData(true)]   // cooperativa (de Funded)
    [InlineData(false)]  // árbitro (de Disputed)
    public void MarkLiberada_DeFundedOuDisputed_VaiParaReleased(bool viaDisputa)
    {
        var trade = NewTrade();
        if (viaDisputa)
        {
            trade.MarkDisputada("buyer@revoa.dev");
        }

        trade.MarkLiberada("0xTx3", viaDisputa ? "arbitro@revoa.dev" : null);

        trade.State.Should().Be(TradeState.Released);
        trade.ReleasedAt.Should().NotBeNull();
        if (viaDisputa)
        {
            trade.ResolvedBy.Should().Be("arbitro@revoa.dev");
        }
    }

    [Fact]
    public void MarkReembolsada_DeDisputed_VaiParaRefundedComAuditoria()
    {
        var trade = NewTrade();
        trade.MarkDisputada("buyer@revoa.dev");

        trade.MarkReembolsada("0xTx4", "arbitro@revoa.dev");

        trade.State.Should().Be(TradeState.Refunded);
        trade.ResolvedBy.Should().Be("arbitro@revoa.dev");
    }

    [Fact]
    public void MarkCancelada_DeFunded_VaiParaCancelled()
    {
        var trade = NewTrade();

        trade.MarkCancelada(null);

        trade.State.Should().Be(TradeState.Cancelled);
    }

    // --- Estados terminais: nenhuma transição sai deles ---

    [Fact]
    public void Released_EhEstadoTerminal()
    {
        var trade = NewTrade();
        trade.MarkLiberada(null);

        ((Action)(() => trade.MarkDisputada("x@revoa.dev")))
            .Should().Throw<DomainException>("trade liberada é terminal");
        ((Action)(() => trade.MarkLiberada(null)))
            .Should().Throw<DomainException>("trade liberada é terminal");
        ((Action)(() => trade.MarkReembolsada(null)))
            .Should().Throw<DomainException>("trade liberada é terminal");
        ((Action)(() => trade.MarkCancelada(null)))
            .Should().Throw<DomainException>("trade liberada é terminal");
    }

    [Fact]
    public void Refunded_EhEstadoTerminal()
    {
        var trade = NewTrade();
        trade.MarkReembolsada(null);

        var act = () => trade.MarkLiberada(null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancelled_EhEstadoTerminal()
    {
        var trade = NewTrade();
        trade.MarkCancelada(null);

        var act = () => trade.MarkDisputada("x@revoa.dev");
        act.Should().Throw<DomainException>();
    }
}
