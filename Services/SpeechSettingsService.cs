using BibleRecallTrainerV2.Models;
using Microsoft.Maui.Storage;

namespace BibleRecallTrainerV2.Services;

public sealed class SpeechSettingsService(IPreferences preferences) : ISpeechSettingsService
{
    private const string VoiceKey = "speech.voice";
    private const string RateKey = "speech.rate";
    private const string PitchKey = "speech.pitch";

    public SpeechSettings Load() => new(
        NullIfEmpty(preferences.Get(VoiceKey, string.Empty)),
        Math.Clamp(preferences.Get(RateKey, SpeechSettings.Default.Rate), 0.1f, 2.0f),
        Math.Clamp(preferences.Get(PitchKey, SpeechSettings.Default.Pitch), 0.0f, 2.0f));

    public void Save(SpeechSettings settings)
    {
        preferences.Set(VoiceKey, settings.VoiceId ?? string.Empty);
        preferences.Set(RateKey, Math.Clamp(settings.Rate, 0.1f, 2.0f));
        preferences.Set(PitchKey, Math.Clamp(settings.Pitch, 0.0f, 2.0f));
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
