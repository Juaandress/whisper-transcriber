using Whisper.net.Ggml;

namespace WhisperDesktop.Models;

public sealed class ModelOption
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required GgmlType GgmlType { get; init; }
    public required string FileName { get; init; }
    public required string SizeLabel { get; init; }
    public required string SpeedLabel { get; init; }
    public required string QualityLabel { get; init; }
    /// <summary>1 (más lento) … 5 (más rápido)</summary>
    public required int SpeedScore { get; init; }
    /// <summary>1 (menor) … 5 (mayor)</summary>
    public required int QualityScore { get; init; }
    public required string Description { get; init; }
    public bool IsRecommended { get; init; }

    public string SpeedDots => ScoreToDots(SpeedScore);
    public string QualityDots => ScoreToDots(QualityScore);
    public string ComboLabel => IsRecommended
        ? $"{DisplayName} — {SizeLabel} (recomendado)"
        : $"{DisplayName} — {SizeLabel}";

    public override string ToString() => ComboLabel;

    private static string ScoreToDots(int score)
    {
        score = Math.Clamp(score, 1, 5);
        return new string('●', score) + new string('○', 5 - score);
    }
}
