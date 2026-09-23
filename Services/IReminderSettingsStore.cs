using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IReminderSettingsStore
{
    ReminderSettings Get();
    void Set(ReminderSettings settings);
}
