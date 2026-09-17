using System.Diagnostics;
using System.Text;

namespace WhisperDesktop.Services;

public sealed class FfmpegAudioExtractor
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public FfmpegAudioExtractor()
    {
        var (ffmpeg, ffprobe) = ResolveBinaries();
        _ffmpegPath = ffmpeg;
        _ffprobePath = ffprobe;
    }

    public bool IsAvailable => System.IO.File.Exists(_ffmpegPath);

    public string FfmpegPath => _ffmpegPath;

    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".mov", ".avi", ".webm",
        ".m4a", ".mp3", ".wav", ".flac", ".ogg", ".aac"
    };

    public static bool IsSupported(string path) =>
        SupportedExtensions.Contains(System.IO.Path.GetExtension(path));

    public async Task<TimeSpan?> GetDurationAsync(string mediaPath, CancellationToken cancellationToken = default)
    {
        if (!System.IO.File.Exists(_ffprobePath))
            return null;

        var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{mediaPath}\"";
        var output = await RunProcessCaptureAsync(_ffprobePath, args, cancellationToken);
        if (double.TryParse(output.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }

    public async Task ExtractWavAsync(
        string inputPath,
        string outputWavPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "No se encontró ffmpeg.exe. Ejecutá tools/publish.ps1 o colocá ffmpeg en la carpeta ffmpeg de la app.");
        }

        progress?.Report($"Convirtiendo {System.IO.Path.GetFileName(inputPath)} a audio WAV…");

        var directory = System.IO.Path.GetDirectoryName(outputWavPath);
        if (!string.IsNullOrEmpty(directory))
            System.IO.Directory.CreateDirectory(directory);

        if (System.IO.File.Exists(outputWavPath))
            System.IO.File.Delete(outputWavPath);

        var args = $"-y -i \"{inputPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{outputWavPath}\"";
        var exitCode = await RunProcessAsync(_ffmpegPath, args, progress, cancellationToken);

        if (exitCode != 0 || !System.IO.File.Exists(outputWavPath))
        {
            throw new InvalidOperationException(
                "No se pudo convertir el archivo. Verificá que no esté corrupto y que el formato sea compatible.");
        }
    }

    private static (string ffmpeg, string ffprobe) ResolveBinaries()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            System.IO.Path.Combine(baseDir, "ffmpeg", "ffmpeg.exe"),
            System.IO.Path.Combine(baseDir, "ffmpeg.exe"),
            System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "tools", "ffmpeg", "ffmpeg.exe"),
        };

        foreach (var candidate in candidates)
        {
            var full = System.IO.Path.GetFullPath(candidate);
            if (System.IO.File.Exists(full))
            {
                var dir = System.IO.Path.GetDirectoryName(full)!;
                var probe = System.IO.Path.Combine(dir, "ffprobe.exe");
                return (full, probe);
            }
        }

        var fromPath = FindOnPath("ffmpeg.exe");
        var probeFromPath = FindOnPath("ffprobe.exe") ?? string.Empty;
        return (fromPath ?? System.IO.Path.Combine(baseDir, "ffmpeg", "ffmpeg.exe"), probeFromPath);
    }

    private static string? FindOnPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = System.IO.Path.Combine(dir.Trim('"'), fileName);
                if (System.IO.File.Exists(candidate))
                    return candidate;
            }
            catch
            {
                // ignore invalid PATH entries
            }
        }

        return null;
    }

    private static async Task<int> RunProcessAsync(
        string fileName,
        string arguments,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                progress?.Report(e.Data);
        };
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                progress?.Report(e.Data);
        };
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);

        if (!process.Start())
            throw new InvalidOperationException("No se pudo iniciar FFmpeg.");

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        await using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }
        });

        return await tcs.Task.WaitAsync(cancellationToken);
    }

    private static async Task<string> RunProcessCaptureAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("No se pudo iniciar ffprobe.");

        var stdout = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stdout.AppendLine(e.Data);
        };
        process.BeginOutputReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return stdout.ToString();
    }
}
