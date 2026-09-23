using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface ISpeechSettingsService
{
    SpeechSettings Load();
    void Save(SpeechSettings settings);
}
