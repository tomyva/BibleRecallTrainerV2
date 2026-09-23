using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public static class SpeechVoiceResolver
{
    public static SpeechVoice? SelectPreferred(IReadOnlyList<SpeechVoice> voices, string? preferredVoiceId) =>
        voices.FirstOrDefault(voice => string.Equals(voice.Id, preferredVoiceId, StringComparison.OrdinalIgnoreCase))
        ?? voices.FirstOrDefault(voice => voice.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase));
}
