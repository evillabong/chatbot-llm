#requires -Version 5.1
<#
.SYNOPSIS
    Funciones compartidas de despliegue a IIS para MIMO. Se carga con dot-source desde
    deploy-tenant.ps1 y deploy-admin.ps1. No ejecuta nada por sí solo.
#>

$ErrorActionPreference = 'Stop'
$script:Appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

function Test-Administrator {
    $principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Anillo de llaves de Data Protection compartido por las APIs (cifra/descifra API keys; ADR 0010).
# Debe ser accesible por las identidades de los App Pools de IIS.
function Initialize-DataProtectionKeys {
    $dpKeys = Join-Path $env:ProgramData 'MIMO\dp-keys'
    New-Item -ItemType Directory -Force -Path $dpKeys | Out-Null
    icacls $dpKeys /grant 'IIS_IUSRS:(OI)(CI)M' /T | Out-Null
    Write-Host "Anillo de llaves Data Protection: $dpKeys (acceso IIS_IUSRS concedido)"
}

function Test-IisSite([string]$site) {
    $found = (& $script:Appcmd list site "$site" /text:name 2>$null)
    return -not [string]::IsNullOrWhiteSpace($found)
}

function Get-IisSitePath([string]$site) {
    $path = (& $script:Appcmd list vdir "$site/" /text:physicalPath 2>$null)
    if ($path) { return $path.Trim() }
    return "C:\inetpub\$site"
}

function Get-IisSitePool([string]$site) {
    $pool = (& $script:Appcmd list app "$site/" /text:applicationPool 2>$null)
    if ($pool) { return $pool.Trim() }
    return $null
}

function Stop-IisSite([string]$site, [string]$pool) {
    if ($pool) { & $script:Appcmd set apppool "$pool" /managedRuntimeVersion:"" | Out-Null }  # No Managed Code
    & $script:Appcmd stop site "$site" 2>&1 | Out-Null
    if ($pool) { & $script:Appcmd stop apppool "$pool" 2>&1 | Out-Null }
    Start-Sleep -Seconds 2
}

function Start-IisSite([string]$site, [string]$pool) {
    if ($pool) { & $script:Appcmd start apppool "$pool" 2>&1 | Out-Null }
    & $script:Appcmd start site "$site" 2>&1 | Out-Null
}

# Despliega un proyecto ASP.NET Core (API) hospedado en IIS vía ANCM.
function Publish-AspNetApi {
    param(
        [Parameter(Mandatory)] [string]$ProjectPath,
        [Parameter(Mandatory)] [string]$Site,
        [Parameter(Mandatory)] [string]$Configuration,
        [Parameter(Mandatory)] [string]$Stage
    )
    Write-Host "=== API $Site ===" -ForegroundColor Cyan
    if (-not (Test-IisSite $Site)) { Write-Warning "El sitio IIS '$Site' no existe; omitido."; return }

    $out = Join-Path $Stage $Site
    Write-Host "Publicando ($Configuration)..."
    dotnet publish $ProjectPath -c $Configuration -o $out | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló para $ProjectPath" }

    $pool = Get-IisSitePool $Site
    $path = Get-IisSitePath $Site
    Write-Host "App Pool=[$pool]  Ruta=[$path]"

    Stop-IisSite $Site $pool
    # Preserva la config local de Producción/Development del servidor (secretos, fuera del repo).
    robocopy $out $path /E /R:2 /W:1 /NFL /NDL /NJH /NJS /XF appsettings.Production.json appsettings.Development.json | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy falló (exit $LASTEXITCODE) para $Site" }
    Start-IisSite $Site $pool

    Write-Host "Desplegada API $Site." -ForegroundColor Green
}

# Despliega una app Blazor WebAssembly (estática) a IIS. El contenido vive en publish\wwwroot.
function Publish-WasmApp {
    param(
        [Parameter(Mandatory)] [string]$ProjectPath,
        [Parameter(Mandatory)] [string]$Site,
        [Parameter(Mandatory)] [string]$Configuration,
        [Parameter(Mandatory)] [string]$Stage,
        [string]$ApiBaseUrl
    )
    Write-Host "=== WASM $Site ===" -ForegroundColor Cyan
    if (-not (Test-Path $ProjectPath)) { Write-Warning "El proyecto '$ProjectPath' no existe todavía; omitido."; return }
    if (-not (Test-IisSite $Site))     { Write-Warning "El sitio IIS '$Site' no existe; omitido."; return }

    $out     = Join-Path $Stage $Site
    $content = Join-Path $out 'wwwroot'
    Write-Host "Publicando ($Configuration)..."
    dotnet publish $ProjectPath -c $Configuration -o $out | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló para $ProjectPath" }

    # Opcional: fijar la URL del API en el appsettings.json publicado (lo consume el navegador).
    # Sin este parámetro se despliega el valor que traiga wwwroot/appsettings.json del repo.
    if ($ApiBaseUrl) {
        $settings = Join-Path $content 'appsettings.json'
        if (Test-Path $settings) {
            $json = Get-Content $settings -Raw | ConvertFrom-Json
            $json.Api.BaseUrl = $ApiBaseUrl
            $json | ConvertTo-Json -Depth 10 | Set-Content $settings
            Write-Host "Api:BaseUrl fijado a $ApiBaseUrl"
        }
    }

    $pool = Get-IisSitePool $Site
    $path = Get-IisSitePath $Site
    Write-Host "App Pool=[$pool]  Ruta=[$path]"

    Stop-IisSite $Site $pool
    # Copia espejo de TODO el publish: el web.config va en la RAÍZ del publish (junto a la carpeta
    # wwwroot), NO dentro de wwwroot. El web.config que genera el SDK de Blazor WASM reescribe las
    # peticiones hacia wwwroot\{R:0} (regla "Serve subdir") + fallback SPA, por lo que el sitio IIS debe
    # apuntar a la raíz del publish. Copiar solo wwwroot dejaba fuera el web.config → MIME incorrecto de
    # .wasm/.webcil y sin fallback SPA → el front no cargaba. /MIR purga del destino lo que ya no existe.
    robocopy $out $path /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy falló (exit $LASTEXITCODE) para $Site" }
    Start-IisSite $Site $pool

    Write-Host "Desplegada WASM $Site." -ForegroundColor Green
}
