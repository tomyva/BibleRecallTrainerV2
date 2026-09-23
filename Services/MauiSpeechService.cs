using BibleRecallTrainerV2.Models;
using Microsoft.Maui.Media;

namespace BibleRecallTrainerV2.Services;

public sealed class MauiSpeechService(ITextToSpeech textToSpeech) : ISpeechService
{
    private IReadOnlyList<Locale>? locales;

    public async Task<IReadOnlyList<SpeechVoice>> GetEnglishVoicesAsync(CancellationToken cancellationToken = default)
    {
        var available = await GetLocalesAsync(cancellationToken);
        return available
            .Where(locale => locale.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            .OrderBy(locale => locale.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(locale => new SpeechVoice(locale.Id, locale.Name, locale.Language, locale.Country))
            .ToList();
    }

    public async Task SpeakAsync(string text, SpeechSettings settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var available = await GetLocalesAsync(cancellationToken);
        var locale = available.FirstOrDefault(item =>
            string.Equals(item.Id, settings.VoiceId, StringComparison.OrdinalIgnoreCase));
        var options = new SpeechOptions
        {
            Locale = locale,
            Rate = Math.Clamp(settings.Rate, 0.1f, 2.0f),
            Pitch = Math.Clamp(settings.Pitch, 0.0f, 2.0f),
            Volume = 1.0f
        };

        await textToSpeech.SpeakAsync(text, options, cancellationToken);
    }

    private async Task<IReadOnlyList<Locale>> GetLocalesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (locales is not null)
            return locales;

        locales = (await textToSpeech.GetLocalesAsync()).ToList();
        cancellationToken.ThrowIfCancellationRequested();
        return locales;
    }
}
