using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IReminderService
{
    bool IsSupported { get; }
    ReminderSettings GetSettings();
    Task<string> SaveAsync(ReminderSettings settings, CancellationToken cancellationToken = default);
    Task EnsureScheduledAsync(CancellationToken cancellationToken = default);
}
