using BibleRecallTrainerV2.Models;

namespace BibleRecallTrainerV2.Services;

public interface IDataHealthService
{
    Task<DataHealthReport> CheckAsync(CancellationToken cancellationToken = default);
}
