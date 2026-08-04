# Notas — Fase 1 (M3 + M5): estado e pendências

> Estado em 03/08/2026. Build verde; integração wired; **contrato RVM provado**. Dois bugs runtime
> pendentes no M5 (client .NET), a resolver na próxima sessão.

## M3 — Chain local + deploy: ✅ CONCLUÍDO
- `anvil` (chain 31337) subindo via Docker (`scripts/foundry.ps1` wrapper; comando de subida:
  `docker run -d --name revoa-anvil -p 8545:8545 --entrypoint sh ghcr.io/foundry-rs/foundry:latest -c "anvil --host 0.0.0.0 --port 8545 --chain-id 31337 --block-time 1"`).
- Contratos deployados via `forge script script/Deploy.s.sol:DeployScript --rpc-url http://host.docker.internal:8545 --broadcast --private-key <faucet>`. Endereços em `contracts/deployed-dev.json` e `appsettings.Development.json` (`Chain`).
- **RVM mint provado via cast** (status 1, Transfer event, balanceOf = 20 RVM). MINTER_ROLE concedida à faucet.

## M5 — Integração .NET↔contratos (Nethereum): ⚠️ IMPLEMENTADO, 2 bugs runtime pendentes
Módulos criados: `account` (Domain/Application/Infrastructure), `token` (idem), `AuthController` (register/verify-email/verify-phone), `WalletCreatedEvent`. Cadeia wired:
`RegisterUser → UserRegisteredEvent → Account cria carteira → WalletCreatedEvent → Token faucet (RVM.mint)`.

**E2E validado até:** User criado no Mongo, e-mail (MailKit→Postfix) + OTP (Zenvia stub) enviados, carteira EOA gerada, `WalletCreatedEvent` publicado, faucet recebe o evento e tenta mintar, `EnsureFaucetMinterRoleAsync` OK (hasRole=true).

### Bug 1 (bloqueante) — Mint via Nethereum não mina / polling trava
- `NethereumRvmService.MintAsync` (`SendTransactionAndWaitForReceiptAsync`) envia a tx mas **nenhum Transfer event** é gerado on-chain (a tx não é minerada), e o polling do receipt trava até timeout.
- **O contrato está OK** (mint direto via `cast` funciona: status 1 + Transfer + balanceOf).
- Hipótese: nonce/gas handling do Nethereum, ou o `SendTransactionAndWaitForReceiptAsync` não está enviando a tx corretamente.
- **Próximo passo:** debugar o `NethereumRvmService.MintAsync` (logar a tx enviada/nonce/gas; comparar com o `cast send` que funciona; considerar `EstimateGasAsync` + `SendTransactionAsync` + poll manual, ou alinhar o nonce com `EthGetTransactionCount`).
- Mitigação já aplicada: `WalletCreatedEventHandler` usa `CancellationToken.None` (não cancelável pelo request) + try/catch (falha de faucet não quebra o cadastro).

### Bug 2 — Account não aparece em `db.Accounts`
- O `UserRegisteredEventHandler` (Account) chama `AddAsync` ANTES do publish, mas `db.Accounts` fica vazio após o cadastro.
- Hipótese: o `AccountsRepository`/`AddAccountInfrastructure` está usando database ou connection diferente do `revoa` (ou a coleção mapeia para outro nome). Verificar o DI de Account (`AddAccountInfrastructure`) — connection string/database.
- **Próximo passo:** confirmar que o Account usa `Mongo:Database=revoa` e coleção `Accounts` (PascalCase); checar logs de erro do Mongo no Account handler.

## Como rodar o e2e local
1. Docker up; subir `anvil`, `revoa-mongo-dev` (-p 27017:27017, --replSet rs0, init rs0), `revoa-mail` (-p 25:25).
2. `.env` com `MINIO_ROOT_PASSWORD` + `JWT_KEY` (gitignored).
3. Re-deploy contratos: `forge script script/Deploy.s.sol:DeployScript --root contracts --rpc-url http://host.docker.internal:8545 --broadcast --private-key <faucet>` (via wrapper).
4. `.\scripts\run-dev.ps1` (app em Development, Email__Host=127.0.0.1).
5. `POST http://localhost:8000/api/auth/register` (body: Nome/Email/Telefone/BirthDate/CouponCode).
6. Verificar: `db.Users`/`db.Accounts` (mongo dev) e `cast call <RVM> "balanceOf(address)(uint256)" <wallet>`.

## Provisório (MVP dev)
- `UserAccount.PrivateKey` em texto (carteira EOA guardada pelo backend) — **NÃO é self-custody real**; a AA (Safe+4337+webauthn) substitui depois (ADR-0002).
