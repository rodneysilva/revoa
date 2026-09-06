using FluentAssertions;
using Revoa.Abstractions;
using Revoa.Catalog.Domain.Aggregates.ListingAggregate;
using Xunit;

namespace Revoa.Domain.Tests;

// Máquina de estados + invariáveis do Listing (OOUX 7). Create() nasce Active
// (Draft existe no enum para fluxo futuro). Regras Modo×Kind vêm do BUSINESS_RULES §1.2.
public class ListingStateMachineTests
{
    private static Location AnyLocation => Location.Create(-23.56, -46.63, "Pinheiros", "São Paulo", "05421-000");

    private static Listing NewListing(
        ListingKind kind = ListingKind.Product,
        ListingMode mode = ListingMode.Trade,
        long precoRvm = 10,
        ListingVisibility visibilidade = ListingVisibility.Global,
        Guid? comunidadeId = null,
        ProductDetails? productDetails = null,
        ServiceDetails? serviceDetails = null) =>
        Listing.Create(
            kind,
            mode,
            "Notebook Dell usado",
            "Funcional, bateria ok.",
            new List<string> { "https://img/1.jpg" },
            precoRvm,
            Guid.NewGuid(),
            "Marina",
            null,
            AnyLocation,
            Guid.NewGuid(),
            comunidadeId,
            visibilidade,
            productDetails: productDetails,
            serviceDetails: serviceDetails);

    private static ProductDetails ProdDetails => ProductDetails.Create(ProductCondition.Usado, 1);

    private static ServiceDetails ServDetails => ServiceDetails.Create(ServiceUnitType.Hours, 60, 30);

    // --- Create: invariáveis Modo×Kind (BUSINESS_RULES §1.2) ---

    [Fact]
    public void Create_ProdutoTrocar_NasceActiveComVersao1()
    {
        var listing = NewListing(productDetails: ProdDetails);

        listing.Status.Should().Be(ListingStatus.Active);
        listing.Version.Should().Be(1);
        listing.Kind.Should().Be(ListingKind.Product);
        listing.NftTokenId.Should().BeNull();
    }

    [Fact]
    public void Create_ServicoTrocar_EhValido()
    {
        var listing = NewListing(kind: ListingKind.Service, serviceDetails: ServDetails);

        listing.Kind.Should().Be(ListingKind.Service);
    }

    [Fact]
    public void Create_RepassarServico_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Service, mode: ListingMode.Resell, serviceDetails: ServDetails);
        act.Should().Throw<DomainException>().WithMessage("*exclusivo de produtos*");
    }

    [Fact]
    public void Create_DoarServico_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Service, mode: ListingMode.Donate, serviceDetails: ServDetails);
        act.Should().Throw<DomainException>().WithMessage("*exclusivo de produtos*");
    }

    [Fact]
    public void Create_VoluntariarProduto_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Product, mode: ListingMode.Volunteer, precoRvm: 0, productDetails: ProdDetails);
        act.Should().Throw<DomainException>().WithMessage("*exclusivo de serviços*");
    }

    // --- Create: preço ---

    [Fact]
    public void Create_PrecoNegativo_LancaDomainException()
    {
        var act = () => NewListing(precoRvm: -1, productDetails: ProdDetails);
        act.Should().Throw<DomainException>().WithMessage("*negativo*");
    }

    [Fact]
    public void Create_DoarProdutoComPreco_LancaDomainException()
    {
        var act = () => NewListing(mode: ListingMode.Donate, precoRvm: 5, productDetails: ProdDetails);
        act.Should().Throw<DomainException>().WithMessage("*0 RVM*");
    }

    [Fact]
    public void Create_VoluntariarServicoComPreco_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Service, mode: ListingMode.Volunteer, precoRvm: 5, serviceDetails: ServDetails);
        act.Should().Throw<DomainException>().WithMessage("*0 RVM*");
    }

    [Fact]
    public void Create_DoarComPrecoZero_EhValido()
    {
        var listing = NewListing(mode: ListingMode.Donate, precoRvm: 0, productDetails: ProdDetails);

        listing.PriceRvm.Should().Be(0);
    }

    // --- Create: VOs por kind ---

    [Fact]
    public void Create_ProdutoSemDetalhes_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Product);
        act.Should().Throw<DomainException>().WithMessage("*obrigatórios*");
    }

    [Fact]
    public void Create_ServicoSemDetalhes_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Service);
        act.Should().Throw<DomainException>().WithMessage("*obrigatórios*");
    }

    [Fact]
    public void Create_ProdutoComDetalhesDeServico_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Product, productDetails: ProdDetails, serviceDetails: ServDetails);
        act.Should().Throw<DomainException>().WithMessage("*não aplicam*");
    }

    [Fact]
    public void Create_ServicoComDetalhesDeProduto_LancaDomainException()
    {
        var act = () => NewListing(kind: ListingKind.Service, productDetails: ProdDetails, serviceDetails: ServDetails);
        act.Should().Throw<DomainException>().WithMessage("*não aplicam*");
    }

    // --- Create: visibilidade ---

    [Fact]
    public void Create_VisibilidadeComunidadeSemCommunityId_LancaDomainException()
    {
        var act = () => NewListing(visibilidade: ListingVisibility.Community, productDetails: ProdDetails);
        act.Should().Throw<DomainException>().WithMessage("*exige CommunityId*");
    }

    [Fact]
    public void Create_VisibilidadeComunidadeComCommunityId_EhValido()
    {
        var listing = NewListing(
            visibilidade: ListingVisibility.Community,
            comunidadeId: Guid.NewGuid(),
            productDetails: ProdDetails);

        listing.Visibility.Should().Be(ListingVisibility.Community);
    }

    // --- NFT (mint-to-escrow) ---

    [Fact]
    public void SetNftTokenId_Produto_PreencheTokenId()
    {
        var listing = NewListing(productDetails: ProdDetails);

        listing.SetNftTokenId(42);

        listing.NftTokenId.Should().Be(42);
    }

    [Fact]
    public void SetNftTokenId_Servico_LancaDomainException()
    {
        var listing = NewListing(kind: ListingKind.Service, serviceDetails: ServDetails);

        var act = () => listing.SetNftTokenId(42);
        act.Should().Throw<DomainException>().WithMessage("*produto*");
    }

    // --- Transições de estado ---

    [Fact]
    public void MarcarConcluido_DeActive_VaiParaCompleted()
    {
        var listing = NewListing(productDetails: ProdDetails);

        listing.MarcarConcluido();

        listing.Status.Should().Be(ListingStatus.Completed);
    }

    [Fact]
    public void MarcarCancelado_DeActive_VaiParaCancelled()
    {
        var listing = NewListing(productDetails: ProdDetails);

        listing.MarcarCancelado();

        listing.Status.Should().Be(ListingStatus.Cancelled);
    }

    [Fact]
    public void Completed_EhEstadoTerminal()
    {
        var listing = NewListing(productDetails: ProdDetails);
        listing.MarcarConcluido();

        var concluirDeNovo = () => listing.MarcarConcluido();
        var cancelar = () => listing.MarcarCancelado();

        concluirDeNovo.Should().Throw<DomainException>("concluído é terminal");
        cancelar.Should().Throw<DomainException>("concluído é terminal");
    }

    [Fact]
    public void Cancelled_EhEstadoTerminal()
    {
        var listing = NewListing(productDetails: ProdDetails);
        listing.MarcarCancelado();

        var act = () => listing.MarcarConcluido();
        act.Should().Throw<DomainException>();
    }
}
