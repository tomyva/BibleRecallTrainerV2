namespace BibleRecallTrainerV2.Services;

public interface IExportShareService
{
    Task ShareAsync(string json, CancellationToken cancellationToken = default);
}
