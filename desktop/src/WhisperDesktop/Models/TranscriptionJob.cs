using CommunityToolkit.Mvvm.ComponentModel;

namespace WhisperDesktop.Models;

public partial class TranscriptionJob : ObservableObject
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public required string SourcePath { get; init; }

    public string FileName => System.IO.Path.GetFileName(SourcePath);

    [ObservableProperty]
    private JobStatus _status = JobStatus.Queued;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = "En cola";

    [ObservableProperty]
    private string? _outputPath;

    [ObservableProperty]
    private string? _errorMessage;

    public string StatusDisplay => Status switch
    {
        JobStatus.Queued => "En cola",
        JobStatus.Converting => "Convirtiendo audio",
        JobStatus.DownloadingModel => "Descargando modelo",
        JobStatus.Transcribing => "Transcribiendo",
        JobStatus.Completed => "Listo",
        JobStatus.Error => "Error",
        JobStatus.Cancelled => "Cancelado",
        _ => Status.ToString()
    };

    partial void OnStatusChanged(JobStatus value) => OnPropertyChanged(nameof(StatusDisplay));
}
