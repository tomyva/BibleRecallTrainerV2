using System.Collections.ObjectModel;
using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class ChapterReaderViewModel : ViewModelBase
{
    private readonly IBibleContentService bibleContent;
    private readonly IChapterSpeechPlayer speechPlayer;
    private readonly ISpeechService speechService;
    private readonly ISpeechSettingsService speechSettingsService;
    private readonly IProgressService progressService;
    private readonly IReadingPositionStore readingPositionStore;
    private IReadOnlyList<BibleVerse> chapterVerses = [];
    private string bookId = string.Empty;
    private int chapterNumber;
    private string heading = string.Empty;
    private string speechStatus = "Preparing speech...";
    private bool hasPreviousChapter;
    private bool hasNextChapter;
    private bool isPlaying;
    private bool isSpeechAvailable;
    private bool speechInitialized;
    private bool suppressSettingsSave;
    private int playbackVersion;
    private double speechRate;
    private double speechPitch;
    private SpeechVoice? selectedVoice;
    private BibleChapterReference? previousChapter;
    private BibleChapterReference? nextChapter;
    private int resumeVerseNumber = 1;
    private int lastSavedVerseNumber;
    private CancellationTokenSource? settingsRestartSession;
    private bool settingsRestartPending;

    public ChapterReaderViewModel(
        IBibleContentService bibleContent,
        IChapterSpeechPlayer speechPlayer,
        ISpeechService speechService,
        ISpeechSettingsService speechSettingsService,
        IProgressService progressService,
        IReadingPositionStore readingPositionStore)
    {
        this.bibleContent = bibleContent;
        this.speechPlayer = speechPlayer;
        this.speechService = speechService;
        this.speechSettingsService = speechSettingsService;
        this.progressService = progressService;
        this.readingPositionStore = readingPositionStore;

        var settings = speechSettingsService.Load();
        speechRate = settings.Rate;
        speechPitch = settings.Pitch;

        ReadChapterCommand = new Command(async () => await ReadChapterAsync(), () => CanRead);
        StopCommand = new Command(StopPlayback, () => IsPlaying || settingsRestartPending);
        PreviousChapterCommand = new Command(async () => await MoveAsync(previousChapter), () => HasPreviousChapter && !IsBusy);
        NextChapterCommand = new Command(async () => await MoveAsync(nextChapter), () => HasNextChapter && !IsBusy);
        PracticeRecallCommand = new Command(async () => await OpenRecallAsync(), () => chapterVerses.Count > 0 && !IsBusy);
        PracticeQuizCommand = new Command(async () => await OpenQuizAsync(), () => chapterVerses.Count > 0 && !IsBusy);
        ReadFromVerseCommand = new Command<ReaderVerseViewModel>(async verse => await ReadFromVerseAsync(verse), verse => verse is not null && IsSpeechAvailable && !IsBusy);
        BackCommand = new Command(async () => await GoBackAsync());
    }

    public event EventHandler<ReaderVerseViewModel>? ActiveVerseRequested;
    public event EventHandler<ReaderVerseViewModel>? ReadingPositionRequested;

    public string Heading
    {
        get => heading;
        private set => SetProperty(ref heading, value);
    }

    public string SpeechStatus
    {
        get => speechStatus;
        private set => SetProperty(ref speechStatus, value);
    }

    public bool IsPlaying
    {
        get => isPlaying;
        private set
        {
            if (!SetProperty(ref isPlaying, value))
                return;
            OnPropertyChanged(nameof(CanRead));
            RefreshCommands();
        }
    }

    public bool IsSpeechAvailable
    {
        get => isSpeechAvailable;
        private set
        {
            if (!SetProperty(ref isSpeechAvailable, value))
                return;
            OnPropertyChanged(nameof(CanRead));
            RefreshCommands();
        }
    }

    public bool CanRead => IsSpeechAvailable && !IsPlaying && !settingsRestartPending && !IsBusy && chapterVerses.Count > 0;

    public bool HasPreviousChapter
    {
        get => hasPreviousChapter;
        private set
        {
            if (SetProperty(ref hasPreviousChapter, value))
                RefreshCommands();
        }
    }

    public bool HasNextChapter
    {
        get => hasNextChapter;
        private set
        {
            if (SetProperty(ref hasNextChapter, value))
                RefreshCommands();
        }
    }

    public SpeechVoice? SelectedVoice
    {
        get => selectedVoice;
        set
        {
            if (!SetProperty(ref selectedVoice, value) || suppressSettingsSave)
                return;
            SaveSpeechSettings();
            if (value is null)
            {
                StopPlayback();
                SpeechStatus = "Speech unavailable";
            }
            else if (!SchedulePlaybackRestart())
            {
                SpeechStatus = "Ready to read";
            }
        }
    }

    public double SpeechRate
    {
        get => speechRate;
        set
        {
            if (SetProperty(ref speechRate, value) && !suppressSettingsSave)
            {
                SaveSpeechSettings();
                SchedulePlaybackRestart();
            }
        }
    }

    public double SpeechPitch
    {
        get => speechPitch;
        set
        {
            if (SetProperty(ref speechPitch, value) && !suppressSettingsSave)
            {
                SaveSpeechSettings();
                SchedulePlaybackRestart();
            }
        }
    }

    public ObservableCollection<ReaderVerseViewModel> Verses { get; } = [];
    public ObservableCollection<SpeechVoice> AvailableVoices { get; } = [];
    public ICommand ReadChapterCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand PreviousChapterCommand { get; }
    public ICommand NextChapterCommand { get; }
    public ICommand PracticeRecallCommand { get; }
    public ICommand PracticeQuizCommand { get; }
    public ICommand ReadFromVerseCommand { get; }
    public ICommand BackCommand { get; }

    public Task LoadAsync(string id, int chapter) => LoadChapterAsync(id, chapter, stopPlayback: true, resumeSavedPosition: true);

    private async Task LoadChapterAsync(string id, int chapter, bool stopPlayback, bool resumeSavedPosition = true)
    {
        if (stopPlayback)
            StopPlayback();
        IsBusy = true;
        ErrorMessage = string.Empty;
        RefreshCommands();
        try
        {
            var books = await bibleContent.GetBooksAsync();
            var book = books.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                ?? throw new BibleContentException($"Book '{id}' is not included in the syllabus.");
            var content = await bibleContent.GetChapterAsync(book.Id, chapter);
            bookId = book.Id;
            chapterNumber = chapter;
            Heading = $"{book.DisplayName} {chapter}";
            chapterVerses = content.Verses;
            Verses.Clear();
            foreach (var verse in chapterVerses)
                Verses.Add(new ReaderVerseViewModel(verse));

            var savedPosition = resumeSavedPosition ? readingPositionStore.Get(bookId, chapterNumber) : null;
            resumeVerseNumber = Math.Clamp(savedPosition?.VerseNumber ?? 1, 1, chapterVerses.Count);
            lastSavedVerseNumber = resumeVerseNumber;
            if (resumeVerseNumber > 1)
            {
                SpeechStatus = $"Resume from verse {resumeVerseNumber}";
                ReadingPositionRequested?.Invoke(this, Verses[resumeVerseNumber - 1]);
            }

            previousChapter = await bibleContent.GetPreviousChapterAsync(bookId, chapterNumber);
            nextChapter = await bibleContent.GetNextChapterAsync(bookId, chapterNumber);
            HasPreviousChapter = previousChapter is not null;
            HasNextChapter = nextChapter is not null;
            await InitializeSpeechAsync();
            try
            {
                await progressService.RecordChapterReadAsync(bookId, chapterNumber);
            }
            catch (ProgressDataException exception)
            {
                ErrorMessage = $"The chapter loaded, but progress could not be saved: {exception.Message}";
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            chapterVerses = [];
            Verses.Clear();
            previousChapter = null;
            nextChapter = null;
            HasPreviousChapter = false;
            HasNextChapter = false;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanRead));
            RefreshCommands();
        }
    }

    public void StopPlayback()
    {
        settingsRestartSession?.Cancel();
        settingsRestartSession?.Dispose();
        settingsRestartSession = null;
        var wasActive = IsPlaying || settingsRestartPending;
        settingsRestartPending = false;
        playbackVersion++;
        speechPlayer.Stop();
        ClearActiveVerse();
        if (wasActive)
            SpeechStatus = "Stopped";
        IsPlaying = false;
        OnPropertyChanged(nameof(CanRead));
        RefreshCommands();
    }

    public void SaveVisiblePosition(int itemIndex)
    {
        // Automatic centering during speech can expose an earlier verse at the top;
        // the speech callback is the authoritative listening position while playing.
        if (IsPlaying || itemIndex < 0 || itemIndex >= Verses.Count)
            return;

        SavePosition(Verses[itemIndex].VerseNumber);
    }

    private async Task InitializeSpeechAsync()
    {
        if (speechInitialized)
            return;

        try
        {
            var voices = await speechService.GetEnglishVoicesAsync();
            AvailableVoices.Clear();
            foreach (var voice in voices)
                AvailableVoices.Add(voice);

            var saved = speechSettingsService.Load();
            suppressSettingsSave = true;
            SelectedVoice = SpeechVoiceResolver.SelectPreferred(AvailableVoices, saved.VoiceId);
            suppressSettingsSave = false;
            IsSpeechAvailable = SelectedVoice is not null;
            SpeechStatus = IsSpeechAvailable
                ? "Ready to read"
                : "Speech unavailable. Install an English text-to-speech voice in device settings.";

            if (SelectedVoice is not null && !string.Equals(SelectedVoice.Id, saved.VoiceId, StringComparison.OrdinalIgnoreCase))
                SaveSpeechSettings();
        }
        catch (Exception exception)
        {
            suppressSettingsSave = false;
            IsSpeechAvailable = false;
            SpeechStatus = $"Speech unavailable: {exception.Message}";
        }
        finally
        {
            speechInitialized = true;
        }
    }

    private async Task ReadChapterAsync()
    {
        if (!CanRead || SelectedVoice is null)
            return;

        var version = ++playbackVersion;
        IsPlaying = true;
        SpeechStatus = "Starting...";
        ErrorMessage = string.Empty;
        try
        {
            while (version == playbackVersion)
            {
                var versesToRead = chapterVerses.Where(verse => verse.VerseNumber >= resumeVerseNumber).ToList();
                await speechPlayer.PlayAsync(versesToRead, CurrentSettings(), verseNumber => SetActiveVerse(verseNumber, version));
                if (version != playbackVersion)
                    return;

                var followingChapter = nextChapter;
                if (followingChapter is null || !string.Equals(followingChapter.BookId, bookId, StringComparison.OrdinalIgnoreCase))
                {
                    SpeechStatus = "Book completed";
                    return;
                }

                await LoadChapterAsync(
                    followingChapter.BookId,
                    followingChapter.ChapterNumber,
                    stopPlayback: false,
                    resumeSavedPosition: false);
            }
        }
        catch (OperationCanceledException)
        {
            if (version == playbackVersion)
                SpeechStatus = "Stopped";
        }
        catch (Exception exception)
        {
            if (version == playbackVersion)
                SpeechStatus = $"Playback failed: {exception.Message}";
        }
        finally
        {
            if (version == playbackVersion)
            {
                IsPlaying = false;
                ClearActiveVerse();
            }
        }
    }

    private void SetActiveVerse(int? verseNumber, int version)
    {
        if (version != playbackVersion)
            return;

        ReaderVerseViewModel? active = null;
        foreach (var verse in Verses)
        {
            verse.IsActive = verse.VerseNumber == verseNumber;
            if (verse.IsActive)
                active = verse;
        }

        if (active is not null)
        {
            resumeVerseNumber = active.VerseNumber;
            SavePosition(active.VerseNumber);
            SpeechStatus = $"Reading verse {active.VerseNumber}";
            ActiveVerseRequested?.Invoke(this, active);
        }
    }

    private void ClearActiveVerse()
    {
        foreach (var verse in Verses)
            verse.IsActive = false;
    }

    private async Task MoveAsync(BibleChapterReference? target)
    {
        if (target is null)
            return;
        var continueReading = IsPlaying || settingsRestartPending;
        StopPlayback();
        await LoadAsync(target.BookId, target.ChapterNumber);
        if (continueReading && CanRead)
            await ReadChapterAsync();
    }

    private async Task ReadFromVerseAsync(ReaderVerseViewModel? verse)
    {
        if (verse is null || !IsSpeechAvailable || IsBusy)
            return;

        StopPlayback();
        resumeVerseNumber = verse.VerseNumber;
        SavePosition(verse.VerseNumber);
        foreach (var item in Verses)
            item.IsActive = ReferenceEquals(item, verse);
        SpeechStatus = $"Starting from verse {verse.VerseNumber}...";
        await ReadChapterAsync();
    }

    private async Task GoBackAsync()
    {
        StopPlayback();
        await Shell.Current.GoToAsync("..");
    }

    private async Task OpenRecallAsync()
    {
        StopPlayback();
        await Shell.Current.GoToAsync($"{nameof(RecallPage)}?bookId={Uri.EscapeDataString(bookId)}&chapter={chapterNumber}");
    }

    private async Task OpenQuizAsync()
    {
        StopPlayback();
        await Shell.Current.GoToAsync($"{nameof(QuizPage)}?bookId={Uri.EscapeDataString(bookId)}&chapter={chapterNumber}");
    }

    private SpeechSettings CurrentSettings() => new(
        SelectedVoice?.Id,
        (float)Math.Clamp(SpeechRate, 0.1, 2.0),
        (float)Math.Clamp(SpeechPitch, 0.0, 2.0));

    private void SaveSpeechSettings() => speechSettingsService.Save(CurrentSettings());

    private bool SchedulePlaybackRestart()
    {
        if (!IsPlaying && !settingsRestartPending)
            return false;

        settingsRestartSession?.Cancel();
        settingsRestartSession?.Dispose();

        if (IsPlaying)
        {
            playbackVersion++;
            speechPlayer.Stop();
            ClearActiveVerse();
            IsPlaying = false;
        }

        settingsRestartPending = true;
        SpeechStatus = "Applying speech settings...";
        OnPropertyChanged(nameof(CanRead));
        RefreshCommands();

        var session = new CancellationTokenSource();
        settingsRestartSession = session;
        _ = RestartPlaybackAfterSettingsChangeAsync(session);
        return true;
    }

    private async Task RestartPlaybackAfterSettingsChangeAsync(CancellationTokenSource session)
    {
        try
        {
            await Task.Delay(300, session.Token);
            if (!ReferenceEquals(settingsRestartSession, session))
                return;

            settingsRestartSession = null;
            settingsRestartPending = false;
            OnPropertyChanged(nameof(CanRead));
            RefreshCommands();
            await ReadChapterAsync();
        }
        catch (OperationCanceledException)
        {
            // A newer slider value or an explicit stop superseded this restart.
        }
        finally
        {
            session.Dispose();
        }
    }

    private void SavePosition(int verseNumber)
    {
        if (verseNumber == lastSavedVerseNumber)
            return;

        lastSavedVerseNumber = verseNumber;
        resumeVerseNumber = verseNumber;
        readingPositionStore.Save(bookId, chapterNumber, verseNumber);
    }

    private void RefreshCommands()
    {
        ((Command)ReadChapterCommand).ChangeCanExecute();
        ((Command)StopCommand).ChangeCanExecute();
        ((Command)PreviousChapterCommand).ChangeCanExecute();
        ((Command)NextChapterCommand).ChangeCanExecute();
        ((Command)PracticeRecallCommand).ChangeCanExecute();
        ((Command)PracticeQuizCommand).ChangeCanExecute();
        ((Command<ReaderVerseViewModel>)ReadFromVerseCommand).ChangeCanExecute();
    }
}
