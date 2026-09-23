namespace BibleRecallTrainerV2.Services;

public sealed class MauiBibleAssetReader : IBibleAssetReader
{
    public Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default) =>
        FileSystem.Current.OpenAppPackageFileAsync($"Bible/{relativePath}");
}
