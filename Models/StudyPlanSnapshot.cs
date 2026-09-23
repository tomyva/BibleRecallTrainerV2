namespace BibleRecallTrainerV2.Models;

public sealed class StudyPlanSnapshot
{
    public int DailyChapterGoal { get; init; }
    public int ChaptersStudiedToday { get; init; }
    public int CompletedChapters { get; init; }
    public int TotalChapters { get; init; }
    public IReadOnlyList<StudyChapterItem> DueReviews { get; init; } = [];
    public StudyChapterItem? NextChapter { get; init; }

    public string DailyProgress => $"{Math.Min(ChaptersStudiedToday, DailyChapterGoal)} of {DailyChapterGoal} chapters today";
    public double DailyProgressValue => DailyChapterGoal == 0 ? 0 : Math.Min(1d, ChaptersStudiedToday / (double)DailyChapterGoal);
    public string SyllabusProgress => $"{CompletedChapters} of {TotalChapters} chapters read";
    public string DueSummary => DueReviews.Count == 1 ? "1 review due" : $"{DueReviews.Count} reviews due";
}

public sealed record StudyChapterItem(string BookId, string BookName, int ChapterNumber, DateTimeOffset? DueUtc = null)
{
    public string DisplayName => $"{BookName} {ChapterNumber}";
    public string DueText => DueUtc is null ? string.Empty : $"Due {DueUtc.Value.LocalDateTime:g}";
}
