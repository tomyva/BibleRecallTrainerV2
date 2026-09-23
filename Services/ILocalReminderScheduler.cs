namespace BibleRecallTrainerV2.Services;

public interface ILocalReminderScheduler
{
    bool IsSupported { get; }
    Task<bool> ScheduleAsync(IReadOnlyList<TimeSpan> timesOfDay, bool requestPermission, CancellationToken cancellationToken = default);
    Task CancelAsync(CancellationToken cancellationToken = default);
}
