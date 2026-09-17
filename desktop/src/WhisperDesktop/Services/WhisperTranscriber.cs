using System.Text;
using Whisper.net;
using Whisper.net.Ggml;

namespace WhisperDesktop.Services;

public sealed class WhisperTranscriber : IAsyncDisposable
{
    private readonly ModelDownloader _modelDownloader = new();
    private WhisperFactory? _factory;
    private GgmlType? _loadedType;
    private string? _loadedPath;

    public async Task EnsureModelAsync(
        GgmlType type,
        string fileName,
        IProgress<(double percent, string message)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await _modelDownloader.EnsureModelAsync(type, fileName, progress, cancellationToken);
        var path = _modelDownloader.GetModelPath(type, fileName);

        if (_factory is null || _loadedType != type || _loadedPath != path)
        {
            _factory?.Dispose();
            _factory = WhisperFactory.FromPath(path);
            _loadedType = type;
            _loadedPath = path;
        }
    }

    public async Task<string> TranscribeAsync(
        string wavPath,
        string languageCode,
        TimeSpan? mediaDuration = null,
        IProgress<(double percent, string message)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_factory is null)
            throw new InvalidOperationException("El modelo de Whisper no está cargado.");

        var builder = _factory.CreateBuilder();
        builder = languageCode is "auto" or ""
            ? builder.WithLanguage("auto")
            : builder.WithLanguage(languageCode);

        using var processor = builder.Build();
        await using var fileStream = System.IO.File.OpenRead(wavPath);

        var sb = new StringBuilder();
        var lastPercent = -1;

        await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
        {
            var text = segment.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                sb.Append(text);
            }

            if (mediaDuration is { TotalSeconds: > 0 })
            {
                var percent = Math.Clamp(segment.End.TotalSeconds / mediaDuration.Value.TotalSeconds * 100.0, 0, 99);
                var rounded = (int)percent;
                if (rounded != lastPercent)
                {
                    lastPercent = rounded;
                    var end = segment.End;
                    progress?.Report((percent,
                        $"Transcribiendo… {rounded}% (aprox. {end.Minutes:D2}:{end.Seconds:D2})"));
                }
            }
            else
            {
                progress?.Report((0, $"Transcribiendo… {segment.End:mm\\:ss}"));
            }
        }

        progress?.Report((100, "Transcripción completada."));
        return sb.ToString().Trim();
    }

    public ValueTask DisposeAsync()
    {
        _factory?.Dispose();
        _factory = null;
        return ValueTask.CompletedTask;
    }
}
