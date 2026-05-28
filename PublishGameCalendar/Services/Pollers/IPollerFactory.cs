namespace AquaOs.Calendar.Services.Pollers;

public interface IPollerFactory
{
    IWebsitePoller Create(string pollerType);
}
