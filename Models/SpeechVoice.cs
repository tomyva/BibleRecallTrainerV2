namespace BibleRecallTrainerV2.Models;

public sealed record SpeechVoice(string Id, string Name, string Language, string? Country)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Country)
        ? $"{Name} ({Language})"
        : $"{Name} ({Language}-{Country})";
}
