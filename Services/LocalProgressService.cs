using System.Text.Json;
using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class LocalProgressService(ILocalDataStore dataStore) : IProgressService
{
    private const string FileName = "learning-progress.json";
    private const int MaximumHistoryEntries = 250;
    private readonly SemaphoreSlim accessGate = new(1, 1);
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private LearningProgressData? cache;

    public async Task<LearningProgressData> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await accessGate.WaitAsync(cancellationToken);
        try
        {
            var data = await LoadAsync(cancellationToken);
            return Clone(data);
        }
        finally
        {
            accessGate.Release();
        }
    }

    public Task RecordChapterReadAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default) =>
        UpdateAsync(bookId, chapterNumber, progress => progress.TimesRead++,
            new PracticeHistoryEntry { Activity = "Read", BookId = bookId, ChapterNumber = chapterNumber, CompletedUtc = DateTimeOffset.UtcNow },
            cancellationToken);

    public Task RecordRecallAttemptAsync(
        string bookId,
        int chapterNumber,
        int verseNumber,
        int percentage,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(bookId, chapterNumber, progress => progress.RecallAttempts++,
            new PracticeHistoryEntry
            {
                Activity = "VoiceRecall",
                BookId = bookId,
                ChapterNumber = chapterNumber,
                VerseNumber = verseNumber,
                Percentage = Math.Clamp(percentage, 0, 100),
                CompletedUtc = DateTimeOffset.UtcNow
            }, cancellationToken);

    public Task RecordQuizCompletedAsync(
        string bookId,
        int chapterNumber,
        int percentage,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(bookId, chapterNumber, progress =>
        {
            progress.QuizAttempts++;
            progress.BestQuizPercentage = Math.Max(progress.BestQuizPercentage, Math.Clamp(percentage, 0, 100));
        }, new PracticeHistoryEntry
        {
            Activity = "Quiz",
            BookId = bookId,
            ChapterNumber = chapterNumber,
            Percentage = Math.Clamp(percentage, 0, 100),
            CompletedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

    private async Task UpdateAsync(
        string bookId,
        int chapterNumber,
        Action<ChapterProgress> update,
        PracticeHistoryEntry history,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bookId) || chapterNumber <= 0)
            throw new ArgumentException("A valid chapter reference is required.");

        await accessGate.WaitAsync(cancellationToken);
        try
        {
            var data = await LoadAsync(cancellationToken);
            var progress = data.Chapters.FirstOrDefault(item =>
                string.Equals(item.BookId, bookId, StringComparison.OrdinalIgnoreCase) && item.ChapterNumber == chapterNumber);
            if (progress is null)
            {
                progress = new ChapterProgress { BookId = bookId, ChapterNumber = chapterNumber };
                data.Chapters.Add(progress);
            }

            update(progress);
            progress.LastStudiedUtc = history.CompletedUtc;
            data.History.Insert(0, history);
            if (data.History.Count > MaximumHistoryEntries)
                data.History.RemoveRange(MaximumHistoryEntries, data.History.Count - MaximumHistoryEntries);
            await SaveAsync(data, cancellationToken);
        }
        finally
        {
            accessGate.Release();
        }
    }

    private async Task<LearningProgressData> LoadAsync(CancellationToken cancellationToken)
    {
        if (cache is not null)
            return cache;

        try
        {
            var json = await dataStore.ReadAsync(FileName, cancellationToken);
            cache = string.IsNullOrWhiteSpace(json)
                ? new LearningProgressData()
                : JsonSerializer.Deserialize<LearningProgressData>(json, jsonOptions)
                  ?? throw new ProgressDataException("The local learning-progress file is empty.");
            if (cache.SchemaVersion != 1)
                throw new ProgressDataException($"Unsupported learning-progress schema version {cache.SchemaVersion}.");
            return cache;
        }
        catch (ProgressDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new ProgressDataException("The local learning-progress file is missing, damaged, or unavailable.", exception);
        }
    }

    private async Task SaveAsync(LearningProgressData data, CancellationToken cancellationToken)
    {
        try
        {
            await dataStore.WriteAsync(FileName, JsonSerializer.Serialize(data, jsonOptions), cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ProgressDataException("Unable to save local learning progress.", exception);
        }
    }

    private LearningProgressData Clone(LearningProgressData data) =>
        JsonSerializer.Deserialize<LearningProgressData>(JsonSerializer.Serialize(data, jsonOptions), jsonOptions)!;
}
