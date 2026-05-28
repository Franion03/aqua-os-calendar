using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Services.Pollers;

public interface IWebsitePoller
{
    Task<List<Event>> FetchEventsAsync(Series series);
}