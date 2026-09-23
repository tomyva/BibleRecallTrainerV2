namespace BibleRecallTrainerV2.Models;

public sealed class LearningProgressData
{
    public int SchemaVersion { get; init; } = 1;
    public List<ChapterProgress> Chapters { get; init; } = [];
    public List<PracticeHistoryEntry> History { get; init; } = [];
}

public sealed class ChapterProgress
{
    public string BookId { get; init; } = string.Empty;
    public int ChapterNumber { get; init; }
    public int TimesRead { get; set; }
    public int RecallAttempts { get; set; }
    public int QuizAttempts { get; set; }
    public int BestQuizPercentage { get; set; }
    public DateTimeOffset? LastStudiedUtc { get; set; }
}

public sealed class PracticeHistoryEntry
{
    public string Activity { get; init; } = string.Empty;
    public string BookId { get; init; } = string.Empty;
    public int ChapterNumber { get; init; }
    public int? VerseNumber { get; init; }
    public int? Percentage { get; init; }
    public DateTimeOffset CompletedUtc { get; init; }
}

public sealed class ReviewScheduleData
{
    public int SchemaVersion { get; init; } = 1;
    public List<ChapterReviewSchedule> Reviews { get; init; } = [];
}

public sealed class ChapterReviewSchedule
{
    public string BookId { get; init; } = string.Empty;
    public int ChapterNumber { get; init; }
    public int IntervalDays { get; set; }
    public int SuccessfulReviews { get; set; }
    public int LastPercentage { get; set; }
    public DateTimeOffset LastReviewedUtc { get; set; }
    public DateTimeOffset NextReviewUtc { get; set; }
}
