#!/bin/bash

set -e

# Directorios montados
VIDEO_DIR="/app/videos"
OUTPUT_DIR="/app/transcriptions"

echo "Buscando archivos de video/audio en $VIDEO_DIR..."

shopt -s nullglob
for media in "$VIDEO_DIR"/*.{mp4,mkv,mov,avi,m4a,mp3,wav,flac,ogg,aac,webm} ; do
  filename=$(basename -- "$media")
  name="${filename%.*}"
  echo "Procesando: $filename"

  # Convertir/extraer audio a wav 16kHz mono (Whisper lo necesita así)
  audio="/tmp/${name}.wav"
  ffmpeg -y -i "$media" -ar 16000 -ac 1 "$audio"

  # Transcribir con Whisper (modelo base, idioma español)
  echo "Transcribiendo audio..."
  whisper "$audio" --model base --language es --output_format txt --output_dir "$OUTPUT_DIR"

  echo "Transcripción guardada en $OUTPUT_DIR/${name}.txt"
  rm "$audio"
done
shopt -u nullglob

echo "Proceso completado."