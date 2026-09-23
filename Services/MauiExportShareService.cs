using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace BibleRecallTrainerV2.Services;

public sealed class MauiExportShareService : IExportShareService
{
    public async Task ShareAsync(string json, CancellationToken cancellationToken = default)
    {
        var fileName = $"bible-recall-progress-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(path, json, cancellationToken);
        await Share.Default.RequestAsync(new ShareFileRequest("Export Bible Recall Trainer progress", new ShareFile(path)));
    }
}
