# Whisper Desktop

App de Windows para transcribir **audio y video** en tu PC, sin subir nada a internet (salvo la descarga del modelo la primera vez).

No necesitás saber programar, ni instalar Docker, ni Python.

## Cómo usarla (usuario final)

1. Abrí la carpeta `WhisperDesktop` (la que te pasaron o la de `desktop\dist\WhisperDesktop` tras publicar).
2. Ejecutá **WhisperDesktop.exe**.
3. Arrastrá un archivo (`.m4a`, `.mp3`, `.mp4`, `.wav`, etc.) o tocá **Elegir archivos**.
4. Dejá el idioma en **Español** (o el que corresponda).
5. Tocá **Transcribir**.
6. Esperá: vas a ver estados como *Convirtiendo audio*, *Descargando modelo*, *Transcribiendo…*.
7. Cuando diga **Listo**, abrí el `.txt` o la carpeta de salida (`Documentos\Transcripciones` por defecto).

### Formatos soportados

Video: `.mp4`, `.mkv`, `.mov`, `.avi`, `.webm`  
Audio: `.m4a`, `.mp3`, `.wav`, `.flac`, `.ogg`, `.aac`

### Requisitos

- Windows 10 u 11 (64 bits)
- Unos 2–4 GB libres en disco (modelo + temporales)
- Conexión a internet **solo la primera vez** (descarga del modelo Whisper)
- En CPU, archivos largos pueden tardar varios minutos

## Cómo publicar (quien arma la distribución)

Desde PowerShell:

```powershell
cd desktop\tools
.\publish.ps1
```

Eso:

1. Descarga FFmpeg si hace falta
2. Publica la app self-contained en `desktop\dist\WhisperDesktop`
3. Incluye `ffmpeg.exe` y `ffprobe.exe`

Podés comprimir esa carpeta en un ZIP y compartirla.

## Desarrollo

```powershell
cd desktop
dotnet run --project src\WhisperDesktop\WhisperDesktop.csproj
```

Si falta FFmpeg en desarrollo, ejecutá antes `tools\publish.ps1` (deja `tools\ffmpeg\`) o instalá FFmpeg en el PATH.

## Relación con Docker

La carpeta raíz del repo sigue teniendo el flujo Docker original (`Dockerfile`, `entrypoint.sh`). Whisper Desktop es la alternativa nativa para usuarios no técnicos.
