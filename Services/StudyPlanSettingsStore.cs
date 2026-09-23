using Microsoft.Maui.Storage;

namespace BibleRecallTrainerV2.Services;

public sealed class StudyPlanSettingsStore(IPreferences preferences) : IStudyPlanSettingsStore
{
    private const string GoalKey = "study.dailyChapterGoal";

    public int GetDailyChapterGoal() => Math.Clamp(preferences.Get(GoalKey, 1), 1, 5);
    public void SetDailyChapterGoal(int value) => preferences.Set(GoalKey, Math.Clamp(value, 1, 5));
}
