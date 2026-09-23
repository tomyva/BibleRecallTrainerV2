namespace BibleRecallTrainerV2.Services;

public interface IProgressExportService
{
    Task<string> CreateJsonAsync(CancellationToken cancellationToken = default);
}
