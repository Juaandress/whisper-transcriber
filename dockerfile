# Dockerfile
FROM python:3.10-slim

# Instalar ffmpeg y dependencias
RUN apt-get update && apt-get install -y ffmpeg git && rm -rf /var/lib/apt/lists/*

# Instalar Whisper (openai/whisper) y dependencias
RUN pip install --no-cache-dir -U pip
RUN pip install --no-cache-dir git+https://github.com/openai/whisper.git

# Crear directorios de trabajo
WORKDIR /app

# Copiar el script de entrada
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# Montar volúmenes para videos y transcripciones
VOLUME ["/app/videos", "/app/transcriptions"]

ENTRYPOINT ["/app/entrypoint.sh"]