using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class ChapterSpeechPlayer(
    ISpeechService speechService,
    IBackgroundPlaybackSession? backgroundPlayback = null) : IChapterSpeechPlayer
{
    private readonly Lock stateLock = new();
    private readonly SemaphoreSlim playbackGate = new(1, 1);
    private CancellationTokenSource? currentSession;

    public bool IsPlaying
    {
        get
        {
            lock (stateLock)
                return currentSession is { IsCancellationRequested: false };
        }
    }

    public async Task PlayAsync(
        IReadOnlyList<BibleVerse> verses,
        SpeechSettings settings,
        Action<int?> activeVerseChanged,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verses);
        ArgumentNullException.ThrowIfNull(activeVerseChanged);

        Stop();
        var session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (stateLock)
            currentSession = session;

        var enteredGate = false;
        try
        {
            await playbackGate.WaitAsync(session.Token);
            enteredGate = true;
            TryBeginBackgroundPlayback();

            foreach (var verse in verses)
            {
                session.Token.ThrowIfCancellationRequested();
                activeVerseChanged(verse.VerseNumber);
                await speechService.SpeakAsync(verse.Text, settings, session.Token);
            }
        }
        finally
        {
            if (enteredGate)
                TryEndBackgroundPlayback();
            activeVerseChanged(null);
            lock (stateLock)
            {
                if (ReferenceEquals(currentSession, session))
                    currentSession = null;
            }

            if (enteredGate)
                playbackGate.Release();
            session.Dispose();
        }
    }

    private void TryBeginBackgroundPlayback()
    {
        try
        {
            backgroundPlayback?.Begin();
        }
        catch
        {
            // Playback must still work if a platform cannot start its background session.
        }
    }

    private void TryEndBackgroundPlayback()
    {
        try
        {
            backgroundPlayback?.End();
        }
        catch
        {
            // Ending a platform session is best-effort and must not hide speech errors.
        }
    }

    public void Stop()
    {
        CancellationTokenSource? session;
        lock (stateLock)
            session = currentSession;

        if (session is { IsCancellationRequested: false })
            session.Cancel();
    }
}
