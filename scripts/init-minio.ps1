#Requires -Version 5.1
<#
.SYNOPSIS
  Garante a existencia do bucket "revoa-assets" no MinIO do revoa (idempotente).

.DESCRIPTION
  Usa o cliente `mc` EMBUTIDO na imagem minio/minio (/usr/bin/mc), executado via
  `docker exec` no container revoa-minio. O MinIO NAO publica porta no host
  (esta na rede "internal" + "traefik_net"), entao tudo acontece dentro do container.

  Credenciais: lidas de .env (MINIO_ROOT_USER / MINIO_ROOT_PASSWORD) na raiz do repo;
  se ausentes, usa os defaults do docker-compose.yml (revoa / revoa12345).

  Idempotente: cria o bucket so se ele ainda nao existir.

.PARAMETER Container
  Nome do container do MinIO (default: revoa-minio).

.PARAMETER Endpoint
  Endpoint visto de DENTRO do container (default: http://minio:9000).

.PARAMETER Alias
  Nome do alias mc (default: revoa).

.PARAMETER Bucket
  Nome do bucket (default: revoa-assets).

.EXAMPLE
  .\scripts\init-minio.ps1
#>
[CmdletBinding()]
param(
    [string]$Container = 'revoa-minio',
    [string]$Endpoint  = 'http://minio:9000',
    [string]$Alias     = 'revoa',
    [string]$Bucket    = 'revoa-assets'
)

$ErrorActionPreference = 'Stop'

function Get-EnvOrDefault {
    param([string]$Key, [string]$Default, [string]$EnvFile)
    if (Test-Path -LiteralPath $EnvFile) {
        foreach ($line in Get-Content -LiteralPath $EnvFile) {
            if ($line -match ('^\s*' + [regex]::Escape($Key) + '\s*=\s*(.*)$')) {
                $val = $Matches[1].Trim().Trim('"').Trim("'")
                if ($val) { return $val }
            }
        }
    }
    return $Default
}

# --- Resolve credenciais (mesma logica do compose) ---------------------------
$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile  = Join-Path $repoRoot '.env'
$accessKey = Get-EnvOrDefault -Key 'MINIO_ROOT_USER'     -Default 'revoa'      -EnvFile $envFile
$secretKey = Get-EnvOrDefault -Key 'MINIO_ROOT_PASSWORD' -Default 'revoa12345' -EnvFile $envFile

# --- Valida container --------------------------------------------------------
$running = docker inspect -f '{{.State.Running}}' $Container 2>$null
if ($LASTEXITCODE -ne 0 -or "$running".Trim() -ne 'true') {
    throw "Container '$Container' nao esta rodando. Suba a infra primeiro: docker compose up -d minio"
}

# --- Configura alias mc ------------------------------------------------------
Write-Host "[minio] Alias '$Alias' -> $Endpoint (usuario: $accessKey)"
$aliasOut = docker exec $Container mc alias set $Alias $Endpoint $accessKey $secretKey 2>&1
Write-Host ("  " + ($aliasOut -join ' '))
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao configurar alias mc. Confira MINIO_ROOT_USER/MINIO_ROOT_PASSWORD em .env."
}

# --- Cria bucket se inexistente (idempotente) --------------------------------
Write-Host "[minio] Verificando bucket '$Bucket'..."
$exists = docker exec $Container sh -c "mc ls $Alias/$Bucket >/dev/null 2>&1 && echo EXISTS || echo MISSING"
$exists = "$exists".Trim()
if ($exists -eq 'EXISTS') {
    Write-Host "[minio] Bucket '$Bucket' ja existe. Pulando criacao."
} else {
    Write-Host "[minio] Bucket '$Bucket' ausente. Criando..."
    docker exec $Container mc mb "$Alias/$Bucket" 2>&1 | ForEach-Object { Write-Host "  $_" }
}

# --- Lista + valida ----------------------------------------------------------
Write-Host "[minio] Buckets em '$Alias':"
$list = docker exec $Container mc ls $Alias 2>&1
$list | ForEach-Object { Write-Host "  $_" }

if ("$list" -match [regex]::Escape($Bucket)) {
    Write-Host "[minio] OK - bucket '$Bucket' presente."
} else {
    Write-Error "[minio] FALHA - bucket '$Bucket' nao encontrado."
}
