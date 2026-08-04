# scripts/run-dev.ps1 — roda o Revoa.Api em Development para e2e local
# (Email__Host aponta para o Postfix local mapeado em 127.0.0.1:25)
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Email__Host = '127.0.0.1'
Set-Location "$PSScriptRoot\.."
dotnet run --project src/Revoa.Api --no-launch-profile
