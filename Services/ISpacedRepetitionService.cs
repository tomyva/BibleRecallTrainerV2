using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface ISpacedRepetitionService
{
    Task RecordResultAsync(string bookId, int chapterNumber, int percentage, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChapterReviewSchedule>> GetDueReviewsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChapterReviewSchedule>> GetScheduleAsync(CancellationToken cancellationToken = default);
}
