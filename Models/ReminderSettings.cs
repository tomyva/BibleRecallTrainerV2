namespace BibleRecallTrainerV2.Models;

public sealed record ReminderSettings(bool IsEnabled, IReadOnlyList<TimeSpan> TimesOfDay)
{
    public static ReminderSettings Default { get; } = new(false, [new TimeSpan(19, 0, 0)]);
}
