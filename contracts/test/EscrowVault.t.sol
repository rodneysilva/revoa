// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Test} from "forge-std/Test.sol";
import {RVM} from "../src/RVM.sol";
import {Treasury} from "../src/Treasury.sol";
import {ProductNFT} from "../src/ProductNFT.sol";
import {ServiceVoucher} from "../src/ServiceVoucher.sol";
import {EscrowVault} from "../src/EscrowVault.sol";

contract EscrowVaultTest is Test {
    RVM rvm;
    Treasury treasury;
    ProductNFT productNft;
    ServiceVoucher serviceVoucher;
    EscrowVault vault;

    address admin = address(this);
    address seller = address(0x5E11);
    address buyer = address(0xB0B);
    address random = address(0xCAFE);
    uint256 constant TOTAL = 1000e18;
    uint256 constant FEE = TOTAL * 200 / 10_000; // 2% = 20e18
    uint256 constant SELLER_GETS = TOTAL - FEE; // 980e18

    function setUp() public {
        rvm = new RVM(admin, 1_000_000e18);
        treasury = new Treasury(admin);
        productNft = new ProductNFT(admin);
        serviceVoucher = new ServiceVoucher(admin);
        vault = new EscrowVault(
            admin, address(rvm), payable(address(treasury)), address(productNft), address(serviceVoucher)
        );

        // Roles: vault precisa queimar/redeem vouchers de serviço na liberação/cancelamento.
        serviceVoucher.grantRole(serviceVoucher.BURNER_ROLE(), address(vault));
        // W1: amarra o ProductNFT ao vault (one-shot) para mintToEscrow.
        productNft.setEscrowVault(address(vault));

        // Capitaliza o comprador (quem paga RVM).
        rvm.transfer(buyer, 10_000e18);
    }

    // ───────── PRODUTO: troca completa com taxa 2% ─────────

    function test_ProductTrade_FullRelease_Fee2pct() public {
        (uint256 tradeId, uint256 tokenId) = _fundProductTrade(TOTAL);

        uint256 sellerBefore = rvm.balanceOf(seller);
        vm.prank(seller); // vendedor confirma entrega → release
        vault.release(tradeId);

        assertEq(rvm.balanceOf(seller) - sellerBefore, SELLER_GETS, "vendedor recebe total - 2%");
        assertEq(rvm.balanceOf(address(treasury)), FEE, "treasury recebe taxa 2%");
        assertEq(productNft.ownerOf(tokenId), buyer, "NFT vai ao comprador");
        assertEq(rvm.balanceOf(address(vault)), 0, "vault sem saldo residual");
        EscrowVault.Trade memory t = vault.getTrade(tradeId);
        assertEq(uint8(t.state), uint8(EscrowVault.State.Released));
    }

    function test_ProductTrade_ReleaseByBuyer_Cooperative() public {
        (uint256 tradeId,) = _fundProductTrade(TOTAL);
        vm.prank(buyer); // comprador também pode liberar (cooperativo)
        vault.release(tradeId);
        assertEq(rvm.balanceOf(address(treasury)), FEE);
    }

    function test_ProductTrade_ReleaseByAnyone_AfterWindow() public {
        (uint256 tradeId,) = _fundProductTrade(TOTAL);
        // Antes da janela: não-parte não pode liberar.
        vm.prank(random);
        vm.expectRevert(EscrowVault.EscrowVault__NotAuthorized.selector);
        vault.release(tradeId);

        // Após 72h: qualquer um força a liberação (auto).
        vm.warp(block.timestamp + 73 hours);
        vm.prank(random);
        vault.release(tradeId);
        assertEq(rvm.balanceOf(address(treasury)), FEE);
    }

    // ───────── Disputa → árbitro ─────────

    function test_ProductTrade_Dispute_ArbitratorReleases() public {
        (uint256 tradeId,) = _fundProductTrade(TOTAL);

        vm.prank(buyer);
        vault.openDispute(tradeId);
        EscrowVault.Trade memory tD = vault.getTrade(tradeId);
        assertEq(uint8(tD.state), uint8(EscrowVault.State.Disputed));

        // Árbitro decide a favor do vendedor (libera).
        vault.claimArbitrator(tradeId, true);
        assertEq(rvm.balanceOf(seller), SELLER_GETS);
        assertEq(rvm.balanceOf(address(treasury)), FEE);
    }

    function test_ProductTrade_Dispute_ArbitratorRefunds() public {
        (uint256 tradeId, uint256 tokenId) = _fundProductTrade(TOTAL);
        uint256 buyerBefore = rvm.balanceOf(buyer);

        vm.prank(buyer);
        vault.openDispute(tradeId);
        // Árbitro decide a favor do comprador (reembolsa + NFT volta ao vendedor).
        vault.claimArbitrator(tradeId, false);
        assertEq(rvm.balanceOf(buyer) - buyerBefore, TOTAL, "comprador reembolsado integral");
        assertEq(productNft.ownerOf(tokenId), seller, "NFT devolvido ao vendedor");
        assertEq(rvm.balanceOf(address(treasury)), 0, "sem taxa no reembolso");
    }

    function testRevert_Dispute_AfterWindow() public {
        (uint256 tradeId,) = _fundProductTrade(TOTAL);
        vm.warp(block.timestamp + 73 hours);
        vm.prank(buyer);
        vm.expectRevert(EscrowVault.EscrowVault__WindowExpired.selector);
        vault.openDispute(tradeId);
    }

    function testRevert_ClaimArbitrator_NotArbitrator() public {
        (uint256 tradeId,) = _fundProductTrade(TOTAL);
        vm.prank(buyer);
        vault.openDispute(tradeId);
        vm.prank(random);
        vm.expectRevert(); // AccessControl: não tem ARBITRATOR_ROLE
        vault.claimArbitrator(tradeId, true);
    }

    // ───────── Cancel / refund ─────────

    function test_ProductTrade_Cancel_BeforeFunding() public {
        uint256 tokenId = productNft.mintToEscrow(address(vault), 1, "uri");
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, TOTAL, EscrowVault.AssetKind.Product, address(productNft), tokenId);

        vm.prank(seller);
        vault.cancel(tradeId);
        assertEq(productNft.ownerOf(tokenId), seller, "NFT devolvido ao vendedor (cancel pre-funding)");
        EscrowVault.Trade memory t = vault.getTrade(tradeId);
        assertEq(uint8(t.state), uint8(EscrowVault.State.Cancelled));
    }

    function test_ProductTrade_Cancel_AfterFunding_RefundsBuyer() public {
        (uint256 tradeId, uint256 tokenId) = _fundProductTrade(TOTAL);
        uint256 buyerBefore = rvm.balanceOf(buyer);

        vm.prank(buyer);
        vault.cancel(tradeId);
        assertEq(rvm.balanceOf(buyer) - buyerBefore, TOTAL, "comprador reembolsado");
        assertEq(productNft.ownerOf(tokenId), seller, "NFT devolvido ao vendedor");
    }

    // ───────── Doação (valor 0): só transfere ativo, sem taxa ─────────

    function test_Donation_Product_ZeroValue_NoFee() public {
        uint256 tokenId = productNft.mintToEscrow(address(vault), 1, "uri");
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, 0, EscrowVault.AssetKind.Product, address(productNft), tokenId);

        // fundTrade com total=0: não move RVM (doação)
        vm.prank(buyer);
        vault.fundTrade(tradeId);

        uint256 treasuryBefore = rvm.balanceOf(address(treasury));
        uint256 sellerBefore = rvm.balanceOf(seller);
        vm.prank(seller);
        vault.release(tradeId);

        assertEq(productNft.ownerOf(tokenId), buyer, "doacao transfere o NFT ao receptor");
        assertEq(rvm.balanceOf(address(treasury)) - treasuryBefore, 0, "doacao NAO paga taxa");
        assertEq(rvm.balanceOf(seller) - sellerBefore, 0, "doacao NAO creditou RVM ao doador");
        assertEq(rvm.balanceOf(address(vault)), 0);
    }

    function test_Donation_Service_Voluntariado_ZeroValue_RedeemsVoucher() public {
        uint256 vId = serviceVoucher.mintOnPurchase(buyer, 7, uint64(block.timestamp + 30 days));
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, 0, EscrowVault.AssetKind.Service, address(serviceVoucher), vId);

        vm.prank(buyer);
        vault.fundTrade(tradeId);

        uint256 treasuryBefore = rvm.balanceOf(address(treasury));
        vm.prank(buyer); // voluntário (receptor) confirma
        vault.release(tradeId);

        // W13: liberação de serviço registra consumo (redeem); voucher permanece como comprovante.
        assertTrue(serviceVoucher.getVoucher(vId).redeemed, "voucher marcado como redeemado");
        assertEq(serviceVoucher.balanceOf(buyer, vId), 1, "voucher permanece (comprovante)");
        assertEq(rvm.balanceOf(address(treasury)) - treasuryBefore, 0, "voluntariado NAO paga taxa");
    }

    // ───────── Serviço: troca completa com voucher redeem (W13) ─────────

    function test_ServiceTrade_FullRelease_RedeemsVoucher() public {
        uint256 vId = serviceVoucher.mintOnPurchase(buyer, 7, uint64(block.timestamp + 30 days));
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, TOTAL, EscrowVault.AssetKind.Service, address(serviceVoucher), vId);

        vm.startPrank(buyer);
        rvm.approve(address(vault), TOTAL);
        vault.fundTrade(tradeId);
        vm.stopPrank();

        uint256 sellerBefore = rvm.balanceOf(seller);
        vm.prank(buyer);
        vault.release(tradeId);

        assertEq(rvm.balanceOf(seller) - sellerBefore, SELLER_GETS);
        assertEq(rvm.balanceOf(address(treasury)), FEE);
        // W13: liberação registra consumo (redeem); voucher permanece como comprovante.
        assertTrue(serviceVoucher.getVoucher(vId).redeemed, "voucher redeemado");
        assertEq(serviceVoucher.balanceOf(buyer, vId), 1, "voucher permanece (comprovante)");
    }

    function test_ServiceTrade_Cancel_BurnsInvalidVoucher() public {
        uint256 vId = serviceVoucher.mintOnPurchase(buyer, 7, uint64(block.timestamp + 30 days));
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, TOTAL, EscrowVault.AssetKind.Service, address(serviceVoucher), vId);

        vm.startPrank(buyer);
        rvm.approve(address(vault), TOTAL);
        vault.fundTrade(tradeId);
        vm.stopPrank();

        uint256 buyerBefore = rvm.balanceOf(buyer);
        vm.prank(buyer);
        vault.cancel(tradeId);
        assertEq(rvm.balanceOf(buyer) - buyerBefore, TOTAL, "comprador reembolsado");
        assertEq(serviceVoucher.balanceOf(buyer, vId), 0, "voucher invalidado/queimado no cancel");
    }

    // ───────── Serviço: W11 (auto-release requer redeem; reembolso por expiração) ─────────

    function test_ServiceTrade_AutoRelease_BlockedWithoutRedeem() public {
        uint256 vId = serviceVoucher.mintOnPurchase(buyer, 7, uint64(block.timestamp + 30 days));
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, TOTAL, EscrowVault.AssetKind.Service, address(serviceVoucher), vId);

        vm.startPrank(buyer);
        rvm.approve(address(vault), TOTAL);
        vault.fundTrade(tradeId);
        vm.stopPrank();

        // Após 72h, terceiro NÃO pode auto-liberar serviço não-redeemado.
        vm.warp(block.timestamp + 73 hours);
        vm.prank(random);
        vm.expectRevert(EscrowVault.EscrowVault__NotRedeemed.selector);
        vault.release(tradeId);

        // Mas após redeem (serviço confirmado), terceiro pode liberar.
        vm.prank(buyer);
        serviceVoucher.redeem(vId);
        vm.prank(random);
        vault.release(tradeId);
        assertEq(rvm.balanceOf(seller), SELLER_GETS, "auto-release apos redeem paga vendedor");
    }

    function test_ServiceTrade_ClaimExpired_RefundsBuyer() public {
        uint256 vId = serviceVoucher.mintOnPurchase(buyer, 7, uint64(block.timestamp + 30 days));
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, TOTAL, EscrowVault.AssetKind.Service, address(serviceVoucher), vId);

        vm.startPrank(buyer);
        rvm.approve(address(vault), TOTAL);
        vault.fundTrade(tradeId);
        vm.stopPrank();

        uint256 buyerBefore = rvm.balanceOf(buyer);

        // Antes de expirar: claimExpired falha.
        vm.prank(buyer);
        vm.expectRevert(EscrowVault.EscrowVault__NotExpired.selector);
        vault.claimExpired(tradeId);

        // Após 30d sem redeem: qualquer um (auto) reembolsa o comprador + queima voucher.
        vm.warp(block.timestamp + 31 days);
        vm.prank(random);
        vault.claimExpired(tradeId);

        assertEq(rvm.balanceOf(buyer) - buyerBefore, TOTAL, "comprador reembolsado na expiracao");
        assertEq(serviceVoucher.balanceOf(buyer, vId), 0, "voucher queimado (invalidado)");
        EscrowVault.Trade memory t = vault.getTrade(tradeId);
        assertEq(uint8(t.state), uint8(EscrowVault.State.Refunded));
    }

    // ───────── Validações / reverts ─────────

    function testRevert_FundTrade_NotBuyer() public {
        uint256 tokenId = productNft.mintToEscrow(address(vault), 1, "uri");
        vm.prank(seller);
        uint256 tradeId =
            vault.createTrade(buyer, TOTAL, EscrowVault.AssetKind.Product, address(productNft), tokenId);

        vm.prank(random);
        vm.expectRevert(EscrowVault.EscrowVault__NotBuyer.selector);
        vault.fundTrade(tradeId);
    }

    function testRevert_CreateTrade_AssetNotInVault() public {
        // NFT mintado ao vault (W1), depois transferido para fora → criação falha.
        uint256 tokenId = productNft.mintToEscrow(address(vault), 1, "uri");
        vm.prank(address(vault));
        productNft.safeTransferFrom(address(vault), seller, tokenId);
        vm.prank(seller);
        vm.expectRevert(EscrowVault.EscrowVault__AssetNotInVault.selector);
        vault.createTrade(buyer, TOTAL, EscrowVault.AssetKind.Product, address(productNft), tokenId);
    }

    function testRevert_Release_BadState() public {
        (uint256 tradeId,) = _fundProductTrade(TOTAL);
        vm.prank(seller);
        vault.release(tradeId);
        // já Released → segunda liberação falha
        vm.prank(seller);
        vm.expectRevert(EscrowVault.EscrowVault__BadState.selector);
        vault.release(tradeId);
    }

    function test_Window_Is72Hours() public view {
        assertEq(uint256(vault.DISPUTE_WINDOW()), 72 hours);
        assertEq(vault.FEE_BPS(), 200);
    }

    // ───────── Helper: produto financiado ─────────

    function _fundProductTrade(uint256 total) internal returns (uint256 tradeId, uint256 tokenId) {
        tokenId = productNft.mintToEscrow(address(vault), 1, "ipfs://meta");
        vm.prank(seller);
        tradeId =
            vault.createTrade(buyer, total, EscrowVault.AssetKind.Product, address(productNft), tokenId);

        vm.startPrank(buyer);
        rvm.approve(address(vault), total);
        vault.fundTrade(tradeId);
        vm.stopPrank();
    }
}
