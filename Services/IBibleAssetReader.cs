namespace BibleRecallTrainerV2.Services;

public interface IBibleAssetReader
{
    Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default);
}
