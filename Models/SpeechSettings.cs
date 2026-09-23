namespace BibleRecallTrainerV2.Models;

public sealed record SpeechSettings(string? VoiceId, float Rate, float Pitch)
{
    public static SpeechSettings Default { get; } = new(null, 1.0f, 1.0f);
}
