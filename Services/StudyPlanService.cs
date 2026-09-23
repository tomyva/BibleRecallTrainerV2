using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class StudyPlanService(
    IBibleContentService bibleContent,
    IProgressService progressService,
    ISpacedRepetitionService spacedRepetitionService,
    IStudyPlanSettingsStore settingsStore,
    TimeProvider timeProvider) : IStudyPlanService
{
    public async Task<StudyPlanSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var books = await bibleContent.GetBooksAsync(cancellationToken);
        var progress = await progressService.GetSnapshotAsync(cancellationToken);
        var due = await spacedRepetitionService.GetDueReviewsAsync(cancellationToken);
        var orderedChapters = books.SelectMany(book => book.Chapters.Select(chapter =>
            new StudyChapterItem(book.Id, book.DisplayName, chapter.ChapterNumber))).ToList();
        var validKeys = orderedChapters.Select(chapter => Key(chapter.BookId, chapter.ChapterNumber))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var readKeys = progress.Chapters.Where(item => item.TimesRead > 0)
            .Select(item => Key(item.BookId, item.ChapterNumber))
            .Where(validKeys.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var today = timeProvider.GetLocalNow().Date;
        var studiedToday = progress.History
            .Where(entry => entry.Activity == "Read" && entry.CompletedUtc.LocalDateTime.Date == today)
            .Select(entry => Key(entry.BookId, entry.ChapterNumber))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var dueItems = due.Select(review =>
            {
                var book = books.FirstOrDefault(item => string.Equals(item.Id, review.BookId, StringComparison.OrdinalIgnoreCase));
                return book is null || !validKeys.Contains(Key(review.BookId, review.ChapterNumber))
                    ? null
                    : new StudyChapterItem(review.BookId, book.DisplayName, review.ChapterNumber, review.NextReviewUtc);
            })
            .OfType<StudyChapterItem>()
            .ToList();

        return new StudyPlanSnapshot
        {
            DailyChapterGoal = settingsStore.GetDailyChapterGoal(),
            ChaptersStudiedToday = studiedToday,
            CompletedChapters = readKeys.Count,
            TotalChapters = orderedChapters.Count,
            DueReviews = dueItems,
            NextChapter = orderedChapters.FirstOrDefault(chapter => !readKeys.Contains(Key(chapter.BookId, chapter.ChapterNumber)))
                          ?? dueItems.FirstOrDefault()
                          ?? orderedChapters.FirstOrDefault()
        };
    }

    public void SetDailyChapterGoal(int value) => settingsStore.SetDailyChapterGoal(value);

    private static string Key(string bookId, int chapterNumber) => $"{bookId}:{chapterNumber}";
}
