using System.Text.Json;
using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class LocalSpacedRepetitionService(ILocalDataStore dataStore, TimeProvider timeProvider) : ISpacedRepetitionService
{
    private const string FileName = "review-schedule.json";
    private readonly SemaphoreSlim accessGate = new(1, 1);
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private ReviewScheduleData? cache;

    public async Task RecordResultAsync(
        string bookId,
        int chapterNumber,
        int percentage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId) || chapterNumber <= 0)
            throw new ArgumentException("A valid chapter reference is required.");

        await accessGate.WaitAsync(cancellationToken);
        try
        {
            var data = await LoadAsync(cancellationToken);
            var review = data.Reviews.FirstOrDefault(item =>
                string.Equals(item.BookId, bookId, StringComparison.OrdinalIgnoreCase) && item.ChapterNumber == chapterNumber);
            var now = timeProvider.GetUtcNow();
            if (review is null)
            {
                review = new ChapterReviewSchedule { BookId = bookId, ChapterNumber = chapterNumber };
                data.Reviews.Add(review);
            }

            var score = Math.Clamp(percentage, 0, 100);
            review.IntervalDays = score switch
            {
                >= 90 => Math.Max(3, review.IntervalDays * 2),
                >= 70 => Math.Max(1, review.IntervalDays + 1),
                _ => 1
            };
            review.SuccessfulReviews = score >= 70 ? review.SuccessfulReviews + 1 : 0;
            review.LastPercentage = score;
            review.LastReviewedUtc = now;
            review.NextReviewUtc = now.Date.AddDays(review.IntervalDays);
            await SaveAsync(data, cancellationToken);
        }
        finally
        {
            accessGate.Release();
        }
    }

    public async Task<IReadOnlyList<ChapterReviewSchedule>> GetDueReviewsAsync(CancellationToken cancellationToken = default)
    {
        var schedule = await GetScheduleAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        return schedule.Where(review => review.NextReviewUtc <= now)
            .OrderBy(review => review.NextReviewUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<ChapterReviewSchedule>> GetScheduleAsync(CancellationToken cancellationToken = default)
    {
        await accessGate.WaitAsync(cancellationToken);
        try
        {
            var data = await LoadAsync(cancellationToken);
            return data.Reviews.Select(Clone).OrderBy(review => review.NextReviewUtc).ToList();
        }
        finally
        {
            accessGate.Release();
        }
    }

    private async Task<ReviewScheduleData> LoadAsync(CancellationToken cancellationToken)
    {
        if (cache is not null)
            return cache;
        try
        {
            var json = await dataStore.ReadAsync(FileName, cancellationToken);
            cache = string.IsNullOrWhiteSpace(json)
                ? new ReviewScheduleData()
                : JsonSerializer.Deserialize<ReviewScheduleData>(json, jsonOptions)
                  ?? throw new ProgressDataException("The local review schedule is empty.");
            if (cache.SchemaVersion != 1)
                throw new ProgressDataException($"Unsupported review-schedule schema version {cache.SchemaVersion}.");
            return cache;
        }
        catch (ProgressDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new ProgressDataException("The local review schedule is damaged or unavailable.", exception);
        }
    }

    private async Task SaveAsync(ReviewScheduleData data, CancellationToken cancellationToken)
    {
        try
        {
            await dataStore.WriteAsync(FileName, JsonSerializer.Serialize(data, jsonOptions), cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ProgressDataException("Unable to save the local review schedule.", exception);
        }
    }

    private static ChapterReviewSchedule Clone(ChapterReviewSchedule review) => new()
    {
        BookId = review.BookId,
        ChapterNumber = review.ChapterNumber,
        IntervalDays = review.IntervalDays,
        SuccessfulReviews = review.SuccessfulReviews,
        LastPercentage = review.LastPercentage,
        LastReviewedUtc = review.LastReviewedUtc,
        NextReviewUtc = review.NextReviewUtc
    };
}
