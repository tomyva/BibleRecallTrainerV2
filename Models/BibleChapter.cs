namespace BibleRecallTrainerV2.Models;

public sealed class BibleChapter
{
    public string BookId { get; init; } = string.Empty;
    public int ChapterNumber { get; init; }
    public IReadOnlyList<BibleVerse> Verses { get; init; } = [];
}
