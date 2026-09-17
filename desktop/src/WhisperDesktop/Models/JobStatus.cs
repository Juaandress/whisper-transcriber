namespace WhisperDesktop.Models;

public enum JobStatus
{
    Queued,
    Converting,
    DownloadingModel,
    Transcribing,
    Completed,
    Error,
    Cancelled
}
