namespace BibleRecallTrainerV2.Models;

public enum RecallWordKind
{
    Match,
    Missing,
    Extra,
    Different
}

public sealed record RecallWordDifference(RecallWordKind Kind, string Expected, string Heard)
{
    public string Description => Kind switch
    {
        RecallWordKind.Match => $"Matched: {Expected}",
        RecallWordKind.Missing => $"Missing: {Expected}",
        RecallWordKind.Extra => $"Extra: {Heard}",
        RecallWordKind.Different => $"Expected {Expected}; heard {Heard}",
        _ => string.Empty
    };
}

public sealed class RecallComparisonResult
{
    public required string CanonicalText { get; init; }
    public required string Transcript { get; init; }
    public required IReadOnlyList<RecallWordDifference> Words { get; init; }
    public required string Feedback { get; init; }
    public int MatchPercentage { get; init; }
    public bool IsExactMatch { get; init; }
}
