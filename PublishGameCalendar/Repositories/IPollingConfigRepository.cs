using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Repositories;

public interface IPollingConfigRepository
{
    Task<List<PollingConfig>> GetAllAsync();
    Task<List<PollingConfig>> GetAllEnabledAsync();
    Task<PollingConfig?> GetBySeriesIdAsync(string seriesId);
    Task CreateAsync(PollingConfig config);
    Task UpdateAsync(PollingConfig config);
}
