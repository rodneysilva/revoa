// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {Script} from "forge-std/Script.sol";
import {RVM} from "../src/RVM.sol";
import {Treasury} from "../src/Treasury.sol";
import {ProductNFT} from "../src/ProductNFT.sol";
import {ServiceVoucher} from "../src/ServiceVoucher.sol";
import {EscrowVault} from "../src/EscrowVault.sol";
import {CouponRedeemer} from "../src/CouponRedeemer.sol";

/// @title Deploy.s.sol — Orquestra o deploy dos contratos do revoa.
/// @notice Ordem: RVM → Treasury → ProductNFT → ServiceVoucher → EscrowVault → CouponRedeemer.
/// Concessão de roles: MINTER (RVM) ao EscrowVault e CouponRedeemer; BURNER (ServiceVoucher) ao EscrowVault.
/// Uso: forge script script/Deploy.s.sol --rpc-url <rpc> --broadcast --private-key <key>
contract DeployScript is Script {
    // Parâmetros ajustáveis via env (defaults p/ dev local/anvil).
    address admin = vm.envOr("DEPLOY_ADMIN", address(0));
    uint256 initialSupply = vm.envOr("RVM_INITIAL_SUPPLY", uint256(0)); // faucet/tesouraria; 0 = mint sob demanda

    function run() public returns (Deployed memory d) {
        if (admin == address(0)) admin = msg.sender; // fallback: deployer

        vm.startBroadcast();

        d.rvm = new RVM(admin, initialSupply);
        d.treasury = new Treasury(admin);
        d.productNft = new ProductNFT(admin);
        d.serviceVoucher = new ServiceVoucher(admin);
        d.escrowVault = new EscrowVault(
            admin, address(d.rvm), payable(address(d.treasury)), address(d.productNft), address(d.serviceVoucher)
        );
        d.couponRedeemer = new CouponRedeemer(admin, address(d.rvm));

        // ── Roles ──
        // RVM MINTER: CouponRedeemer (resgate de cupom) + EscrowVault (flexibilidade p/ bônus futuro).
        RVM(d.rvm).grantRole(RVM(d.rvm).MINTER_ROLE(), address(d.couponRedeemer));
        RVM(d.rvm).grantRole(RVM(d.rvm).MINTER_ROLE(), address(d.escrowVault));
        // ServiceVoucher BURNER: EscrowVault queima vouchers na liberação/cancelamento.
        ServiceVoucher(d.serviceVoucher).grantRole(
            ServiceVoucher(d.serviceVoucher).BURNER_ROLE(), address(d.escrowVault)
        );
        // ARBITRATOR (já concedido ao admin no construtor) — conceder a multisig/conta de moderação aqui.

        vm.stopBroadcast();

        // Log dos endereços p/ o Indexer/backend.
        // forge-std: console.log
        // (mantido comentado p/ reduzir ruído; os retornos já estão no struct Deployed)
    }

    struct Deployed {
        RVM rvm;
        Treasury treasury;
        ProductNFT productNft;
        ServiceVoucher serviceVoucher;
        EscrowVault escrowVault;
        CouponRedeemer couponRedeemer;
    }
}
