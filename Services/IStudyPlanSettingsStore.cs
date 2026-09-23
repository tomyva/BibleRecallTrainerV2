namespace BibleRecallTrainerV2.Services;

public interface IStudyPlanSettingsStore
{
    int GetDailyChapterGoal();
    void SetDailyChapterGoal(int value);
}
