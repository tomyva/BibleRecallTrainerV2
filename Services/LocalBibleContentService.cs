using System.Collections.Concurrent;
using System.Text.Json;
using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public sealed class LocalBibleContentService : IBibleContentService
{
    private const string ManifestFile = "syllabus.json";
    private readonly IBibleAssetReader assetReader;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, Lazy<Task<BibleBook>>> bookCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<Task<BibleSyllabusManifest>> manifest;

    public LocalBibleContentService(IBibleAssetReader assetReader)
    {
        this.assetReader = assetReader;
        manifest = new Lazy<Task<BibleSyllabusManifest>>(() => LoadManifestAsync(CancellationToken.None));
    }

    public async Task<IReadOnlyList<BibleBook>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        var syllabus = await manifest.Value.WaitAsync(cancellationToken);
        var books = new List<BibleBook>(syllabus.Books.Count);
        foreach (var entry in syllabus.Books)
            books.Add(await GetBookAsync(entry, cancellationToken));

        return books;
    }

    public async Task<IReadOnlyList<BibleChapter>> GetChaptersAsync(string bookId, CancellationToken cancellationToken = default) =>
        (await FindBookAsync(bookId, cancellationToken)).Chapters;

    public async Task<BibleChapter> GetChapterAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default)
    {
        var book = await FindBookAsync(bookId, cancellationToken);
        return book.Chapters.FirstOrDefault(chapter => chapter.ChapterNumber == chapterNumber)
            ?? throw new BibleContentException($"Chapter {chapterNumber} is not included for {book.DisplayName}.");
    }

    public Task<BibleChapterReference?> GetPreviousChapterAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default) =>
        GetAdjacentChapterAsync(bookId, chapterNumber, -1, cancellationToken);

    public Task<BibleChapterReference?> GetNextChapterAsync(string bookId, int chapterNumber, CancellationToken cancellationToken = default) =>
        GetAdjacentChapterAsync(bookId, chapterNumber, 1, cancellationToken);

    private async Task<BibleBook> FindBookAsync(string bookId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            throw new BibleContentException("A book identifier is required.");

        var syllabus = await manifest.Value.WaitAsync(cancellationToken);
        var entry = syllabus.Books.FirstOrDefault(book => string.Equals(book.Id, bookId, StringComparison.OrdinalIgnoreCase))
            ?? throw new BibleContentException($"Book '{bookId}' is not included in the syllabus.");
        return await GetBookAsync(entry, cancellationToken);
    }

    private async Task<BibleChapterReference?> GetAdjacentChapterAsync(
        string bookId,
        int chapterNumber,
        int offset,
        CancellationToken cancellationToken)
    {
        var books = await GetBooksAsync(cancellationToken);
        var chapters = books.SelectMany(book => book.Chapters.Select(chapter =>
            new BibleChapterReference(book.Id, chapter.ChapterNumber))).ToList();
        var index = chapters.FindIndex(reference =>
            string.Equals(reference.BookId, bookId, StringComparison.OrdinalIgnoreCase) &&
            reference.ChapterNumber == chapterNumber);

        if (index < 0)
            throw new BibleContentException($"Chapter '{bookId} {chapterNumber}' is not included in the syllabus.");

        var adjacentIndex = index + offset;
        return adjacentIndex >= 0 && adjacentIndex < chapters.Count ? chapters[adjacentIndex] : null;
    }

    private Task<BibleBook> GetBookAsync(BibleSyllabusBook entry, CancellationToken cancellationToken)
    {
        var lazyBook = bookCache.GetOrAdd(entry.Id, _ =>
            new Lazy<Task<BibleBook>>(() => LoadBookAsync(entry, CancellationToken.None)));
        return lazyBook.Value.WaitAsync(cancellationToken);
    }

    private async Task<BibleSyllabusManifest> LoadManifestAsync(CancellationToken cancellationToken)
    {
        var syllabus = await DeserializeAsync<BibleSyllabusManifest>(ManifestFile, cancellationToken);
        if (syllabus.Books.Count == 0)
            throw new BibleContentException("The Bible syllabus does not contain any books.");
        if (syllabus.Books.Select(book => book.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != syllabus.Books.Count)
            throw new BibleContentException("The Bible syllabus contains duplicate book identifiers.");
        return syllabus;
    }

    private async Task<BibleBook> LoadBookAsync(BibleSyllabusBook entry, CancellationToken cancellationToken)
    {
        var book = await DeserializeAsync<BibleBook>(entry.File, cancellationToken);
        if (!string.Equals(book.Id, entry.Id, StringComparison.OrdinalIgnoreCase))
            throw new BibleContentException($"Bible data file '{entry.File}' has an unexpected book identifier.");
        if (book.Chapters.Count != entry.ExpectedChapterCount)
            throw new BibleContentException($"{book.DisplayName} should contain {entry.ExpectedChapterCount} syllabus chapters but contains {book.Chapters.Count}.");

        var expectedChapter = 1;
        foreach (var chapter in book.Chapters)
        {
            if (!string.Equals(chapter.BookId, book.Id, StringComparison.OrdinalIgnoreCase) || chapter.ChapterNumber != expectedChapter)
                throw new BibleContentException($"{book.DisplayName} has invalid or unordered chapter data near chapter {expectedChapter}.");
            if (chapter.Verses.Count == 0)
                throw new BibleContentException($"{book.DisplayName} {chapter.ChapterNumber} does not contain verses.");

            var expectedVerse = 1;
            foreach (var verse in chapter.Verses)
            {
                if (verse.VerseNumber != expectedVerse || string.IsNullOrWhiteSpace(verse.Text))
                    throw new BibleContentException($"{book.DisplayName} {chapter.ChapterNumber} has invalid or unordered verse data near verse {expectedVerse}.");
                expectedVerse++;
            }
            expectedChapter++;
        }
        return book;
    }

    private async Task<T> DeserializeAsync<T>(string fileName, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await assetReader.OpenAsync(fileName, cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, jsonOptions, cancellationToken)
                ?? throw new BibleContentException($"Bible data file '{fileName}' is empty.");
        }
        catch (BibleContentException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new BibleContentException($"Unable to load Bible data file '{fileName}'. The bundled content may be missing or malformed.", exception);
        }
    }
}
