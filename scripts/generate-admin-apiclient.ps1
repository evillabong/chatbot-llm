#requires -Version 7
<#
.SYNOPSIS
  Regenera el cliente Kiota (Mimo.Admin.Api.Sdk) desde el OpenAPI vivo de Mimo.Admin.Api.

.DESCRIPTION
  1. Levanta Mimo.Admin.Api en Development (en segundo plano) en un puerto http temporal.
  2. Descarga el documento OpenAPI a src/Mimo.Admin.Api.Sdk/openapi/mimo-admin-api.json.
  3. Detiene la API.
  4. Ejecuta `kiota generate` (namespace Mimo.Admin.Api.Sdk, clase MimoAdminApiClient).

  Requiere la herramienta global `kiota`.
#>
param(
    [int]$Port = 5098
)

$ErrorActionPreference = 'Stop'
$repo       = Split-Path $PSScriptRoot -Parent
$apiProj    = Join-Path $repo 'src/Mimo.Admin.Api/Mimo.Admin.Api.csproj'
$specPath   = Join-Path $repo 'src/Mimo.Admin.Api.Sdk/openapi/mimo-admin-api.json'
$clientDir  = Join-Path $repo 'src/Mimo.Admin.Api.Sdk/Generated'
$openApiUrl = "http://localhost:$Port/openapi/v1.json"

New-Item -ItemType Directory -Force -Path (Split-Path $specPath -Parent) | Out-Null

Write-Host "Levantando Mimo.Admin.Api en :$Port ..." -ForegroundColor Cyan
$proc = Start-Process -FilePath 'dotnet' `
    -ArgumentList 'run','--project',$apiProj,'--no-launch-profile' `
    -PassThru -WindowStyle Hidden `
    -Environment @{ ASPNETCORE_ENVIRONMENT = 'Development'; ASPNETCORE_URLS = "http://localhost:$Port" }

try {
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        if ((Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue).TcpTestSucceeded) { $ready = $true; break }
        Start-Sleep -Milliseconds 1000
    }
    if (-not $ready) { throw "La API no respondió en el puerto $Port." }
    Start-Sleep -Seconds 2

    Write-Host "Descargando OpenAPI -> $specPath" -ForegroundColor Cyan
    Invoke-WebRequest -Uri $openApiUrl -OutFile $specPath -UseBasicParsing
}
finally {
    Write-Host "Deteniendo la API ..." -ForegroundColor Cyan
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
}

Write-Host "Regenerando el cliente con kiota ..." -ForegroundColor Cyan
kiota generate `
    --openapi $specPath `
    --language CSharp `
    --class-name MimoAdminApiClient `
    --namespace-name Mimo.Admin.Api.Sdk `
    --output $clientDir `
    --exclude-backward-compatible `
    --type-access-modifier Public `
    --clean-output `
    --log-level Warning

Write-Host "Cliente admin regenerado. Revisa los cambios con git diff." -ForegroundColor Green
