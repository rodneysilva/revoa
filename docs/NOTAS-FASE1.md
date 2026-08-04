# Notas — Fase 1 (M3 + M5): estado (ATUALIZADO — bugs resolvidos)

> Atualizado: M3 ✅ e M5 ✅ (mint + Account funcionando; e2e provado).

## M3 — Chain local + deploy: ✅ CONCLUÍDO
- `anvil` (chain 31337) via Docker; contratos deployados via `forge script`.
- Endereços em `contracts/deployed-dev.json` + `appsettings.Development.json` (`Chain`).
- **RVM mint provado** via `cast` e, agora, **via app .NET (Nethereum)**.

## M5 — Integração .NET↔contratos (Nethereum): ✅ FUNCIONANDO (e2e provado)
Módulos: `account` + `token` + `AuthController` + `WalletCreatedEvent`. Cadeia:
`RegisterUser → UserRegisteredEvent → Account cria carteira → WalletCreatedEvent → Token faucet (RVM.mint)`.

### E2E validado (registro → faucet → saldo)
- `POST /api/auth/register` → retorna `userId` (+ needsEmail/PhoneVerification).
- Account criada e persistida; carteira EOA gerada; e-mail (MailKit→Postfix) + OTP (Zenvia) enviados.
- **Faucet mint OK on-chain** (`Mint OK tx=... block=...`); **`balanceOf = 20 RVM`** confirmado na carteira do usuário.

### Bug 1 (mint Nethereum) — RESOLVIDO ✅
- Causa: `MintAsync` usava `gas=null`; o receipt polling ficava sem minar.
- Fix: `EstimateGasAsync` antes do `SendTransactionAndWaitForReceiptAsync` (espelha o `cast send`). Logs de `Mint`/`Mint OK`/`Mint REVERTIDO` no `NethereumRvmService`.
- `WalletCreatedEventHandler` usa `CancellationToken.None` + try/catch (falha de faucet não quebra o cadastro).

### Bug 2 (Account não aparecia em `db.Accounts`) — NÃO ERA BUG ✅
- A Account **é** persistida (`AddAsync OK` no log). A "ausência" era **conflito de mongo na porta 27017 do host**:
  há um **`mongod` nativo instalado no host** (PID variável, ouvindo `127.0.0.1:27017` em IPv4) **além do** `revoa-mongo-dev` (Docker, em IPv6 `::1`/`::`).
- O app (.NET) resolve `localhost`→IPv4 e cai no **mongo nativo do host** (onde `db.revoa.Users/Accounts` são escritos), não no Docker dev.
- **Para e2e local consistente:** parar o `mongod` nativo (`Stop-Service MongoDB` / `Get-Service *mongo*`) para o app usar o `revoa-mongo-dev` (Docker), OU conectar o app explicitamente ao Docker. Não é bug de código.

## Provisório (MVP dev) — mantido
- `UserAccount.PrivateKey` em texto (carteira EOA guardada pelo backend) — **NÃO é self-custody real**; a AA (Safe+4337+webauthn) substitui depois (ADR-0002).

## Como rodar o e2e local
1. Docker up; subir `anvil`, `revoa-mongo-dev` (-p 27017:27017, --replSet rs0, init rs0), `revoa-mail` (-p 25:25). **Parar o `mongod` nativo do host** (conflito de porta).
2. `.env` com `MINIO_ROOT_PASSWORD` + `JWT_KEY` (gitignored).
3. Re-deploy contratos (se anvil reiniciou): `.\scripts\foundry.ps1 forge script script/Deploy.s.sol:DeployScript --root contracts --rpc-url http://host.docker.internal:8545 --broadcast --private-key <faucet>`.
4. `.\scripts\run-dev.ps1` (app em Development, `Email__Host=127.0.0.1`).
5. `POST http://localhost:8000/api/auth/register` (body: Nome/Email/Telefone/BirthDate/CouponCode).
6. Verificar: `db.revoa.Users/Accounts` (mongo que o app usa) e `cast call <RVM> "balanceOf(address)(uint256)" <wallet> --rpc-url http://127.0.0.1:8545` → **20 RVM**.
