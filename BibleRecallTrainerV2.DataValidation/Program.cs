using System.Text;
using System.Text.Json;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;
using CommunityToolkit.Maui.Media;

var service = new LocalBibleContentService(new DirectoryAssetReader(Path.Combine(AppContext.BaseDirectory, "Bible")));
var books = await service.GetBooksAsync();

Assert(books.Count == 5, "The syllabus must contain exactly 5 books.");
Assert(books.Sum(book => book.Chapters.Count) == 35, "The syllabus must contain exactly 35 chapters.");

var expectedChapterCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["ruth"] = 4,
    ["1-samuel"] = 7,
    ["ecclesiastes"] = 6,
    ["john"] = 12,
    ["galatians"] = 6
};

foreach (var book in books)
{
    Assert(expectedChapterCounts.TryGetValue(book.Id, out var expected), $"Unexpected syllabus book '{book.Id}'.");
    Assert(book.Chapters.Count == expected, $"{book.DisplayName} must contain {expected} syllabus chapters.");

    foreach (var chapter in book.Chapters)
    {
        var loaded = await service.GetChapterAsync(book.Id, chapter.ChapterNumber);
        Assert(loaded.Verses.Count > 0, $"{book.DisplayName} {chapter.ChapterNumber} must contain verses.");
        Assert(loaded.Verses.Select(verse => verse.VerseNumber).SequenceEqual(Enumerable.Range(1, loaded.Verses.Count)),
            $"{book.DisplayName} {chapter.ChapterNumber} verse numbers must be valid and ordered.");
    }
}

Assert(await service.GetPreviousChapterAsync("ruth", 1) is null, "Ruth 1 must be the first syllabus chapter.");
var afterRuth = await service.GetNextChapterAsync("ruth", 4);
Assert(afterRuth is { BookId: "1-samuel", ChapterNumber: 1 }, "Ruth 4 must advance to 1 Samuel 1.");
Assert(await service.GetNextChapterAsync("galatians", 6) is null, "Galatians 6 must be the final syllabus chapter.");

var malformedService = new LocalBibleContentService(new InMemoryAssetReader("{ not valid json"));
try
{
    await malformedService.GetBooksAsync();
    throw new InvalidOperationException("Malformed JSON should fail validation.");
}
catch (BibleContentException exception)
{
    Assert(exception.Message.Contains("missing or malformed", StringComparison.OrdinalIgnoreCase),
        "Malformed JSON must produce a useful error.");
}

await ValidateSpeechPlaybackAsync();
ValidateRecallComparison();
await ValidateRecognitionSessionsAsync();
ValidateQuizGeneration(books);
await ValidateProgressPersistenceAsync();
await ValidateSpacedRepetitionAsync();
await ValidateStudyPlanAsync(service);
await ValidateRemindersAsync();
await ValidateDataHealthAndExportAsync(service);

Console.WriteLine("Validation passed: Bible data, speech, recall, quizzes, progress, spaced repetition, study planning, reminders, data health, and export.");

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task ValidateSpeechPlaybackAsync()
{
    var settings = SpeechSettings.Default;
    var verses = new[]
    {
        new BibleVerse { VerseNumber = 1, Text = "First verse" },
        new BibleVerse { VerseNumber = 2, Text = "Second verse" }
    };

    var orderedSpeech = new RecordingSpeechService();
    var background = new RecordingBackgroundPlaybackSession();
    var orderedPlayer = new ChapterSpeechPlayer(orderedSpeech, background);
    var activeVerses = new List<int?>();
    await orderedPlayer.PlayAsync(verses, settings, activeVerses.Add);
    Assert(orderedSpeech.Spoken.SequenceEqual(["First verse", "Second verse"]), "Verses must be spoken in order.");
    Assert(activeVerses.SequenceEqual([1, 2, null]), "Active verse must progress in order and clear on completion.");
    Assert(background.BeginCount == 1 && background.EndCount == 1, "A speech session must hold background playback open until it ends.");

    var overlapSpeech = new BlockingFirstSpeechService();
    var overlapPlayer = new ChapterSpeechPlayer(overlapSpeech);
    var firstPlayback = overlapPlayer.PlayAsync(
        [new BibleVerse { VerseNumber = 1, Text = "block" }], settings, _ => { });
    await overlapSpeech.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    var replacementPlayback = overlapPlayer.PlayAsync(
        [new BibleVerse { VerseNumber = 2, Text = "replacement" }], settings, _ => { });
    await AssertThrowsAsync<OperationCanceledException>(() => firstPlayback, "A replacement session must cancel existing playback.");
    await replacementPlayback;
    Assert(overlapSpeech.MaximumConcurrentCalls == 1, "Speech sessions must never overlap.");

    var stopSpeech = new AlwaysBlockingSpeechService();
    var stopPlayer = new ChapterSpeechPlayer(stopSpeech);
    var stoppedPlayback = stopPlayer.PlayAsync(
        [new BibleVerse { VerseNumber = 1, Text = "stop me" }], settings, _ => { });
    await stopSpeech.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    stopPlayer.Stop();
    await AssertThrowsAsync<OperationCanceledException>(() => stoppedPlayback, "Stop must cancel playback.");

    var failurePlayer = new ChapterSpeechPlayer(new FailingSpeechService());
    var failureActiveVerses = new List<int?>();
    await AssertThrowsAsync<InvalidOperationException>(
        () => failurePlayer.PlayAsync(verses, settings, failureActiveVerses.Add),
        "Speech failures must be observable to the caller.");
    Assert(failureActiveVerses.LastOrDefault() is null, "The active verse must clear after a speech failure.");

    var voices = new[]
    {
        new SpeechVoice("en-US", "English US", "en", "US"),
        new SpeechVoice("en-GB", "English UK", "en", "GB")
    };
    Assert(SpeechVoiceResolver.SelectPreferred(voices, "en-GB")?.Id == "en-GB", "An installed saved voice must be retained.");
    Assert(SpeechVoiceResolver.SelectPreferred(voices, "removed-voice")?.Id == "en-US", "A removed voice must fall back safely.");
}

static void ValidateRecallComparison()
{
    var comparer = new RecallComparisonService();
    const string canonical = "Jesus said, “Don’t be afraid.”";
    var exact = comparer.Compare(canonical, "JESUS said don't be afraid");
    Assert(exact.IsExactMatch, "Case, common punctuation, and typographic quotes must not prevent an exact match.");
    Assert(exact.CanonicalText == canonical, "Comparison must preserve the canonical text.");

    var missing = comparer.Compare("one two three", "one three");
    Assert(missing.Words.Any(word => word.Kind is RecallWordKind.Missing && word.Expected == "two"), "Missing words must be identified.");

    var extra = comparer.Compare("one three", "one two three");
    Assert(extra.Words.Any(word => word.Kind is RecallWordKind.Extra && word.Heard == "two"), "Extra words must be identified.");

    var different = comparer.Compare("one two three", "one too three");
    Assert(different.Words.Any(word => word.Kind is RecallWordKind.Different && word.Expected == "two" && word.Heard == "too"),
        "Word substitutions must be identified.");

    var empty = comparer.Compare("one two", string.Empty);
    Assert(empty.MatchPercentage == 0 && empty.Words.Count(word => word.Kind is RecallWordKind.Missing) == 2,
        "Empty transcripts must be handled as missing words.");
}

static async Task ValidateRecognitionSessionsAsync()
{
    var engine = new FakeSpeechToText();
    var service = new ToolkitSpeechRecognitionService(engine);
    var partials = new List<string>();
    var recognition = service.RecognizeAsync(partials.Add);
    await WaitUntilAsync(() => engine.StartCount == 1, "Recognition did not start.");
    engine.ReportPartial("in the");
    engine.Complete("in the beginning");
    var result = await recognition;
    Assert(result.Text == "in the beginning" && partials.SequenceEqual(["in the"]), "Recognition must return partial and final text.");

    var first = service.RecognizeAsync(_ => { });
    await WaitUntilAsync(() => engine.StartCount == 2, "First replacement test session did not start.");
    var replacement = service.RecognizeAsync(_ => { });
    await AssertThrowsAsync<OperationCanceledException>(() => first, "Starting a new recognition session must cancel the previous session.");
    await WaitUntilAsync(() => engine.StartCount == 3, "Replacement recognition session did not start.");
    Assert(engine.MaximumConcurrentSessions == 1, "Recognition sessions must never overlap.");
    engine.Complete("replacement result");
    Assert((await replacement).Text == "replacement result", "The replacement session must receive its own result.");

    engine.StopResult = "stopped result";
    var stopped = service.RecognizeAsync(_ => { });
    await WaitUntilAsync(() => engine.StartCount == 4, "Stop test session did not start.");
    await service.StopAsync();
    Assert((await stopped).Text == "stopped result", "Stop must complete recognition with the final transcript.");

    var latePartials = new List<string>();
    var cancelled = service.RecognizeAsync(latePartials.Add);
    await WaitUntilAsync(() => engine.StartCount == 5, "Cancel test session did not start.");
    service.Cancel();
    await AssertThrowsAsync<OperationCanceledException>(() => cancelled, "Cancel must terminate recognition without an error result.");
    engine.ReportPartial("late result");
    Assert(latePartials.Count == 0, "Late recognition results must not update a cancelled session.");

    var failed = service.RecognizeAsync(_ => { });
    await WaitUntilAsync(() => engine.StartCount == 6, "Failure test session did not start.");
    engine.Fail(new InvalidOperationException("Test recognition failure"));
    await AssertThrowsAsync<InvalidOperationException>(() => failed, "Recognition failures must reach the caller.");
}

static void ValidateQuizGeneration(IReadOnlyList<BibleBook> books)
{
    var book = books.Single(item => item.Id == "ruth");
    var chapter = book.Chapters[0];
    var original = chapter.Verses.Select(verse => verse.Text).ToArray();
    var quiz = new QuizService();
    var questions = quiz.CreateChapterQuiz(book, chapter);
    var repeated = quiz.CreateChapterQuiz(book, chapter);
    Assert(questions.Count == 5, "A normal chapter quiz must contain five questions.");
    Assert(questions.SequenceEqual(repeated), "Quiz generation must be deterministic.");
    Assert(questions.All(question => question.Prompt.Contains("____", StringComparison.Ordinal)), "Every quiz question must contain a blank.");
    Assert(chapter.Verses.Select(verse => verse.Text).SequenceEqual(original), "Quiz generation must not alter canonical verses.");
    var answer = questions[0].Answer;
    Assert(quiz.CheckAnswer(questions[0], answer.ToUpperInvariant()).IsCorrect, "Answer checking must ignore letter case.");
    Assert(!quiz.CheckAnswer(questions[0], "definitely-wrong").IsCorrect, "Incorrect answers must not be accepted.");
}

static async Task ValidateProgressPersistenceAsync()
{
    var store = new MemoryDataStore();
    var progress = new LocalProgressService(store);
    await progress.RecordChapterReadAsync("ruth", 1);
    await progress.RecordRecallAttemptAsync("ruth", 1, 1, 78);
    await progress.RecordQuizCompletedAsync("ruth", 1, 80);
    await progress.RecordQuizCompletedAsync("ruth", 1, 60);

    var snapshot = await progress.GetSnapshotAsync();
    var ruth = snapshot.Chapters.Single();
    Assert(ruth.TimesRead == 1 && ruth.RecallAttempts == 1 && ruth.QuizAttempts == 2, "Progress counters must be persisted.");
    Assert(ruth.BestQuizPercentage == 80, "A later lower quiz result must not reduce the best score.");
    Assert(snapshot.History.Count == 4 && snapshot.History[0].Activity == "Quiz", "Progress history must be newest first.");
    snapshot.Chapters.Clear();
    Assert((await progress.GetSnapshotAsync()).Chapters.Count == 1, "Progress snapshots must not expose mutable internal state.");

    var malformed = new MemoryDataStore { Content = "{ invalid" };
    await AssertThrowsAsync<ProgressDataException>(
        () => new LocalProgressService(malformed).GetSnapshotAsync(),
        "Malformed progress data must produce a useful error.");
}

static async Task ValidateSpacedRepetitionAsync()
{
    var clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
    var reviews = new LocalSpacedRepetitionService(new MemoryDataStore(), clock);
    await reviews.RecordResultAsync("ruth", 1, 60);
    var first = (await reviews.GetScheduleAsync()).Single();
    Assert(first.IntervalDays == 1 && first.SuccessfulReviews == 0, "A weak review must return in one day and reset the streak.");
    Assert((await reviews.GetDueReviewsAsync()).Count == 0, "A newly scheduled review must not be immediately due.");

    clock.UtcNow = clock.UtcNow.AddDays(1);
    Assert((await reviews.GetDueReviewsAsync()).Count == 1, "A review must become due at its scheduled time.");
    await reviews.RecordResultAsync("ruth", 1, 95);
    var strong = (await reviews.GetScheduleAsync()).Single();
    Assert(strong.IntervalDays == 3 && strong.SuccessfulReviews == 1, "A strong result must use at least a three-day interval.");

    clock.UtcNow = clock.UtcNow.AddDays(3);
    await reviews.RecordResultAsync("ruth", 1, 95);
    var repeatedStrong = (await reviews.GetScheduleAsync()).Single();
    Assert(repeatedStrong.IntervalDays == 6 && repeatedStrong.SuccessfulReviews == 2, "Repeated strong results must double the interval.");
}

static async Task ValidateStudyPlanAsync(IBibleContentService bibleContent)
{
    var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
    var progress = new LocalProgressService(new MemoryDataStore());
    var reviews = new LocalSpacedRepetitionService(new MemoryDataStore(), clock);
    var settings = new MemoryStudyPlanSettings { DailyChapterGoal = 2 };
    var plan = new StudyPlanService(bibleContent, progress, reviews, settings, clock);

    var initial = await plan.GetSnapshotAsync();
    Assert(initial.TotalChapters == 35 && initial.CompletedChapters == 0, "A new plan must cover all 35 syllabus chapters.");
    Assert(initial.NextChapter is { BookId: "ruth", ChapterNumber: 1 }, "A new plan must begin at Ruth 1.");
    Assert(initial.DailyChapterGoal == 2 && initial.ChaptersStudiedToday == 0, "The saved daily goal must be reflected in the plan.");

    await progress.RecordChapterReadAsync("ruth", 1);
    await progress.RecordChapterReadAsync("ruth", 2);
    await reviews.RecordResultAsync("ruth", 1, 60);
    clock.UtcNow = clock.UtcNow.AddDays(1);
    var updated = await plan.GetSnapshotAsync();
    Assert(updated.CompletedChapters == 2 && updated.NextChapter is { BookId: "ruth", ChapterNumber: 3 },
        "The plan must continue at the first unread chapter.");
    Assert(updated.DueReviews.Count == 1 && updated.DueReviews[0].DisplayName == "Ruth 1", "Due reviews must have a readable chapter name.");

    plan.SetDailyChapterGoal(4);
    Assert(settings.DailyChapterGoal == 4, "Daily goal changes must be saved.");
}

static async Task ValidateRemindersAsync()
{
    var store = new MemoryReminderSettings();
    var scheduler = new RecordingReminderScheduler { IsSupported = true, ScheduleResult = true };
    var reminders = new ReminderService(store, scheduler);
    var times = new[] { new TimeSpan(8, 0, 0), new TimeSpan(18, 30, 0) };

    var enabledMessage = await reminders.SaveAsync(new ReminderSettings(true, times));
    Assert(store.Settings.IsEnabled && store.Settings.TimesOfDay.SequenceEqual(times), "Enabled reminders must be saved after scheduling succeeds.");
    Assert(scheduler.ScheduledTimes.SequenceEqual(times) && scheduler.RequestedPermission, "Saving must schedule every selected time and request permission.");
    Assert(enabledMessage.Contains("18:30", StringComparison.Ordinal) || enabledMessage.Contains("6:30", StringComparison.Ordinal),
        "The confirmation must include the chosen reminder time.");

    await reminders.SaveAsync(new ReminderSettings(false, times));
    Assert(scheduler.CancelCount == 1 && !store.Settings.IsEnabled, "Turning reminders off must cancel and save the disabled setting.");

    store.Settings = new ReminderSettings(true, times);
    await reminders.EnsureScheduledAsync();
    Assert(!scheduler.RequestedPermission, "Startup restoration must never prompt for notification permission.");

    var unsupportedStore = new MemoryReminderSettings();
    var unsupported = new ReminderService(unsupportedStore, new RecordingReminderScheduler());
    await unsupported.SaveAsync(new ReminderSettings(true, times));
    Assert(!unsupportedStore.Settings.IsEnabled, "Unsupported devices must not save an enabled reminder that cannot run.");
}

static async Task ValidateDataHealthAndExportAsync(IBibleContentService bibleContent)
{
    var progressStore = new MemoryDataStore();
    var reviewStore = new MemoryDataStore();
    var progress = new LocalProgressService(progressStore);
    var reviews = new LocalSpacedRepetitionService(reviewStore, new AdjustableTimeProvider(DateTimeOffset.UtcNow));
    await progress.RecordChapterReadAsync("ruth", 1);
    await reviews.RecordResultAsync("ruth", 1, 88);

    var healthy = await new DataHealthService(bibleContent, progress, reviews).CheckAsync();
    Assert(healthy.IsHealthy, "Valid local progress and review data must pass the health check.");

    var invalidProgress = new StaticProgressService(new LearningProgressData
    {
        Chapters =
        [
            new ChapterProgress { BookId = "not-a-book", ChapterNumber = 99, TimesRead = -1, BestQuizPercentage = 101 },
            new ChapterProgress { BookId = "not-a-book", ChapterNumber = 99 }
        ]
    });
    var invalidHealth = await new DataHealthService(bibleContent, invalidProgress, reviews).CheckAsync();
    Assert(!invalidHealth.IsHealthy && invalidHealth.Issues.Count >= 3,
        "Unknown, duplicate, and invalid progress values must be reported.");

    var clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero));
    var goal = new MemoryStudyPlanSettings { DailyChapterGoal = 3 };
    var reminder = new MemoryReminderSettings { Settings = new ReminderSettings(true, [new TimeSpan(20, 15, 0)]) };
    var positions = new MemoryReadingPositionStore();
    positions.Save("ruth", 1, 7);
    var json = await new ProgressExportService(progress, reviews, goal, reminder, positions, clock).CreateJsonAsync();
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    Assert(root.GetProperty("formatVersion").GetInt32() == 1, "Exports must include a format version.");
    Assert(root.GetProperty("dailyChapterGoal").GetInt32() == 3, "Exports must include the study goal.");
    Assert(root.GetProperty("progress").GetProperty("chapters").GetArrayLength() == 1, "Exports must include progress.");
    Assert(root.GetProperty("reviewSchedule").GetArrayLength() == 1, "Exports must include the review schedule.");
    Assert(root.GetProperty("reminder").GetProperty("isEnabled").GetBoolean(), "Exports must include reminder settings.");
    Assert(root.GetProperty("readingPositions").GetArrayLength() == 1, "Exports must include reading positions.");
}

static async Task WaitUntilAsync(Func<bool> condition, string message)
{
    var timeout = DateTime.UtcNow.AddSeconds(5);
    while (!condition())
    {
        if (DateTime.UtcNow >= timeout)
            throw new InvalidOperationException(message);
        await Task.Delay(10);
    }
}

static async Task AssertThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

file sealed class DirectoryAssetReader(string root) : IBibleAssetReader
{
    public Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(File.OpenRead(Path.Combine(root, relativePath)));
}

file sealed class InMemoryAssetReader(string content) : IBibleAssetReader
{
    public Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content)));
}

file sealed class RecordingSpeechService : ISpeechService
{
    public List<string> Spoken { get; } = [];

    public Task<IReadOnlyList<SpeechVoice>> GetEnglishVoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpeechVoice>>([]);

    public Task SpeakAsync(string text, SpeechSettings settings, CancellationToken cancellationToken = default)
    {
        Spoken.Add(text);
        return Task.CompletedTask;
    }
}

file sealed class RecordingBackgroundPlaybackSession : IBackgroundPlaybackSession
{
    public int BeginCount { get; private set; }
    public int EndCount { get; private set; }
    public void Begin() => BeginCount++;
    public void End() => EndCount++;
}

file sealed class BlockingFirstSpeechService : ISpeechService
{
    private int concurrentCalls;
    public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int MaximumConcurrentCalls { get; private set; }

    public Task<IReadOnlyList<SpeechVoice>> GetEnglishVoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpeechVoice>>([]);

    public async Task SpeakAsync(string text, SpeechSettings settings, CancellationToken cancellationToken = default)
    {
        var current = Interlocked.Increment(ref concurrentCalls);
        MaximumConcurrentCalls = Math.Max(MaximumConcurrentCalls, current);
        try
        {
            if (text == "block")
            {
                FirstStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
        finally
        {
            Interlocked.Decrement(ref concurrentCalls);
        }
    }
}

file sealed class AlwaysBlockingSpeechService : ISpeechService
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<IReadOnlyList<SpeechVoice>> GetEnglishVoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpeechVoice>>([]);

    public async Task SpeakAsync(string text, SpeechSettings settings, CancellationToken cancellationToken = default)
    {
        Started.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}

file sealed class FailingSpeechService : ISpeechService
{
    public Task<IReadOnlyList<SpeechVoice>> GetEnglishVoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpeechVoice>>([]);

    public Task SpeakAsync(string text, SpeechSettings settings, CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException("Test speech failure"));
}

file sealed class FakeSpeechToText : ISpeechToText
{
    private int activeSessions;

    public event EventHandler<SpeechToTextRecognitionResultUpdatedEventArgs> RecognitionResultUpdated = delegate { };
    public event EventHandler<SpeechToTextRecognitionResultCompletedEventArgs> RecognitionResultCompleted = delegate { };
    public event EventHandler<SpeechToTextStateChangedEventArgs> StateChanged = delegate { };

    public SpeechToTextState CurrentState { get; private set; } = SpeechToTextState.Stopped;
    public int StartCount { get; private set; }
    public int MaximumConcurrentSessions { get; private set; }
    public string StopResult { get; set; } = string.Empty;

    public Task<bool> RequestPermissions(CancellationToken cancellationToken) => Task.FromResult(true);

    public Task StartListenAsync(SpeechToTextOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartCount++;
        var active = Interlocked.Increment(ref activeSessions);
        MaximumConcurrentSessions = Math.Max(MaximumConcurrentSessions, active);
        SetState(SpeechToTextState.Listening);
        return Task.CompletedTask;
    }

    public Task StopListenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (CurrentState is SpeechToTextState.Listening)
            Complete(StopResult);
        return Task.CompletedTask;
    }

    public void ReportPartial(string text) =>
        RecognitionResultUpdated(this, new SpeechToTextRecognitionResultUpdatedEventArgs(text));

    public void Complete(string text)
    {
        if (CurrentState is SpeechToTextState.Listening)
            Interlocked.Decrement(ref activeSessions);
        SetState(SpeechToTextState.Stopped);
        RecognitionResultCompleted(this, new SpeechToTextRecognitionResultCompletedEventArgs(new SpeechToTextResult(text, null!)));
    }

    public void Fail(Exception exception)
    {
        if (CurrentState is SpeechToTextState.Listening)
            Interlocked.Decrement(ref activeSessions);
        SetState(SpeechToTextState.Stopped);
        RecognitionResultCompleted(this, new SpeechToTextRecognitionResultCompletedEventArgs(new SpeechToTextResult(string.Empty, exception)));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private void SetState(SpeechToTextState state)
    {
        CurrentState = state;
        StateChanged(this, new SpeechToTextStateChangedEventArgs(state));
    }
}

file sealed class MemoryDataStore : ILocalDataStore
{
    public string? Content { get; set; }

    public Task<string?> ReadAsync(string fileName, CancellationToken cancellationToken = default) => Task.FromResult(Content);

    public Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        Content = content;
        return Task.CompletedTask;
    }
}

file sealed class AdjustableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => UtcNow;
}

file sealed class MemoryStudyPlanSettings : IStudyPlanSettingsStore
{
    public int DailyChapterGoal { get; set; } = 1;
    public int GetDailyChapterGoal() => DailyChapterGoal;
    public void SetDailyChapterGoal(int value) => DailyChapterGoal = Math.Clamp(value, 1, 5);
}

file sealed class MemoryReminderSettings : IReminderSettingsStore
{
    public ReminderSettings Settings { get; set; } = ReminderSettings.Default;
    public ReminderSettings Get() => Settings;
    public void Set(ReminderSettings settings) => Settings = settings;
}

file sealed class RecordingReminderScheduler : ILocalReminderScheduler
{
    public bool IsSupported { get; set; }
    public bool ScheduleResult { get; set; }
    public IReadOnlyList<TimeSpan> ScheduledTimes { get; private set; } = [];
    public bool RequestedPermission { get; private set; }
    public int CancelCount { get; private set; }

    public Task<bool> ScheduleAsync(IReadOnlyList<TimeSpan> timesOfDay, bool requestPermission, CancellationToken cancellationToken = default)
    {
        ScheduledTimes = timesOfDay.ToList();
        RequestedPermission = requestPermission;
        return Task.FromResult(ScheduleResult);
    }

    public Task CancelAsync(CancellationToken cancellationToken = default)
    {
        CancelCount++;
        return Task.CompletedTask;
    }
}

file sealed class MemoryReadingPositionStore : IReadingPositionStore
{
    private readonly List<ReadingPosition> positions = [];
    public ReadingPosition? Get(string bookId, int chapterNumber) => positions.FirstOrDefault(item => item.BookId == bookId && item.ChapterNumber == chapterNumber);
    public IReadOnlyList<ReadingPosition> GetAll() => positions.ToList();
    public void Save(string bookId, int chapterNumber, int verseNumber)
    {
        positions.RemoveAll(item => item.BookId == bookId && item.ChapterNumber == chapterNumber);
        positions.Add(new ReadingPosition(bookId, chapterNumber, verseNumber));
    }
    public void ResetAll() => positions.Clear();
}

file sealed class StaticProgressService(LearningProgressData data) : IProgressService
{
    public Task<LearningProgressData> GetSnapshotAsync(CancellationToken cancellationToken = default) => Task.FromResult(data);
    public Task RecordChapterReadAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RecordRecallAttemptAsync(string bookId, int chapterNumber, int verseNumber, int percentage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RecordQuizCompletedAsync(string bookId, int chapterNumber, int percentage, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
