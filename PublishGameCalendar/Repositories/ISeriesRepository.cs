using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Repositories;

public interface ISeriesRepository
{
    Task<List<Series>> GetAllAsync();
    Task<Series?> GetByIdAsync(string id);
    Task<Series> CreateAsync(Series series);
    Task UpdateAsync(Series series);
    Task DeleteAsync(string id);
}
