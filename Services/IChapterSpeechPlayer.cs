using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IChapterSpeechPlayer
{
    bool IsPlaying { get; }

    Task PlayAsync(
        IReadOnlyList<BibleVerse> verses,
        SpeechSettings settings,
        Action<int?> activeVerseChanged,
        CancellationToken cancellationToken = default);

    void Stop();
}
