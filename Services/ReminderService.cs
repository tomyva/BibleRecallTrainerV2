using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class ReminderService(
    IReminderSettingsStore settingsStore,
    ILocalReminderScheduler scheduler) : IReminderService
{
    public bool IsSupported => scheduler.IsSupported;

    public ReminderSettings GetSettings() => settingsStore.Get();

    public async Task<string> SaveAsync(ReminderSettings settings, CancellationToken cancellationToken = default)
    {
        if (!settings.IsEnabled)
        {
            settingsStore.Set(settings);
            await scheduler.CancelAsync(cancellationToken);
            return "Daily reminder turned off.";
        }

        if (!scheduler.IsSupported)
        {
            settingsStore.Set(settings with { IsEnabled = false });
            await scheduler.CancelAsync(cancellationToken);
            return "Daily reminders are not supported on this device.";
        }

        var times = Normalize(settings.TimesOfDay);
        var scheduled = await scheduler.ScheduleAsync(times, true, cancellationToken);
        if (!scheduled)
            return "Notification permission is needed to turn on reminders.";

        settingsStore.Set(new ReminderSettings(true, times));
        return times.Count == 1
            ? $"Daily reminder set for {DateTime.Today.Add(times[0]):t}."
            : $"{times.Count} daily reminders set: {string.Join(", ", times.Select(time => DateTime.Today.Add(time).ToString("t")))}.";
    }

    public async Task EnsureScheduledAsync(CancellationToken cancellationToken = default)
    {
        var settings = settingsStore.Get();
        if (settings.IsEnabled && scheduler.IsSupported)
        {
            var scheduled = await scheduler.ScheduleAsync(Normalize(settings.TimesOfDay), false, cancellationToken);
            if (!scheduled)
                settingsStore.Set(settings with { IsEnabled = false });
        }
    }

    private static IReadOnlyList<TimeSpan> Normalize(IEnumerable<TimeSpan> times) => times
        .Select(time => TimeSpan.FromMinutes(Math.Clamp((int)time.TotalMinutes, 0, (24 * 60) - 1)))
        .Distinct()
        .Order()
        .Take(8)
        .DefaultIfEmpty(ReminderSettings.Default.TimesOfDay[0])
        .ToList();
}
