using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IReadingPositionStore
{
    ReadingPosition? Get(string bookId, int chapterNumber);
    IReadOnlyList<ReadingPosition> GetAll();
    void Save(string bookId, int chapterNumber, int verseNumber);
    void ResetAll();
}
