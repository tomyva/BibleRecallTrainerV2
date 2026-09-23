using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IStudyPlanService
{
    Task<StudyPlanSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    void SetDailyChapterGoal(int value);
}
