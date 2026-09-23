using BibleRecallTrainerV2.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace BibleRecallTrainerV2.Services;

public sealed class ReminderSettingsStore(IPreferences preferences) : IReminderSettingsStore
{
    private const string EnabledKey = "reminder.enabled";
    private const string MinutesKey = "reminder.minutesAfterMidnight";
    private const string TimesKey = "reminder.timesAfterMidnight.v2";

    public ReminderSettings Get()
    {
        var minutes = ReadMinutes();
        return new ReminderSettings(
            preferences.Get(EnabledKey, false),
            minutes.Select(minutesAfterMidnight => TimeSpan.FromMinutes(minutesAfterMidnight)).ToList());
    }

    public void Set(ReminderSettings settings)
    {
        var minutes = settings.TimesOfDay
            .Select(time => Math.Clamp((int)time.TotalMinutes, 0, (24 * 60) - 1))
            .Distinct()
            .Order()
            .ToList();
        if (minutes.Count == 0)
            minutes.Add(19 * 60);

        preferences.Set(EnabledKey, settings.IsEnabled);
        preferences.Set(TimesKey, JsonSerializer.Serialize(minutes));
        preferences.Remove(MinutesKey);
    }

    private List<int> ReadMinutes()
    {
        var json = preferences.Get(TimesKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var saved = JsonSerializer.Deserialize<List<int>>(json);
                if (saved is { Count: > 0 })
                    return saved.Select(value => Math.Clamp(value, 0, (24 * 60) - 1)).Distinct().Order().ToList();
            }
            catch (JsonException)
            {
                preferences.Remove(TimesKey);
            }
        }

        // Migrate the original single-reminder preference without losing the user's time.
        return [Math.Clamp(preferences.Get(MinutesKey, 19 * 60), 0, (24 * 60) - 1)];
    }
}
