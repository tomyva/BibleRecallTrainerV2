namespace BibleRecallTrainerV2.Models;

public sealed class BibleSyllabusManifest
{
    public string Translation { get; init; } = string.Empty;
    public string Abbreviation { get; init; } = string.Empty;
    public string Edition { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string SourceArchive { get; init; } = string.Empty;
    public string License { get; init; } = string.Empty;
    public IReadOnlyList<BibleSyllabusBook> Books { get; init; } = [];
}

public sealed class BibleSyllabusBook
{
    public string Id { get; init; } = string.Empty;
    public string File { get; init; } = string.Empty;
    public int ExpectedChapterCount { get; init; }
}
