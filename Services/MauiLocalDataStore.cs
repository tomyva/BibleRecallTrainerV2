namespace BibleRecallTrainerV2.Services;

public sealed class MauiLocalDataStore : ILocalDataStore
{
    public async Task<string?> ReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);
        return File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : null;
    }

    public async Task WriteAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var path = GetPath(fileName);
        var temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, content, cancellationToken);
        File.Move(temporaryPath, path, true);
    }

    private static string GetPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
            throw new ArgumentException("A simple local file name is required.", nameof(fileName));
        return Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
    }
}
