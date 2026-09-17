# Whisper Desktop — Publicación
# Publica la app self-contained win-x64, incluye FFmpeg y genera el MSI (WiX).

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$RepoRoot = Split-Path -Parent $Root
$Project = Join-Path $Root "src\WhisperDesktop\WhisperDesktop.csproj"
$InstallerWxs = Join-Path $Root "installer\Package.wxs"
$FfmpegDir = Join-Path $PSScriptRoot "ffmpeg"
$PublishDir = Join-Path $Root "dist\WhisperDesktop"
$DistDir = Join-Path $Root "dist"
$MsiPath = Join-Path $DistDir "WhisperDesktop-Setup.msi"
$TempDir = Join-Path $env:TEMP "WhisperDesktop-build"

Write-Host "==> Whisper Desktop publish v$Version" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $FfmpegDir, $TempDir, $DistDir | Out-Null

# --- FFmpeg ---
$ffmpegExe = Join-Path $FfmpegDir "ffmpeg.exe"
$ffprobeExe = Join-Path $FfmpegDir "ffprobe.exe"

if (-not (Test-Path $ffmpegExe) -or -not (Test-Path $ffprobeExe)) {
    Write-Host "==> Descargando FFmpeg (GitHub BtbN)…" -ForegroundColor Yellow
    $zipUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
    $zipPath = Join-Path $TempDir "ffmpeg-win64.zip"

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    & curl.exe -L --retry 3 --retry-delay 2 -o $zipPath $zipUrl
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $zipPath)) {
        throw "No se pudo descargar FFmpeg."
    }

    $extractDir = Join-Path $TempDir "ffmpeg-extract"
    if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

    $binDir = Get-ChildItem -Path $extractDir -Directory | Select-Object -First 1
    $binPath = Join-Path $binDir.FullName "bin"
    Copy-Item (Join-Path $binPath "ffmpeg.exe") $ffmpegExe -Force
    Copy-Item (Join-Path $binPath "ffprobe.exe") $ffprobeExe -Force
    Write-Host "==> FFmpeg listo en $FfmpegDir" -ForegroundColor Green
} else {
    Write-Host "==> FFmpeg ya está en $FfmpegDir" -ForegroundColor Green
}

# --- Publish ---
Write-Host "==> Publicando win-x64 self-contained…" -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $PublishDir `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -p:IncludeNativeLibrariesForSelfExtract=true

$outFfmpeg = Join-Path $PublishDir "ffmpeg"
New-Item -ItemType Directory -Force -Path $outFfmpeg | Out-Null
Copy-Item $ffmpegExe (Join-Path $outFfmpeg "ffmpeg.exe") -Force
Copy-Item $ffprobeExe (Join-Path $outFfmpeg "ffprobe.exe") -Force

# --- MSI (WiX) ---
Write-Host "==> Generando MSI con WiX…" -ForegroundColor Yellow

# Preferir WiX 5 desde dotnet tools (evita OSMF EULA de WiX 7)
$dotnetWix = Join-Path $env:USERPROFILE ".dotnet\tools\wix.exe"
if (Test-Path $dotnetWix) {
    $wixCmd = $dotnetWix
} else {
    $wixOnPath = Get-Command wix -ErrorAction SilentlyContinue
    if (-not $wixOnPath) {
        throw "No se encontró 'wix'. Instalá: dotnet tool install --global wix --version 5.0.2"
    }
    $wixCmd = $wixOnPath.Source
}

Write-Host "    Usando: $wixCmd"

Push-Location (Join-Path $Root "installer")
try {
    & $wixCmd extension add "WixToolset.UI.wixext/5.0.2"
    & $wixCmd build `
        .\Package.wxs `
        -ext WixToolset.UI.wixext `
        -arch x64 `
        -d "Version=$Version" `
        -bindpath "publish=$PublishDir" `
        -out $MsiPath
    if ($LASTEXITCODE -ne 0) {
        throw "wix build falló con código $LASTEXITCODE"
    }
} finally {
    Pop-Location
}

if (-not (Test-Path $MsiPath)) {
    throw "No se generó el MSI en $MsiPath"
}

Write-Host ""
Write-Host "Listo." -ForegroundColor Green
Write-Host "  App:  $PublishDir"
Write-Host "  MSI:  $MsiPath"
Write-Host ""
Write-Host "Para publicar en GitHub Releases:"
Write-Host "  gh release create v$Version `"$MsiPath`" --title `"Whisper Desktop v$Version`" --notes `"Instalador Windows (MSI).`""
