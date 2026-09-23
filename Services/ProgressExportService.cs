using System.Text.Json;
using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class ProgressExportService(
    IProgressService progressService,
    ISpacedRepetitionService spacedRepetitionService,
    IStudyPlanSettingsStore studyPlanSettings,
    IReminderSettingsStore reminderSettings,
    IReadingPositionStore readingPositionStore,
    TimeProvider timeProvider) : IProgressExportService
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<string> CreateJsonAsync(CancellationToken cancellationToken = default)
    {
        var export = new LearningDataExport
        {
            ExportedUtc = timeProvider.GetUtcNow(),
            Progress = await progressService.GetSnapshotAsync(cancellationToken),
            ReviewSchedule = await spacedRepetitionService.GetScheduleAsync(cancellationToken),
            DailyChapterGoal = studyPlanSettings.GetDailyChapterGoal(),
            Reminder = reminderSettings.Get(),
            ReadingPositions = readingPositionStore.GetAll()
        };
        return JsonSerializer.Serialize(export, Options);
    }
}
