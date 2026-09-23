using System.Text.Json;
using BibleRecallTrainerV2.Models;
using Microsoft.Maui.Storage;

namespace BibleRecallTrainerV2.Services;

public sealed class ReadingPositionStore(IPreferences preferences) : IReadingPositionStore
{
    private const string PositionsKey = "reading.positions.v1";
    private readonly Lock sync = new();

    public ReadingPosition? Get(string bookId, int chapterNumber)
    {
        lock (sync)
        {
            return Load().FirstOrDefault(item =>
                string.Equals(item.BookId, bookId, StringComparison.OrdinalIgnoreCase) &&
                item.ChapterNumber == chapterNumber);
        }
    }

    public IReadOnlyList<ReadingPosition> GetAll()
    {
        lock (sync)
            return Load().ToList();
    }

    public void Save(string bookId, int chapterNumber, int verseNumber)
    {
        if (string.IsNullOrWhiteSpace(bookId) || chapterNumber <= 0 || verseNumber <= 0)
            return;

        lock (sync)
        {
            var positions = Load();
            positions.RemoveAll(item =>
                string.Equals(item.BookId, bookId, StringComparison.OrdinalIgnoreCase) &&
                item.ChapterNumber == chapterNumber);
            positions.Add(new ReadingPosition(bookId, chapterNumber, verseNumber));
            preferences.Set(PositionsKey, JsonSerializer.Serialize(positions));
        }
    }

    public void ResetAll()
    {
        lock (sync)
            preferences.Remove(PositionsKey);
    }

    private List<ReadingPosition> Load()
    {
        var json = preferences.Get(PositionsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ReadingPosition>>(json) ?? [];
        }
        catch (JsonException)
        {
            preferences.Remove(PositionsKey);
            return [];
        }
    }
}
