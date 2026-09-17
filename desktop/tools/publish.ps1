# Whisper Desktop — Publicación
# Descarga FFmpeg (essentials), publica la app self-contained win-x64 e incluye ffmpeg.exe.

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "src\WhisperDesktop\WhisperDesktop.csproj"
$FfmpegDir = Join-Path $PSScriptRoot "ffmpeg"
$PublishDir = Join-Path $Root "dist\WhisperDesktop"
$TempDir = Join-Path $env:TEMP "WhisperDesktop-build"

Write-Host "==> Whisper Desktop publish" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $FfmpegDir, $TempDir, $PublishDir | Out-Null

# --- FFmpeg ---
$ffmpegExe = Join-Path $FfmpegDir "ffmpeg.exe"
$ffprobeExe = Join-Path $FfmpegDir "ffprobe.exe"

if (-not (Test-Path $ffmpegExe) -or -not (Test-Path $ffprobeExe)) {
    Write-Host "==> Descargando FFmpeg (GitHub BtbN)…" -ForegroundColor Yellow
    # Build essentials-like shared win64 from BtbN (más estable que el zip de gyan)
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
    -p:IncludeNativeLibrariesForSelfExtract=true

# Asegurar ffmpeg en la salida
$outFfmpeg = Join-Path $PublishDir "ffmpeg"
New-Item -ItemType Directory -Force -Path $outFfmpeg | Out-Null
Copy-Item $ffmpegExe (Join-Path $outFfmpeg "ffmpeg.exe") -Force
Copy-Item $ffprobeExe (Join-Path $outFfmpeg "ffprobe.exe") -Force

# Copiar guía de usuario
$readmeSrc = Join-Path $Root "README.md"
if (Test-Path $readmeSrc) {
    Copy-Item $readmeSrc (Join-Path $PublishDir "LEEME.txt") -Force
}

Write-Host ""
Write-Host "Listo. Carpeta para distribuir:" -ForegroundColor Green
Write-Host "  $PublishDir"
Write-Host ""
Write-Host "El usuario abre WhisperDesktop.exe (no necesita instalar .NET ni Docker)."
Write-Host "La primera transcripción descargará el modelo de Whisper (~140 MB para Base)."
