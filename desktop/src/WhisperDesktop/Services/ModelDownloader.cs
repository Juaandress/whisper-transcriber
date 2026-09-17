using System.Net.Http;
using Whisper.net.Ggml;

namespace WhisperDesktop.Services;

public sealed class ModelDownloader
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromHours(2)
    };

    private static readonly Dictionary<GgmlType, string> ModelUrls = new()
    {
        [GgmlType.Tiny] = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin",
        [GgmlType.Base] = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin",
        [GgmlType.Small] = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin",
        [GgmlType.Medium] = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin",
        [GgmlType.LargeV2] = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v2.bin",
        [GgmlType.LargeV3] = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin",
        [GgmlType.LargeV3Turbo] = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo.bin",
    };

    public string GetModelPath(GgmlType type, string fileName) =>
        System.IO.Path.Combine(AppPaths.ModelsDirectory, fileName);

    public bool IsModelPresent(GgmlType type, string fileName) =>
        System.IO.File.Exists(GetModelPath(type, fileName));

    public async Task EnsureModelAsync(
        GgmlType type,
        string fileName,
        IProgress<(double percent, string message)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureDirectories();
        var path = GetModelPath(type, fileName);

        // Umbral mínimo por modelo para no dar por válido un download a medias
        var minBytes = type switch
        {
            GgmlType.Tiny => 20_000_000L,
            GgmlType.Base => 100_000_000L,
            GgmlType.Small => 200_000_000L,
            GgmlType.Medium => 500_000_000L,
            GgmlType.LargeV2 or GgmlType.LargeV3 => 1_000_000_000L,
            GgmlType.LargeV3Turbo => 500_000_000L,
            _ => 1_000_000L
        };
        if (System.IO.File.Exists(path) && new System.IO.FileInfo(path).Length > minBytes)
            return;

        if (!ModelUrls.TryGetValue(type, out var url))
            throw new NotSupportedException($"El modelo {type} no está disponible en esta versión.");

        progress?.Report((0, $"Descargando modelo {fileName} (solo la primera vez)…"));

        var tempPath = path + ".download";
        try
        {
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1L;
            await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var local = new System.IO.FileStream(tempPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 81920, true);

            var buffer = new byte[81920];
            long readTotal = 0;
            int read;
            var lastReported = -1;

            while ((read = await remote.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await local.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                readTotal += read;

                if (total > 0)
                {
                    var percent = (int)(readTotal * 100 / total);
                    if (percent != lastReported)
                    {
                        lastReported = percent;
                        var mb = readTotal / (1024.0 * 1024.0);
                        var totalMb = total / (1024.0 * 1024.0);
                        progress?.Report((percent, $"Descargando modelo… {percent}% ({mb:0.0}/{totalMb:0.0} MB)"));
                    }
                }
                else
                {
                    var mb = readTotal / (1024.0 * 1024.0);
                    progress?.Report((0, $"Descargando modelo… {mb:0.0} MB"));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            progress?.Report((0, "Reintentando descarga del modelo…"));
            await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(type, QuantizationType.NoQuantization, cancellationToken);
            await using var fileWriter = System.IO.File.Create(tempPath);
            await modelStream.CopyToAsync(fileWriter, cancellationToken);
        }

        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
        System.IO.File.Move(tempPath, path);
        progress?.Report((100, "Modelo listo."));
    }
}
