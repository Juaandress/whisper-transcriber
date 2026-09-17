using System.Windows;
using WhisperDesktop.Models;

namespace WhisperDesktop.Services;

public sealed class TranscriptionJobRunner
{
    private readonly FfmpegAudioExtractor _ffmpeg = new();
    private readonly WhisperTranscriber _whisper = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool FfmpegAvailable => _ffmpeg.IsAvailable;

    public string FfmpegPath => _ffmpeg.FfmpegPath;

    private static void OnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }

    public async Task RunAsync(
        TranscriptionJob job,
        string outputDirectory,
        ModelOption model,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        string? wavPath = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!System.IO.File.Exists(job.SourcePath))
                throw new System.IO.FileNotFoundException("No se encontró el archivo de origen.", job.SourcePath);

            if (!FfmpegAudioExtractor.IsSupported(job.SourcePath))
                throw new NotSupportedException("Formato no soportado.");

            System.IO.Directory.CreateDirectory(outputDirectory);
            AppPaths.EnsureDirectories();

            wavPath = System.IO.Path.Combine(AppPaths.TempDirectory, $"{job.Id}.wav");
            var duration = await _ffmpeg.GetDurationAsync(job.SourcePath, cancellationToken);

            OnUi(() =>
            {
                job.Status = JobStatus.Converting;
                job.Progress = 5;
                job.StatusMessage = $"Convirtiendo {job.FileName}…";
            });

            var convertProgress = new Progress<string>(_ =>
            {
                OnUi(() =>
                {
                    if (job.Status == JobStatus.Converting)
                        job.StatusMessage = "Convirtiendo audio…";
                });
            });

            await _ffmpeg.ExtractWavAsync(job.SourcePath, wavPath, convertProgress, cancellationToken);

            OnUi(() =>
            {
                job.Status = JobStatus.DownloadingModel;
                job.Progress = 15;
                job.StatusMessage = "Preparando modelo de Whisper…";
            });

            var modelProgress = new Progress<(double percent, string message)>(p =>
            {
                OnUi(() =>
                {
                    job.Status = JobStatus.DownloadingModel;
                    job.Progress = 15 + (p.percent * 0.15);
                    job.StatusMessage = p.message;
                });
            });

            await _whisper.EnsureModelAsync(model.GgmlType, model.FileName, modelProgress, cancellationToken);

            OnUi(() =>
            {
                job.Status = JobStatus.Transcribing;
                job.Progress = 30;
                job.StatusMessage = "Transcribiendo…";
            });

            var transcribeProgress = new Progress<(double percent, string message)>(p =>
            {
                OnUi(() =>
                {
                    job.Status = JobStatus.Transcribing;
                    job.Progress = 30 + (p.percent * 0.7);
                    job.StatusMessage = p.message;
                });
            });

            var text = await _whisper.TranscribeAsync(
                wavPath,
                languageCode,
                duration,
                transcribeProgress,
                cancellationToken);

            var outputPath = System.IO.Path.Combine(
                outputDirectory,
                System.IO.Path.GetFileNameWithoutExtension(job.FileName) + ".txt");

            await System.IO.File.WriteAllTextAsync(outputPath, text + Environment.NewLine, cancellationToken);

            OnUi(() =>
            {
                job.OutputPath = outputPath;
                job.Status = JobStatus.Completed;
                job.Progress = 100;
                job.StatusMessage = "Listo";
                job.ErrorMessage = null;
            });
        }
        catch (OperationCanceledException)
        {
            OnUi(() =>
            {
                job.Status = JobStatus.Cancelled;
                job.StatusMessage = "Cancelado";
            });
            throw;
        }
        catch (Exception ex)
        {
            OnUi(() =>
            {
                job.Status = JobStatus.Error;
                job.Progress = 0;
                job.ErrorMessage = ex.Message;
                job.StatusMessage = "Error: " + ex.Message;
            });
        }
        finally
        {
            if (wavPath is not null)
            {
                try
                {
                    if (System.IO.File.Exists(wavPath))
                        System.IO.File.Delete(wavPath);
                }
                catch
                {
                    // ignore temp cleanup
                }
            }

            _gate.Release();
        }
    }

    public ValueTask DisposeAsync() => _whisper.DisposeAsync();
}
