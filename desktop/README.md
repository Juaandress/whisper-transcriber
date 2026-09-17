# Whisper Desktop

App WPF (.NET 8) para transcribir audio y video en Windows.

## Usuario final

Descargá el instalador desde la página principal del repo:

**[Descargar WhisperDesktop-Setup.msi](https://github.com/Juaandress/whisper-transcriber/releases/latest/download/WhisperDesktop-Setup.msi)**

## Desarrolladores

Ver la sección **Desarrollo (.NET)** del [README principal](../readme.md).

```powershell
# Ejecutar
dotnet run --project src\WhisperDesktop\WhisperDesktop.csproj

# Publicar + MSI
.\tools\publish.ps1 -Version 1.0.0
```
