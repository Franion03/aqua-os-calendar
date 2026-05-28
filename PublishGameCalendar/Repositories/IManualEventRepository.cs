using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Repositories;

public interface IManualEventRepository
{
    Task<List<ManualEventEntity>> GetBySeriesIdAsync(string seriesId);
    Task<ManualEventEntity?> GetByIdAsync(string seriesId, string eventUid);
    Task<ManualEventEntity> CreateAsync(ManualEventEntity entity);
    Task UpdateAsync(ManualEventEntity entity);
    Task DeleteAsync(string seriesId, string eventUid);
}