# Whisper Transcriber - Local Video Transcription with Docker

This project enables transcription of local videos and audio files using OpenAI's *Whisper* model running inside a Docker container. All processing happens *locally*, with no data uploaded to the cloud, ensuring your content’s privacy.

**Also available:** a Windows desktop app for non-technical users in [`desktop/`](desktop/README.md) (no Docker required). Open `desktop\dist\WhisperDesktop\WhisperDesktop.exe` after running `desktop\tools\publish.ps1`.

**Supported formats:** video (`.mp4`, `.mkv`, `.mov`, `.avi`, `.webm`) and audio (`.m4a`, `.mp3`, `.wav`, `.flac`, `.ogg`, `.aac`).

---

## Features

- Extracts/converts audio with ffmpeg.
- Transcribes with Whisper (default base model).
- Supports common video and audio formats (including `.m4a`).
- Saves transcriptions as plain text (.txt) files in a local folder.
- Easy to use with Docker—no need to install Python or dependencies on the host machine.
- Configurable for different models and languages.
- Optional Windows app (WPF) with drag-and-drop and live progress.

---

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed on your system.
- Sufficient disk space for videos and transcription files.
- Media files in supported video/audio formats (see above).

---

## Project Structure

whisper-transcriber/
├── Dockerfile
├── entrypoint.sh
├── videos/ # Folder to place your videos
├── transcriptions/ # Folder where transcriptions will be saved
└── README.md

---

## How to Use

### 1. Clone or download the project

git clone <https://your-repository.git>
cd whisper-transcriber

### 2. Create folders for videos and transcriptions

mkdir -p videos transcriptions

### 3. Place your media files inside the videos folder

Copy or move your video/audio files (e.g. `.mp4`, `.m4a`, `.mp3`) into videos/.

### 4. Build the Docker image

docker build -t whisper-transcriber .

### 5. Run the container to process the videos

Windows (PowerShell):

docker run --rm -v "${PWD}\videos:/app/videos" -v "${PWD}\transcriptions:/app/transcriptions" whisper-transcriber

macOS/Linux (Bash):

docker run --rm -v "$(pwd)/videos:/app/videos" -v "$(pwd)/transcriptions:/app/transcriptions" whisper-transcriber

- The container will look for media in /app/videos (mounted from your local videos folder).
- It will convert/extract audio and generate transcriptions under /app/transcriptions.
- Transcription .txt files will have the same base name as the original files.

### Customization

- Change Whisper model: Edit entrypoint.sh and modify the --model base parameter to tiny, small, medium, or large depending on your hardware and accuracy needs.
- Language is set to Spanish (`--language es`) by default. Change or remove that flag in entrypoint.sh if you need another language or auto-detection.

### Important Notes

- The entire process runs locally; no data leaves your machine.
- Transcription accuracy depends on audio quality and the model used.
- You can process multiple files at once by placing them all in the videos folder.
- Ensure you have enough disk space for temporary files and output.

### Example Output

For a file named meeting.m4a (or meeting.mp4), after running the container you will get:

#### transcriptions/meeting.txt

Containing the plain text transcription.

### Support and Contributions

If you encounter any issues or want to suggest improvements, feel free to open an issue or pull request in the repository.

### License

This project is free for personal and corporate use. The Whisper model is open source under the MIT license.

Enjoy secure, private, and local transcription!
