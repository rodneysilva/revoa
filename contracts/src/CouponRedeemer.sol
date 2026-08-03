// SPDX-License-Identifier: MIT
pragma solidity 0.8.24;

import {AccessControl} from "@openzeppelin/contracts/access/AccessControl.sol";
import {RVM} from "./RVM.sol";

/// @title CouponRedeemer — Cupom/convite on-chain (resgate único por carteira).
/// @notice Valida (exists, !expired, maxUses, !usedBy) → minta RVM extra na Safe do usuário.
/// Anti-sybil complementar: maxUses global + 1 resgate por carteira. Cupom é OPCIONAL (incentivo de captação).
contract CouponRedeemer is AccessControl {
    RVM public immutable rvm;

    bytes32 public constant COUPON_ADMIN_ROLE = keccak256("COUPON_ADMIN_ROLE");

    struct Coupon {
        uint256 amount; // RVM creditado no resgate
        uint32 maxUses; // 0 = ilimitado (raro)
        uint32 usedCount;
        uint64 expiry; // timestamp; 0 = sem expiração
        bool exists;
        bool revoked;
    }

    mapping(bytes32 => Coupon) public coupons; // codeHash => Coupon
    mapping(bytes32 => mapping(address => bool)) public usedBy; // codeHash => (account => usado)

    event CouponCreated(bytes32 indexed codeHash, uint256 amount, uint32 maxUses, uint64 expiry);
    event CouponRevoked(bytes32 indexed codeHash);
    event CouponRedeemed(bytes32 indexed codeHash, address indexed account, uint256 amount);

    constructor(address admin, address rvmAddr) {
        if (admin == address(0) || rvmAddr == address(0)) revert CouponRedeemer__ZeroAddress();
        rvm = RVM(rvmAddr);
        _grantRole(DEFAULT_ADMIN_ROLE, admin);
        _grantRole(COUPON_ADMIN_ROLE, admin);
    }

    /// @notice Cria um cupom a partir do código secreto (hasheado on-chain).
    function createCoupon(string calldata code, uint256 amount, uint32 maxUses, uint64 expiry)
        external
        onlyRole(COUPON_ADMIN_ROLE)
    {
        bytes32 h = _hash(code);
        if (coupons[h].exists) revert CouponRedeemer__AlreadyExists();
        if (amount == 0) revert CouponRedeemer__ZeroAmount();
        if (expiry != 0 && expiry <= block.timestamp) revert CouponRedeemer__InvalidExpiry();
        coupons[h] = Coupon({
            amount: amount,
            maxUses: maxUses,
            usedCount: 0,
            expiry: expiry,
            exists: true,
            revoked: false
        });
        emit CouponCreated(h, amount, maxUses, expiry);
    }

    /// @notice Revoga um cupom (não pode mais ser resgatado).
    function revokeCoupon(string calldata code) external onlyRole(COUPON_ADMIN_ROLE) {
        bytes32 h = _hash(code);
        if (!coupons[h].exists) revert CouponRedeemer__NotFound();
        coupons[h].revoked = true;
        emit CouponRevoked(h);
    }

    /// @notice Resgata o cupom: valida tudo → minta RVM para msg.sender → marca usado.
    function redeem(string calldata code) external {
        bytes32 h = _hash(code);
        Coupon storage c = coupons[h];
        if (!c.exists) revert CouponRedeemer__NotFound();
        if (c.revoked) revert CouponRedeemer__Revoked();
        if (c.expiry != 0 && block.timestamp > c.expiry) revert CouponRedeemer__Expired();
        if (usedBy[h][msg.sender]) revert CouponRedeemer__AlreadyUsed();
        if (c.maxUses != 0 && c.usedCount >= c.maxUses) revert CouponRedeemer__MaxUsesReached();

        usedBy[h][msg.sender] = true;
        c.usedCount += 1;

        rvm.mint(msg.sender, c.amount);
        emit CouponRedeemed(h, msg.sender, c.amount);
    }

    function _hash(string calldata code) internal pure returns (bytes32) {
        return keccak256(abi.encodePacked(code));
    }

    error CouponRedeemer__ZeroAddress();
    error CouponRedeemer__ZeroAmount();
    error CouponRedeemer__InvalidExpiry();
    error CouponRedeemer__AlreadyExists();
    error CouponRedeemer__NotFound();
    error CouponRedeemer__Revoked();
    error CouponRedeemer__Expired();
    error CouponRedeemer__AlreadyUsed();
    error CouponRedeemer__MaxUsesReached();
}
