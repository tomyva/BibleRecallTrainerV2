using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IBibleContentService
{
    Task<IReadOnlyList<BibleBook>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BibleChapter>> GetChaptersAsync(string bookId, CancellationToken cancellationToken = default);
    Task<BibleChapter> GetChapterAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default);
    Task<BibleChapterReference?> GetPreviousChapterAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default);
    Task<BibleChapterReference?> GetNextChapterAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default);
}
