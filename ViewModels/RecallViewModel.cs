using System.Collections.ObjectModel;
using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;
using Microsoft.Maui.ApplicationModel;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class RecallViewModel : ViewModelBase
{
    private readonly IBibleContentService bibleContent;
    private readonly ISpeechRecognitionService recognitionService;
    private readonly IRecallComparisonService comparisonService;
    private readonly IChapterSpeechPlayer speechPlayer;
    private readonly ISpeechService speechService;
    private readonly ISpeechSettingsService speechSettingsService;
    private readonly IProgressService progressService;
    private IReadOnlyList<BibleVerse> verses = [];
    private CancellationTokenSource? recognitionCancellation;
    private int currentIndex;
    private int interactionVersion;
    private string bookName = string.Empty;
    private int chapterNumber;
    private string bookId = string.Empty;
    private string transcript = string.Empty;
    private string status = "Prepare the verse, hide it, then start recall.";
    private string comparisonFeedback = string.Empty;
    private bool isVerseHidden;
    private bool isListening;
    private bool hasComparison;
    private bool isExactMatch;
    private bool canOpenSettings;

    public RecallViewModel(
        IBibleContentService bibleContent,
        ISpeechRecognitionService recognitionService,
        IRecallComparisonService comparisonService,
        IChapterSpeechPlayer speechPlayer,
        ISpeechService speechService,
        ISpeechSettingsService speechSettingsService,
        IProgressService progressService)
    {
        this.bibleContent = bibleContent;
        this.recognitionService = recognitionService;
        this.comparisonService = comparisonService;
        this.speechPlayer = speechPlayer;
        this.speechService = speechService;
        this.speechSettingsService = speechSettingsService;
        this.progressService = progressService;

        ListenCommand = new Command(async () => await ListenAsync(), () => CanInteract);
        ToggleVerseCommand = new Command(ToggleVerse, () => CurrentVerse is not null);
        StartRecallCommand = new Command(async () => await StartRecallAsync(), () => CanInteract);
        StopRecallCommand = new Command(async () => await StopRecallAsync(), () => IsListening);
        CancelRecallCommand = new Command(CancelRecognition, () => IsListening);
        RevealAndCompareCommand = new Command(async () => await RevealAndCompareAsync(), () => HasTranscript && !IsListening);
        TryAgainCommand = new Command(async () => await TryAgainAsync(), () => !IsListening && CurrentVerse is not null);
        ClearTranscriptCommand = new Command(ClearAttempt, () => HasTranscript && !IsListening);
        PreviousVerseCommand = new Command(() => MoveVerse(-1), () => HasPreviousVerse && !IsListening);
        NextVerseCommand = new Command(() => MoveVerse(1), () => HasNextVerse && !IsListening);
        OpenSettingsCommand = new Command(recognitionService.OpenApplicationSettings, () => CanOpenSettings);
        BackCommand = new Command(async () => await GoBackAsync());
    }

    public string Reference => CurrentVerse is null ? string.Empty : $"{bookName} {chapterNumber}:{CurrentVerse.VerseNumber}";
    public string Position => verses.Count == 0 ? string.Empty : $"Verse {currentIndex + 1} of {verses.Count}";
    public BibleVerse? CurrentVerse => currentIndex >= 0 && currentIndex < verses.Count ? verses[currentIndex] : null;
    public string CanonicalText => CurrentVerse?.Text ?? string.Empty;
    public bool IsCanonicalVisible => !IsVerseHidden;
    public string ToggleVerseText => IsVerseHidden ? "Show Verse" : "Hide Verse";
    public bool HasPreviousVerse => currentIndex > 0;
    public bool HasNextVerse => currentIndex >= 0 && currentIndex < verses.Count - 1;
    public bool HasTranscript => !string.IsNullOrWhiteSpace(Transcript);
    public bool CanInteract => CurrentVerse is not null && !IsListening && !IsBusy;

    public string Transcript
    {
        get => transcript;
        private set
        {
            if (!SetProperty(ref transcript, value))
                return;
            OnPropertyChanged(nameof(HasTranscript));
            RefreshCommands();
        }
    }

    public string Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public string ComparisonFeedback
    {
        get => comparisonFeedback;
        private set => SetProperty(ref comparisonFeedback, value);
    }

    public bool IsVerseHidden
    {
        get => isVerseHidden;
        private set
        {
            if (!SetProperty(ref isVerseHidden, value))
                return;
            OnPropertyChanged(nameof(IsCanonicalVisible));
            OnPropertyChanged(nameof(ToggleVerseText));
        }
    }

    public bool IsListening
    {
        get => isListening;
        private set
        {
            if (!SetProperty(ref isListening, value))
                return;
            OnPropertyChanged(nameof(CanInteract));
            RefreshCommands();
        }
    }

    public bool HasComparison
    {
        get => hasComparison;
        private set => SetProperty(ref hasComparison, value);
    }

    public bool IsExactMatch
    {
        get => isExactMatch;
        private set => SetProperty(ref isExactMatch, value);
    }

    public bool CanOpenSettings
    {
        get => canOpenSettings;
        private set
        {
            if (SetProperty(ref canOpenSettings, value))
                ((Command)OpenSettingsCommand).ChangeCanExecute();
        }
    }

    public ObservableCollection<RecallWordDifference> Differences { get; } = [];
    public ICommand ListenCommand { get; }
    public ICommand ToggleVerseCommand { get; }
    public ICommand StartRecallCommand { get; }
    public ICommand StopRecallCommand { get; }
    public ICommand CancelRecallCommand { get; }
    public ICommand RevealAndCompareCommand { get; }
    public ICommand TryAgainCommand { get; }
    public ICommand ClearTranscriptCommand { get; }
    public ICommand PreviousVerseCommand { get; }
    public ICommand NextVerseCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand BackCommand { get; }

    public async Task LoadAsync(string bookId, int chapter)
    {
        CancelSessions();
        IsBusy = true;
        ErrorMessage = string.Empty;
        RefreshCommands();
        try
        {
            var books = await bibleContent.GetBooksAsync();
            var book = books.FirstOrDefault(item => string.Equals(item.Id, bookId, StringComparison.OrdinalIgnoreCase))
                ?? throw new BibleContentException($"Book '{bookId}' is not included in the syllabus.");
            var content = await bibleContent.GetChapterAsync(book.Id, chapter);
            bookName = book.DisplayName;
            this.bookId = book.Id;
            chapterNumber = chapter;
            verses = content.Verses;
            currentIndex = 0;
            ResetAttempt(hideVerse: false);
            NotifyVerseChanged();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            verses = [];
            NotifyVerseChanged();
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanInteract));
            RefreshCommands();
        }
    }

    public void CancelSessions()
    {
        interactionVersion++;
        speechPlayer.Stop();
        recognitionCancellation?.Cancel();
        recognitionCancellation?.Dispose();
        recognitionCancellation = null;
        recognitionService.Cancel();
        IsListening = false;
    }

    private async Task StartRecallAsync()
    {
        if (!CanInteract || CurrentVerse is null)
            return;

        CancelSessions();
        speechPlayer.Stop();
        var version = ++interactionVersion;
        var cancellation = new CancellationTokenSource();
        recognitionCancellation = cancellation;
        Transcript = string.Empty;
        HasComparison = false;
        Differences.Clear();
        ComparisonFeedback = string.Empty;
        ErrorMessage = string.Empty;
        CanOpenSettings = false;
        Status = "Requesting microphone permission...";

        try
        {
            var granted = await recognitionService.RequestPermissionsAsync(cancellation.Token);
            if (version != interactionVersion)
                return;
            if (!granted)
            {
                Status = "Microphone or speech-recognition permission was denied. You can enable it in application settings.";
                CanOpenSettings = true;
                return;
            }

            IsListening = true;
            Status = "Listening — recite the hidden verse now.";
            var result = await recognitionService.RecognizeAsync(
                partial => ApplyPartialResult(partial, version),
                cancellation.Token);
            if (version != interactionVersion)
                return;

            Transcript = result.Text;
            Status = HasTranscript
                ? "Recognition complete. Reveal the verse to compare."
                : "No speech was recognized. Try again when you are ready.";
        }
        catch (OperationCanceledException)
        {
            if (version == interactionVersion)
                Status = "Recall cancelled.";
        }
        catch (Exception exception)
        {
            if (version == interactionVersion)
                Status = $"Recognition failed: {exception.Message}";
        }
        finally
        {
            if (version == interactionVersion)
            {
                IsListening = false;
                recognitionCancellation?.Dispose();
                recognitionCancellation = null;
            }
        }
    }

    private void ApplyPartialResult(string partial, int version) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (version == interactionVersion && IsListening)
            Transcript = partial;
    });

    private async Task StopRecallAsync()
    {
        if (!IsListening)
            return;
        Status = "Processing your recall...";
        try
        {
            await recognitionService.StopAsync();
        }
        catch (Exception exception)
        {
            Status = $"Unable to stop recognition cleanly: {exception.Message}";
            CancelRecognition();
        }
    }

    private void CancelRecognition()
    {
        var wasListening = IsListening;
        CancelSessions();
        if (wasListening)
            Status = "Recall cancelled.";
    }

    private async Task ListenAsync()
    {
        if (!CanInteract || CurrentVerse is null)
            return;

        CancelSessions();
        var version = ++interactionVersion;
        Status = "Preparing the verse...";
        try
        {
            var saved = speechSettingsService.Load();
            var voices = await speechService.GetEnglishVoicesAsync();
            var voice = SpeechVoiceResolver.SelectPreferred(voices, saved.VoiceId);
            if (voice is null)
            {
                Status = "Text-to-Speech is unavailable because no English voice is installed.";
                return;
            }

            if (version != interactionVersion || CurrentVerse is null)
                return;

            Status = "Listening to the verse.";
            await speechPlayer.PlayAsync(
                [CurrentVerse],
                saved with { VoiceId = voice.Id },
                _ => { });
            if (version == interactionVersion)
                Status = "Ready for recall.";
        }
        catch (OperationCanceledException)
        {
            if (version == interactionVersion)
                Status = "Verse playback stopped.";
        }
        catch (Exception exception)
        {
            if (version == interactionVersion)
                Status = $"Unable to read the verse: {exception.Message}";
        }
    }

    private void ToggleVerse() => IsVerseHidden = !IsVerseHidden;

    private async Task RevealAndCompareAsync()
    {
        if (CurrentVerse is null || !HasTranscript)
            return;

        IsVerseHidden = false;
        var result = comparisonService.Compare(CurrentVerse.Text, Transcript);
        Differences.Clear();
        foreach (var word in result.Words.Where(word => word.Kind is not RecallWordKind.Match))
            Differences.Add(word);
        ComparisonFeedback = $"{result.Feedback} Basic word-level match: {result.MatchPercentage}%.";
        IsExactMatch = result.IsExactMatch;
        HasComparison = true;
        Status = "Comparison ready.";
        try
        {
            await progressService.RecordRecallAttemptAsync(bookId, chapterNumber, CurrentVerse.VerseNumber, result.MatchPercentage);
        }
        catch (ProgressDataException exception)
        {
            ErrorMessage = $"Comparison completed, but progress could not be saved: {exception.Message}";
        }
    }

    private async Task TryAgainAsync()
    {
        ResetAttempt(hideVerse: true);
        await StartRecallAsync();
    }

    private void ClearAttempt()
    {
        ResetAttempt(IsVerseHidden);
        Status = "Transcript cleared. Start recall when you are ready.";
    }

    private void MoveVerse(int offset)
    {
        var next = currentIndex + offset;
        if (next < 0 || next >= verses.Count)
            return;
        CancelSessions();
        currentIndex = next;
        ResetAttempt(hideVerse: false);
        NotifyVerseChanged();
        Status = "Prepare the verse, hide it, then start recall.";
    }

    private void ResetAttempt(bool hideVerse)
    {
        Transcript = string.Empty;
        IsVerseHidden = hideVerse;
        HasComparison = false;
        IsExactMatch = false;
        ComparisonFeedback = string.Empty;
        Differences.Clear();
        CanOpenSettings = false;
    }

    private void NotifyVerseChanged()
    {
        OnPropertyChanged(nameof(CurrentVerse));
        OnPropertyChanged(nameof(Reference));
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(CanonicalText));
        OnPropertyChanged(nameof(HasPreviousVerse));
        OnPropertyChanged(nameof(HasNextVerse));
        OnPropertyChanged(nameof(CanInteract));
        RefreshCommands();
    }

    private async Task GoBackAsync()
    {
        CancelSessions();
        await Shell.Current.GoToAsync("..");
    }

    private void RefreshCommands()
    {
        ((Command)ListenCommand).ChangeCanExecute();
        ((Command)ToggleVerseCommand).ChangeCanExecute();
        ((Command)StartRecallCommand).ChangeCanExecute();
        ((Command)StopRecallCommand).ChangeCanExecute();
        ((Command)CancelRecallCommand).ChangeCanExecute();
        ((Command)RevealAndCompareCommand).ChangeCanExecute();
        ((Command)TryAgainCommand).ChangeCanExecute();
        ((Command)ClearTranscriptCommand).ChangeCanExecute();
        ((Command)PreviousVerseCommand).ChangeCanExecute();
        ((Command)NextVerseCommand).ChangeCanExecute();
    }
}
