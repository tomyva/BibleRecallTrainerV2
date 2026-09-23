using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface ISpeechRecognitionService
{
    Task<bool> RequestPermissionsAsync(CancellationToken cancellationToken = default);
    Task<SpeechRecognitionResult> RecognizeAsync(Action<string> partialResult, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void Cancel();
    void OpenApplicationSettings();
}
