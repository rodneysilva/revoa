# scripts/foundry.ps1 — wrapper Foundry via Docker (ghcr.io/foundry-rs/foundry)
# Evita instalar Foundry no host Windows; usa a imagem oficial.
# A imagem tem ENTRYPOINT sh -c, então juntamos os args em UMA string (lida com espaços).
#
# Uso:
#   .\scripts\foundry.ps1 forge --version
#   .\scripts\foundry.ps1 forge init contracts --no-commit
#   .\scripts\foundry.ps1 forge build
#   .\scripts\foundry.ps1 forge test
#   .\scripts\foundry.ps1 forge coverage
#   .\scripts\foundry.ps1 anvil                        # interativo: usar em terminal proprio
#   .\scripts\foundry.ps1 cast block-number --rpc-url http://host.docker.internal:8545
#
# Obs: o volume monta a raiz do repo em /workspace; o forge escreve em contracts/ no host.
param([Parameter(ValueFromRemainingArguments = $true)][string[]] $FwdArgs)

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if (-not $FwdArgs) {
    Write-Host "Uso: .\scripts\foundry.ps1 <comando foundry> [args...]" -ForegroundColor Yellow
    Write-Host "Ex:  .\scripts\foundry.ps1 forge test"
    exit 1
}

$cmd = ($FwdArgs -join " ")

docker run --rm `
    -v "${repo}:/workspace" `
    -w /workspace `
    --entrypoint sh `
    ghcr.io/foundry-rs/foundry:latest `
    -c $cmd

exit $LASTEXITCODE
