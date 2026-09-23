namespace BibleRecallTrainerV2.Services;

public interface ILocalDataStore
{
    Task<string?> ReadAsync(string fileName, CancellationToken cancellationToken = default);
    Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default);
}
