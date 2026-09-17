using Whisper.net.Ggml;

namespace WhisperDesktop.Models;

public sealed record ModelOption(string Id, string DisplayName, GgmlType GgmlType, string FileName)
{
    public override string ToString() => DisplayName;
}
