using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IProgressService
{
    Task<LearningProgressData> GetSnapshotAsync(CancellationToken cancellationToken = default);
    Task RecordChapterReadAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default);
    Task RecordRecallAttemptAsync(string bookId, int chapterNumber, int verseNumber, int percentage, CancellationToken cancellationToken = default);
    Task RecordQuizCompletedAsync(string bookId, int chapterNumber, int percentage, CancellationToken cancellationToken = default);
}
