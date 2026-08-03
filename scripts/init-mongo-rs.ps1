#Requires -Version 5.1
<#
.SYNOPSIS
  Inicia (ou confirma) o replica set "rs0" do MongoDB do revoa.

.DESCRIPTION
  Idempotente: se rs.status().ok == 1, pula a inicializacao.
  Usa `docker exec` no container revoa-mongo porque o MongoDB NAO publica porta
  no host (fica isolado na rede "internal" do compose). A logica de initiate eh
  escrita num arquivo .js temporario, copiado para dentro do container e
  executado por mongosh — evita problemas de aspas do PowerShell.

  O membro eh anunciado como "mongo:27017" (DNS interno do compose) para que os
  clientes (app .NET) consigam alcancar o node via mongodb://mongo:27017/?replicaSet=rs0.

.PARAMETER Container
  Nome do container do MongoDB (default: revoa-mongo).

.PARAMETER ReplSetName
  Nome do replica set (default: rs0). Deve bater com --replSet do compose.

.PARAMETER MemberHost
  Host:port pelo qual o membro eh anunciado (default: mongo:27017).

.PARAMETER TimeoutSec
  Tempo maximo de espera pela eleicao de PRIMARY (default: 60).

.EXAMPLE
  .\scripts\init-mongo-rs.ps1
#>
[CmdletBinding()]
param(
    [string]$Container   = 'revoa-mongo',
    [string]$ReplSetName = 'rs0',
    [string]$MemberHost  = 'mongo:27017',
    [int]$TimeoutSec     = 60
)

$ErrorActionPreference = 'Stop'

function Assert-ContainerRunning {
    param([string]$Name)
    $state = docker inspect -f '{{.State.Running}}' $Name 2>$null
    if ($LASTEXITCODE -ne 0 -or "$state".Trim() -ne 'true') {
        throw "Container '$Name' nao esta rodando. Suba a infra primeiro: docker compose up -d mongo"
    }
}

Assert-ContainerRunning -Name $Container

# --- Logica de initiate em JS (parametrizada via here-string) ----------------
$initJs = @"
const cfg = { _id: '${ReplSetName}', members: [ { _id: 0, host: '${MemberHost}' } ] };
let already = false;
try {
  const s = rs.status();
  if (s && s.ok === 1) { already = true; }
} catch (e) {
  // NotYetInitialized: esperado na 1a execucao
}
if (already) {
  print('RS_RESULT already_initiated');
} else {
  try {
    const r = rs.initiate(cfg);
    print('RS_RESULT initiated ok=' + (r ? r.ok : 'unknown'));
  } catch (e2) {
    print('RS_RESULT error ' + e2);
  }
}
"@

$tmpJs = Join-Path $env:TEMP 'revoa-init-rs.js'
Set-Content -LiteralPath $tmpJs -Value $initJs -Encoding ascii
docker cp $tmpJs "${Container}:/tmp/revoa-init-rs.js" | Out-Null

Write-Host "[mongo-rs] Verificando/iniciando replica set '$ReplSetName'..."
docker exec $Container mongosh --quiet /tmp/revoa-init-rs.js 2>&1 |
    ForEach-Object { Write-Host "  $_" }

# --- Aguarda eleicao de PRIMARY ----------------------------------------------
Write-Host "[mongo-rs] Aguardando eleicao de PRIMARY (timeout ${TimeoutSec}s)..."
$deadline = (Get-Date).AddSeconds($TimeoutSec)
$primary  = $false
while ((Get-Date) -lt $deadline) {
    $res = docker exec $Container mongosh --quiet --eval 'print(db.hello().isWritablePrimary ? 1 : 0)' 2>$null
    if ("$res".Trim() -eq '1') { $primary = $true; break }
    Start-Sleep -Seconds 2
}
if (-not $primary) { Write-Warning "[mongo-rs] PRIMARY nao alcancado em ${TimeoutSec}s." }

# --- Validacao final ----------------------------------------------------------
$ok      = (docker exec $Container mongosh --quiet --eval 'print(rs.status().ok)' 2>$null)
$myState = (docker exec $Container mongosh --quiet --eval 'print(rs.status().myState)' 2>$null)
$ok      = "$ok".Trim()
$myState = "$myState".Trim()
Write-Host "[mongo-rs] rs.status().ok = $ok ; myState = $myState (1 = PRIMARY)"

if ($ok -eq '1') {
    Write-Host "[mongo-rs] OK - replica set pronto para transacoes ACID."
} else {
    Write-Error "[mongo-rs] FALHA: rs.status().ok != 1"
}
