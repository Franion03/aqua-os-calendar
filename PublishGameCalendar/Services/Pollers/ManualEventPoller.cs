using AquaOs.Calendar.Domain;
using AquaOs.Calendar.Repositories;

namespace AquaOs.Calendar.Services.Pollers;

/// <summary>
/// Returns events stored in DynamoDB (manual events) instead of scraping a website.
/// Used when poller_type = "ManualEventPoller".
/// </summary>
public class ManualEventPoller : IWebsitePoller
{
    private readonly IManualEventRepository _repo;

    public ManualEventPoller(IManualEventRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<Event>> FetchEventsAsync(Series series)
    {
        List<ManualEventEntity> entities = await _repo.GetBySeriesIdAsync(series.Id);
        return entities.Select(e => e.ToDomainEvent()).ToList();
    }
}