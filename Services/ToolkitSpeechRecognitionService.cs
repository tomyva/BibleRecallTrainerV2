using System.Globalization;
using System.Text;
using BibleRecallTrainerV2.Models;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.ApplicationModel;

namespace BibleRecallTrainerV2.Services;

public sealed class ToolkitSpeechRecognitionService(ISpeechToText speechToText) : ISpeechRecognitionService
{
    private readonly SemaphoreSlim sessionGate = new(1, 1);
    private readonly Lock stateLock = new();
    private CancellationTokenSource? currentSession;

    public async Task<bool> RequestPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var microphone = await Permissions.RequestAsync<Permissions.Microphone>();
        if (microphone is not PermissionStatus.Granted)
            return false;
        return await speechToText.RequestPermissions(cancellationToken);
    }

    public async Task<SpeechRecognitionResult> RecognizeAsync(
        Action<string> partialResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partialResult);
        Cancel();

        var session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (stateLock)
            currentSession = session;
        var enteredGate = false;
        try
        {
            await sessionGate.WaitAsync(session.Token);
            enteredGate = true;
            return await RunSessionAsync(session, partialResult);
        }
        finally
        {
            lock (stateLock)
            {
                if (ReferenceEquals(currentSession, session))
                    currentSession = null;
            }
            session.Dispose();
            if (enteredGate)
                sessionGate.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        speechToText.StopListenAsync(cancellationToken);

    public void Cancel()
    {
        CancellationTokenSource? session;
        lock (stateLock)
            session = currentSession;
        if (session is { IsCancellationRequested: false })
            session.Cancel();
    }

    public void OpenApplicationSettings() => AppInfo.Current.ShowSettingsUI();

    private async Task<SpeechRecognitionResult> RunSessionAsync(
        CancellationTokenSource session,
        Action<string> partialResult)
    {
        var completion = new TaskCompletionSource<SpeechRecognitionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var partialText = new StringBuilder();

        void OnUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.RecognitionResult))
                return;
            if (partialText.Length > 0)
                partialText.Append(' ');
            partialText.Append(args.RecognitionResult.Trim());
            partialResult(partialText.ToString());
        }

        void OnCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
        {
            if (args.RecognitionResult.IsSuccessful)
                completion.TrySetResult(new SpeechRecognitionResult(args.RecognitionResult.Text?.Trim() ?? string.Empty));
            else
                completion.TrySetException(args.RecognitionResult.Exception ?? new InvalidOperationException("Speech recognition did not complete successfully."));
        }

        speechToText.RecognitionResultUpdated += OnUpdated;
        speechToText.RecognitionResultCompleted += OnCompleted;
        using var registration = session.Token.Register(() => completion.TrySetCanceled(session.Token));

        try
        {
            await speechToText.StartListenAsync(new SpeechToTextOptions
            {
                Culture = CultureInfo.GetCultureInfo("en-US"),
                ShouldReportPartialResults = true
            }, session.Token);
            return await completion.Task;
        }
        finally
        {
            speechToText.RecognitionResultUpdated -= OnUpdated;
            speechToText.RecognitionResultCompleted -= OnCompleted;
            await StopQuietlyAsync();
        }
    }

    private async Task StopQuietlyAsync()
    {
        try
        {
            await speechToText.StopListenAsync(CancellationToken.None);
        }
        catch
        {
            // Cancellation cleanup must not replace the original result or error.
        }
    }
}
