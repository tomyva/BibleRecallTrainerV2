namespace BibleRecallTrainerV2.Models;

public sealed class DataHealthReport
{
    public IReadOnlyList<string> Issues { get; init; } = [];
    public bool IsHealthy => Issues.Count == 0;
    public string Summary => IsHealthy
        ? "Your local study data passed all checks."
        : Issues.Count == 1 ? "1 data issue was found." : $"{Issues.Count} data issues were found.";
}

public sealed class LearningDataExport
{
    public int FormatVersion { get; init; } = 1;
    public DateTimeOffset ExportedUtc { get; init; }
    public LearningProgressData Progress { get; init; } = new();
    public IReadOnlyList<ChapterReviewSchedule> ReviewSchedule { get; init; } = [];
    public int DailyChapterGoal { get; init; }
    public ReminderSettings Reminder { get; init; } = ReminderSettings.Default;
    public IReadOnlyList<ReadingPosition> ReadingPositions { get; init; } = [];
}
