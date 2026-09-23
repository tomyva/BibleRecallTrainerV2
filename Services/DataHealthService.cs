using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class DataHealthService(
    IBibleContentService bibleContent,
    IProgressService progressService,
    ISpacedRepetitionService spacedRepetitionService) : IDataHealthService
{
    public async Task<DataHealthReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var books = await bibleContent.GetBooksAsync(cancellationToken);
            var progress = await progressService.GetSnapshotAsync(cancellationToken);
            var reviews = await spacedRepetitionService.GetScheduleAsync(cancellationToken);
            var valid = books.SelectMany(book => book.Chapters.Select(chapter => Key(book.Id, chapter.ChapterNumber)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var issues = new List<string>();

            AddDuplicateIssues(progress.Chapters.Select(item => Key(item.BookId, item.ChapterNumber)), "progress", issues);
            AddDuplicateIssues(reviews.Select(item => Key(item.BookId, item.ChapterNumber)), "review schedule", issues);

            foreach (var item in progress.Chapters)
            {
                if (!valid.Contains(Key(item.BookId, item.ChapterNumber)))
                    issues.Add($"Progress references an unknown chapter: {item.BookId} {item.ChapterNumber}.");
                if (item.TimesRead < 0 || item.RecallAttempts < 0 || item.QuizAttempts < 0 || item.BestQuizPercentage is < 0 or > 100)
                    issues.Add($"Progress has invalid values for {item.BookId} {item.ChapterNumber}.");
            }

            foreach (var item in reviews)
            {
                if (!valid.Contains(Key(item.BookId, item.ChapterNumber)))
                    issues.Add($"The review schedule references an unknown chapter: {item.BookId} {item.ChapterNumber}.");
                if (item.IntervalDays < 1 || item.LastPercentage is < 0 or > 100 || item.NextReviewUtc < item.LastReviewedUtc)
                    issues.Add($"The review schedule has invalid values for {item.BookId} {item.ChapterNumber}.");
            }

            return new DataHealthReport { Issues = issues };
        }
        catch (Exception exception)
        {
            return new DataHealthReport { Issues = [$"Stored data could not be read: {exception.Message}"] };
        }
    }

    private static void AddDuplicateIssues(IEnumerable<string> keys, string area, ICollection<string> issues)
    {
        foreach (var duplicate in keys.GroupBy(key => key, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            issues.Add($"The {area} contains duplicate entries for {duplicate.Key}.");
    }

    private static string Key(string bookId, int chapterNumber) => $"{bookId}:{chapterNumber}";
}
