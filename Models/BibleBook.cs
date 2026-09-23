namespace BibleRecallTrainerV2.Models;

public sealed class BibleBook
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Testament { get; init; } = string.Empty;
    public IReadOnlyList<BibleChapter> Chapters { get; init; } = [];

    public string ChapterRange => Chapters.Count == 1 ? "Chapter 1" : $"Chapters 1–{Chapters.Count}";
}
