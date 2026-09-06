# ═══════════════════════════════════════════════════════════════
# scripts/bootstrap-dev.ps1 - Sobe/repõe o ambiente DEV do revoa
# Idempotente: recria o ambiente após reboot/reinício do anvil.
#   -Fresh : zera o banco 'revoa' (recria usuários/listings do zero).
#
# Faz: anvil → deploya contratos (se chain fresca) → mongo →
#      backend → seed mocks → usuários de teste → frontend → dev.revoa.org.
# Uso:  .\scripts\bootstrap-dev.ps1            # só sobe/garante
#       .\scripts\bootstrap-dev.ps1 -Fresh      # ambiente limpo/reproduzível
# ═══════════════════════════════════════════════════════════════
[CmdletBinding()]
param([switch]$Fresh)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot\..").Path
$rpcLocal = 'http://127.0.0.1:8545'             # host → anvil
$rpcContainer = 'http://host.docker.internal:8545' # container foundry → host → anvil
$account0Key = '0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80'
$rvmAddr = '0x5FbDB2315678afecb367f032d93F642f64180aa3'
$foundry = Join-Path $repo 'scripts\foundry.ps1'
$runDev = Join-Path $repo 'scripts\run-dev.ps1'
$mongoUri = 'mongodb://127.0.0.1:27017/revoa'
$mongosh = "$env:LOCALAPPDATA\Programs\mongosh\mongosh.exe"
$infraDir = 'C:\Users\rodne\projetosia\infra'
$frontendDir = Join-Path $repo 'frontend'

# Usuários de teste (recriados com -Fresh).
$testUsers = @(
    @{ Nome='Rodney Silva'; Email='rodneydocarmo@gmail.com'; Tel='11964775391'; Nasc='1987-07-10' }
    @{ Nome='Ana Trocadeira'; Email='ana.troca@revoa.dev'; Tel='11910000001'; Nasc='1990-01-01' }
    @{ Nome='Bruno Voluntario'; Email='bruno.voluntario@revoa.dev'; Tel='11910000002'; Nasc='1991-02-02' }
)

function Step($m){ Write-Host "`n==> $m" -ForegroundColor Cyan }
function Ok($m){ Write-Host "  OK $m" -ForegroundColor Green }
function Warn($m){ Write-Host "  !  $m" -ForegroundColor Yellow }
function Err($m){ Write-Host "  X  $m" -ForegroundColor Red }

function Test-Rpc {
    try { Invoke-RestMethod $rpcLocal -Method Post -Body '{"jsonrpc":"2.0","id":1,"method":"eth_blockNumber","params":[]}' -ContentType 'application/json' -TimeoutSec 5 | Out-Null; $true } catch { $false }
}
function Wait-Until($check, $secs, $msg) {
    for ($i=0; $i -lt $secs; $i++) { if (& $check) { return $true }; Start-Sleep -Seconds 1 }
    Warn "$msg (timeout ${secs}s)"; return $false
}

# ── 1) Docker ───────────────────────────────────────────────────
Step '1) Docker Desktop'
$dockerOk = $false
try { docker version | Out-Null; $dockerOk = $true } catch {}
if (-not $dockerOk) { Err 'Docker nao esta rodando. Inicie o Docker Desktop.'; exit 1 }
Ok 'Docker ativo'

# ── 2) anvil (chain 31337) ──────────────────────────────────────
Step '2) anvil (chain 31337)'
$anvil = docker ps -a --filter "name=revoa-anvil" --format '{{.Names}}' 2>$null
if (-not $anvil) {
    Warn 'Container revoa-anvil ausente - criando...'
    docker run -d --name revoa-anvil -p 8545:8545 ghcr.io/foundry-rs/foundry:latest -c 'anvil --host 0.0.0.0 --port 8545 --chain-id 31337 --block-time 1' | Out-Null
} elseif (-not (docker ps --filter "name=revoa-anvil" -q)) {
    docker start revoa-anvil | Out-Null
}
if (Wait-Until { Test-Rpc } 30 'anvil respondendo') { Ok 'anvil no ar (127.0.0.1:8545)' } else { Err 'anvil nao respondeu'; exit 1 }

# Garante o Postfix (revoa-mail) — sem ele o MailKit falha no envio do email de cadastro → 500.
if (docker ps -a --filter "name=revoa-mail" -q 2>$null) { docker start revoa-mail 2>&1 | Out-Null; Ok 'Postfix (revoa-mail) no ar (:25)' }

# ── 3) Contratos (deploy só se chain fresca) ────────────────────
Step '3) Contratos'
$code = & $foundry cast code $rvmAddr --rpc-url $rpcContainer 2>$null
if ($code -and $code -ne '0x' -and $code.Length -gt 4) {
    Ok 'Contratos ja deployados (RVM com codigo). Skip deploy.'
} else {
    Warn 'Chain fresca - deployando contratos...'
    & $foundry "cd contracts && forge script script/Deploy.s.sol --rpc-url $rpcContainer --broadcast --private-key $account0Key" 2>&1 | Out-Null
    $code2 = & $foundry cast code $rvmAddr --rpc-url $rpcContainer 2>$null
    if ($code2 -and $code2 -ne '0x' -and $code2.Length -gt 4) { Ok 'Contratos deployados (enderecos deterministicos batem com appsettings).' }
    else { Err 'Deploy falhou (RVM sem codigo).'; exit 1 }
}

# ── 4) Mongo (127.0.0.1:27017) ──────────────────────────────────
Step '4) Mongo (127.0.0.1:27017)'
$mongoUp = $false
try { $mongoUp = [bool](Test-NetConnection 127.0.0.1 -Port 27017 -WarningAction SilentlyContinue).TcpTestSucceeded } catch {}
if (-not $mongoUp) {
    # tenta iniciar o container dev, senão o serviço nativo
    if (docker ps -a --filter "name=revoa-mongo-dev" -q 2>$null) { docker start revoa-mongo-dev | Out-Null }
    elseif (Get-Service MongoDB -ErrorAction SilentlyContinue) { Start-Service MongoDB }
}
Start-Sleep -Seconds 2
$mongoUp = try { [bool](Test-NetConnection 127.0.0.1 -Port 27017 -WarningAction SilentlyContinue).TcpTestSucceeded } catch { $false }
if ($mongoUp) { Ok 'Mongo em 127.0.0.1:27017' } else { Err 'Mongo nao responde em 27017. Inicie o mongod nativo ou o container revoa-mongo-dev.'; exit 1 }

# ── 5) Wipe (-Fresh) ────────────────────────────────────────────
if ($Fresh) {
    Step '5) Wipe banco (revoa) - Fresh'
    if (Test-Path $mongosh) { & $mongosh --quiet $mongoUri --eval 'db.dropDatabase()' 2>&1 | Out-Null; Ok 'Banco revoa zerado.' }
    else { Warn 'mongosh nao encontrado - pule o wipe manualmente.' }
}

# ── 6) Infra (dev.revoa.org via Traefik+cloudflared) ────────────
Step '6) Infra (dev.revoa.org)'
if (Test-Path $infraDir) {
    if (-not (docker ps --filter "name=traefik" -q 2>$null)) { Push-Location $infraDir; try { docker compose up -d 2>&1 | Out-Null } finally { Pop-Location } }
    if (docker ps -a --filter "name=cloudflared" -q 2>$null) { docker restart cloudflared 2>&1 | Out-Null }
    Ok 'Traefik + cloudflared (dev.revoa.org)'
} else { Warn "Infra nao encontrada em $infraDir - dev.revoa.org indisponivel." }

# ── 7) Backend (.NET :8000) ─────────────────────────────────────
Step '7) Backend (.NET :8000)'
$api = 'http://localhost:8000/health'
$beUp = $false
try { $beUp = [bool](Invoke-WebRequest $api -TimeoutSec 4 -UseBasicParsing -ErrorAction Stop) } catch {}
if (-not $beUp) {
    Warn 'Backend parado - iniciando...'
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$runDev`"" -WindowStyle Minimized
    if (Wait-Until { try { Invoke-WebRequest $api -TimeoutSec 4 -UseBasicParsing -ErrorAction Stop } catch { $false } } 90 'backend /health') { Ok 'Backend no ar (:8000)' } else { Err 'Backend nao subiu.' }
} else { Ok 'Backend ja no ar (:8000)' }

# ── 8) Seed mocks ───────────────────────────────────────────────
Step '8) Seed mocks (catálogo)'
try {
    # seed-catalog exige admin (Policy Admin): garante o usuário admin e emite JWT via dev-verify
    # (endpoint dev-only; o register pode falhar se o usuário já existe — ignorado de propósito).
    $admin = $testUsers[0]
    try { Invoke-RestMethod 'http://localhost:8000/api/auth/register' -Method Post -ContentType 'application/json' `
        -Body ('{"Nome":"' + $admin.Nome + '","Email":"' + $admin.Email + '","Telefone":"' + $admin.Tel + '","BirthDate":"' + $admin.Nasc + '"}') | Out-Null } catch {}
    $login = Invoke-RestMethod 'http://localhost:8000/api/auth/dev-verify' -Method Post -ContentType 'application/json' `
        -Body ('{"Email":"' + $admin.Email + '"}')
    $s = Invoke-RestMethod 'http://localhost:8000/api/dev/seed-catalog' -Method Post -TimeoutSec 120 `
        -Headers @{ Authorization = 'Bearer ' + $login.Token }
    Ok "Seed: $($s.total) listings ($($s.produtos) produtos + $($s.servicos) servicos), $($s.categorias) categorias."
} catch { Warn "Seed falhou: $($_.Exception.Message)" }

# ── 9) Usuários de teste (-Fresh) ───────────────────────────────
if ($Fresh) {
    Step '9) Usuários de teste (register + dev-verify)'
    foreach ($u in $testUsers) {
        try {
            Invoke-RestMethod 'http://localhost:8000/api/auth/register' -Method Post -ContentType 'application/json' `
                -Body ('{"Nome":"' + $u.Nome + '","Email":"' + $u.Email + '","Telefone":"' + $u.Tel + '","BirthDate":"' + $u.Nasc + '"}') | Out-Null
            Invoke-RestMethod 'http://localhost:8000/api/auth/dev-verify' -Method Post -ContentType 'application/json' `
                -Body ('{"Email":"' + $u.Email + '"}') | Out-Null
            Ok "$($u.Nome) <$($u.Email)> criado/verificado (carteira + RVM + ETH)"
        } catch { Warn "Usuario $($u.Email): $($_.Exception.Message)" }
    }
}

# ── 10) Frontend (Vite :5173) ───────────────────────────────────
Step '10) Frontend (Vite :5173)'
$feUp = try { [bool](Test-NetConnection 127.0.0.1 -Port 5173 -WarningAction SilentlyContinue).TcpTestSucceeded } catch { $false }
if (-not $feUp) {
    Start-Process powershell -ArgumentList "-NoProfile -Command Set-Location '$frontendDir'; npm run dev" -WindowStyle Minimized
    Ok 'Frontend iniciando (:5173)'
} else { Ok 'Frontend ja no ar (:5173)' }

# ── Resumo ──────────────────────────────────────────────────────
Write-Host "`n════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " AMBIENTE DEV PRONTO" -ForegroundColor Green
Write-Host "  app        : https://dev.revoa.org  (ou http://localhost:5173)" -ForegroundColor White
Write-Host "  api/health : http://localhost:8000/health" -ForegroundColor White
Write-Host "  login dev  : botao 'Entrar direto (dev)' com um e-mail verificado" -ForegroundColor White
Write-Host "  anvil      : $rpcLocal (chain 31337)" -ForegroundColor White
if ($Fresh) { Write-Host "  usuarios   : rodneydocarmo@gmail.com / ana.troca@revoa.dev / bruno.voluntario@revoa.dev" -ForegroundColor White }
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan

