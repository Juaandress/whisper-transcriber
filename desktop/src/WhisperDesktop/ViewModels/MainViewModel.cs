using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WhisperDesktop.Models;
using WhisperDesktop.Services;
using Whisper.net.Ggml;

namespace WhisperDesktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly TranscriptionJobRunner _runner = new();
    private CancellationTokenSource? _cts;

    public ObservableCollection<TranscriptionJob> Jobs { get; } = new();

    public ObservableCollection<LanguageOption> Languages { get; } =
    [
        new("es", "Español"),
        new("auto", "Detectar automáticamente"),
        new("en", "Inglés"),
        new("pt", "Portugués"),
        new("fr", "Francés"),
        new("it", "Italiano"),
    ];

    public ObservableCollection<ModelOption> Models { get; } =
    [
        new()
        {
            Id = "tiny",
            DisplayName = "Tiny",
            GgmlType = GgmlType.Tiny,
            FileName = "ggml-tiny.bin",
            SizeLabel = "~75 MB",
            SpeedLabel = "Muy rápida",
            QualityLabel = "Básica",
            SpeedScore = 5,
            QualityScore = 1,
            Description = "Ideal para pruebas rápidas. Puede fallar con audio difícil."
        },
        new()
        {
            Id = "base",
            DisplayName = "Base",
            GgmlType = GgmlType.Base,
            FileName = "ggml-base.bin",
            SizeLabel = "~140 MB",
            SpeedLabel = "Rápida",
            QualityLabel = "Buena",
            SpeedScore = 4,
            QualityScore = 2,
            Description = "Buen equilibrio para el día a día.",
            IsRecommended = true
        },
        new()
        {
            Id = "small",
            DisplayName = "Small",
            GgmlType = GgmlType.Small,
            FileName = "ggml-small.bin",
            SizeLabel = "~460 MB",
            SpeedLabel = "Moderada",
            QualityLabel = "Muy buena",
            SpeedScore = 3,
            QualityScore = 3,
            Description = "Mejor con ruido, acentos o nombres propios."
        },
        new()
        {
            Id = "medium",
            DisplayName = "Medium",
            GgmlType = GgmlType.Medium,
            FileName = "ggml-medium.bin",
            SizeLabel = "~1,5 GB",
            SpeedLabel = "Lenta",
            QualityLabel = "Alta",
            SpeedScore = 2,
            QualityScore = 4,
            Description = "Más precisa. Pedí paciencia en CPU y varios GB libres."
        },
        new()
        {
            Id = "large-v2",
            DisplayName = "Large v2",
            GgmlType = GgmlType.LargeV2,
            FileName = "ggml-large-v2.bin",
            SizeLabel = "~3 GB",
            SpeedLabel = "Muy lenta",
            QualityLabel = "Muy alta",
            SpeedScore = 1,
            QualityScore = 5,
            Description = "Máxima calidad clásica. Descarga y proceso pesados."
        },
        new()
        {
            Id = "large-v3",
            DisplayName = "Large v3",
            GgmlType = GgmlType.LargeV3,
            FileName = "ggml-large-v3.bin",
            SizeLabel = "~3 GB",
            SpeedLabel = "Muy lenta",
            QualityLabel = "Máxima",
            SpeedScore = 1,
            QualityScore = 5,
            Description = "La mejor calidad general de Whisper. Exige máquina potente o mucha paciencia."
        },
        new()
        {
            Id = "large-v3-turbo",
            DisplayName = "Large v3 Turbo",
            GgmlType = GgmlType.LargeV3Turbo,
            FileName = "ggml-large-v3-turbo.bin",
            SizeLabel = "~1,6 GB",
            SpeedLabel = "Media-alta",
            QualityLabel = "Muy alta",
            SpeedScore = 3,
            QualityScore = 4,
            Description = "Calidad cercana a Large, más rápida que Large v2/v3 completo."
        },
    ];

    [ObservableProperty]
    private LanguageOption _selectedLanguage = null!;

    [ObservableProperty]
    private ModelOption _selectedModel = null!;

    [ObservableProperty]
    private string _outputDirectory = AppPaths.DefaultOutputDirectory;

    [ObservableProperty]
    private string _statusLog = "Listo. Arrastrá archivos de audio o video, o usá «Elegir archivos».";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _ffmpegStatus = string.Empty;

    public MainViewModel()
    {
        SelectedLanguage = Languages[0];
        SelectedModel = Models[1];
        AppPaths.EnsureDirectories();
        RefreshFfmpegStatus();
    }

    [RelayCommand]
    private void OpenTerms() => OpenUrl("https://github.com/Juaandress/whisper-transcriber/blob/main/docs/TERMINOS.md");

    [RelayCommand]
    private void OpenPrivacy() => OpenUrl("https://github.com/Juaandress/whisper-transcriber/blob/main/docs/PRIVACIDAD.md");

    [RelayCommand]
    private void OpenLicense() => OpenUrl("https://github.com/Juaandress/whisper-transcriber/blob/main/LICENSE");

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private void RefreshFfmpegStatus()
    {
        if (_runner.FfmpegAvailable)
            FfmpegStatus = $"FFmpeg listo ({_runner.FfmpegPath})";
        else
            FfmpegStatus = "FFmpeg no encontrado. Ejecutá tools\\publish.ps1 o instalá FFmpeg.";
    }

    public void AddFiles(IEnumerable<string> paths)
    {
        var added = 0;
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
                continue;

            if (!FfmpegAudioExtractor.IsSupported(path))
            {
                AppendLog($"Formato no soportado: {Path.GetFileName(path)}");
                continue;
            }

            if (Jobs.Any(j => string.Equals(j.SourcePath, path, StringComparison.OrdinalIgnoreCase)
                              && j.Status is JobStatus.Queued or JobStatus.Converting
                                  or JobStatus.DownloadingModel or JobStatus.Transcribing))
            {
                continue;
            }

            Jobs.Add(new TranscriptionJob { SourcePath = path });
            added++;
        }

        if (added > 0)
        {
            AppendLog($"Se agregaron {added} archivo(s) a la cola.");
            StartCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void ChooseFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Elegir audio o video",
            Multiselect = true,
            Filter =
                "Audio y video|*.mp4;*.mkv;*.mov;*.avi;*.webm;*.m4a;*.mp3;*.wav;*.flac;*.ogg;*.aac|" +
                "Todos los archivos|*.*"
        };

        if (dialog.ShowDialog() == true)
            AddFiles(dialog.FileNames);
    }

    [RelayCommand]
    private void ChooseOutputFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Carpeta de salida",
            InitialDirectory = Directory.Exists(OutputDirectory)
                ? OutputDirectory
                : AppPaths.DefaultOutputDirectory
        };

        if (dialog.ShowDialog() == true)
            OutputDirectory = dialog.FolderName;
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var toRemove = Jobs.Where(j => j.Status is JobStatus.Completed or JobStatus.Error or JobStatus.Cancelled).ToList();
        foreach (var job in toRemove)
            Jobs.Remove(job);
        StartCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveJob(TranscriptionJob? job)
    {
        if (job is null)
            return;
        if (job.Status is JobStatus.Converting or JobStatus.DownloadingModel or JobStatus.Transcribing)
            return;
        Jobs.Remove(job);
        StartCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void OpenOutputFolder()
    {
        Directory.CreateDirectory(OutputDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = OutputDirectory,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void OpenJobOutput(TranscriptionJob? job)
    {
        if (job?.OutputPath is null || !File.Exists(job.OutputPath))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = job.OutputPath,
            UseShellExecute = true
        });
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        RefreshFfmpegStatus();
        if (!_runner.FfmpegAvailable)
        {
            MessageBox.Show(
                "No se encontró FFmpeg.\n\nEjecutá desktop\\tools\\publish.ps1 para descargarlo, " +
                "o colocá ffmpeg.exe en la carpeta ffmpeg junto a la aplicación.",
                "Whisper Desktop",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var pending = Jobs.Where(j => j.Status is JobStatus.Queued or JobStatus.Error).ToList();
        if (pending.Count == 0)
        {
            AppendLog("No hay archivos pendientes en la cola.");
            return;
        }

        foreach (var job in pending.Where(j => j.Status == JobStatus.Error))
        {
            job.Status = JobStatus.Queued;
            job.Progress = 0;
            job.ErrorMessage = null;
            job.StatusMessage = "En cola";
        }

        IsBusy = true;
        _cts = new CancellationTokenSource();
        StartCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();

        try
        {
            Directory.CreateDirectory(OutputDirectory);
            AppendLog($"Iniciando transcripción de {pending.Count} archivo(s). Idioma: {SelectedLanguage.DisplayName}. Modelo: {SelectedModel.DisplayName}.");

            foreach (var job in pending)
            {
                if (_cts.IsCancellationRequested)
                    break;

                AppendLog($"Procesando: {job.FileName}");
                await _runner.RunAsync(job, OutputDirectory, SelectedModel, SelectedLanguage.Code, _cts.Token);

                if (job.Status == JobStatus.Completed)
                    AppendLog($"Listo: {job.OutputPath}");
                else if (job.Status == JobStatus.Error)
                    AppendLog($"Error en {job.FileName}: {job.ErrorMessage}");
            }

            AppendLog(_cts.IsCancellationRequested ? "Proceso cancelado." : "Cola finalizada.");
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
            StartCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanStart() => !IsBusy && Jobs.Count > 0;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cts?.Cancel();
        AppendLog("Cancelando…");
    }

    private bool CanCancel() => IsBusy;

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        StatusLog = string.IsNullOrWhiteSpace(StatusLog) ? line : StatusLog + Environment.NewLine + line;
    }

    partial void OnIsBusyChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }
}
