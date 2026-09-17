# Whisper Transcriber

Transcripción **local** de audio y video con [OpenAI Whisper](https://github.com/openai/whisper).  
Tu contenido no se sube a la nube.

| | |
|---|---|
| **App Windows** | Instalador MSI (recomendado para usuarios finales) |
| **Docker** | Alternativa por línea de comandos |
| **Licencia** | [MIT](LICENSE) |

---

## Descargar para Windows (MSI)

[![Descargar MSI](https://img.shields.io/github/v/release/Juaandress/whisper-transcriber?label=Descargar%20MSI&logo=windows)](https://github.com/Juaandress/whisper-transcriber/releases/latest/download/WhisperDesktop-Setup.msi)

**[⬇️ Descargar WhisperDesktop-Setup.msi](https://github.com/Juaandress/whisper-transcriber/releases/latest/download/WhisperDesktop-Setup.msi)**

1. Descargá el instalador `.msi`
2. Ejecutalo (Windows 10/11, 64 bits)
3. Abrí **Whisper Desktop** desde el menú Inicio
4. Arrastrá un archivo de audio o video y tocá **Transcribir**

La primera vez descarga el modelo Whisper (~140 MB). Después funciona offline.

> El MSI se publica en [GitHub Releases](https://github.com/Juaandress/whisper-transcriber/releases) (práctica recomendada: no versionar binarios en el repo).

### Formatos soportados

**Video:** `.mp4` `.mkv` `.mov` `.avi` `.webm`  
**Audio:** `.m4a` `.mp3` `.wav` `.flac` `.ogg` `.aac`

---

## Requisitos

### App Windows
- Windows 10 u 11 (x64)
- ~2–4 GB libres (modelo + temporales)
- Internet solo la primera vez (descarga del modelo)

### Docker (opcional)
- [Docker Desktop](https://docs.docker.com/get-docker/)

---

## Desarrollo (.NET)

Stack de la app de escritorio:

- .NET 8 / WPF
- [Whisper.net](https://github.com/sandrohanea/whisper.net) (whisper.cpp)
- FFmpeg embebido
- Instalador con [WiX Toolset](https://wixtoolset.org/)

### Requisitos para compilar

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WiX CLI](https://wixtoolset.org/) (`winget install WiXToolset.WiXCLI`)

### Ejecutar en desarrollo

```powershell
cd desktop
dotnet run --project src\WhisperDesktop\WhisperDesktop.csproj
```

### Publicar app + MSI

```powershell
cd desktop\tools
.\publish.ps1 -Version 1.0.0
```

Salida:

- `desktop\dist\WhisperDesktop\` — carpeta portable
- `desktop\dist\WhisperDesktop-Setup.msi` — instalador

### Crear una release en GitHub

```powershell
gh release create v1.0.0 desktop\dist\WhisperDesktop-Setup.msi `
  --title "Whisper Desktop v1.0.0" `
  --notes "Instalador Windows (MSI)."
```

También podés etiquetar `v1.0.0` y empujar: el workflow [`.github/workflows/release.yml`](.github/workflows/release.yml) genera el MSI automáticamente.

```powershell
git tag v1.0.0
git push origin v1.0.0
```

---

## Uso con Docker

```powershell
git clone https://github.com/Juaandress/whisper-transcriber.git
cd whisper-transcriber
mkdir videos, transcriptions
# Copiá tus archivos a videos\
docker build -t whisper-transcriber .
docker run --rm -v "${PWD}\videos:/app/videos" -v "${PWD}\transcriptions:/app/transcriptions" whisper-transcriber
```

En macOS/Linux usá `$(pwd)` en lugar de `${PWD}`.

- Idioma por defecto: español (`--language es` en `entrypoint.sh`)
- Modelo por defecto: `base`

---

## Estructura del repositorio

```
whisper-transcriber/
├── dockerfile / entrypoint.sh   # Flujo Docker
├── desktop/
│   ├── src/WhisperDesktop/      # App WPF (.NET 8)
│   ├── installer/Package.wxs    # Definición del MSI (WiX)
│   └── tools/publish.ps1        # Publish + MSI
├── .github/workflows/release.yml
├── LICENSE
└── README.md
```

Las carpetas `videos/` y `transcriptions/` son locales (ignoradas por git).

---

## Privacidad

Todo el procesamiento ocurre en tu máquina. Solo se descarga el modelo de Whisper la primera vez (desde Hugging Face / repositorio oficial de ggml).

---

## Licencia

MIT — ver [LICENSE](LICENSE).  
Whisper / whisper.cpp tienen sus propias licencias open source.
