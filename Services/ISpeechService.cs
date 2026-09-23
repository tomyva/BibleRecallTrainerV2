using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface ISpeechService
{
    Task<IReadOnlyList<SpeechVoice>> GetEnglishVoicesAsync(CancellationToken cancellationToken = default);
    Task SpeakAsync(string text, SpeechSettings settings, CancellationToken cancellationToken = default);
}
