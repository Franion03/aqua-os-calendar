using AquaOs.Calendar.Domain;
using AquaOs.Calendar.Repositories;
using AquaOs.Calendar.Services.GoogleCalendar;
using AquaOs.Calendar.Services.Ics;
using AquaOs.Calendar.Services.Notifications;
using AquaOs.Calendar.Services.Pollers;

namespace AquaOs.Calendar.Services.Orchestrator;

public class OrchestratorService : BackgroundService
{
    private readonly ILogger<OrchestratorService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrchestratorService(IServiceScopeFactory scopeFactory, ILogger<OrchestratorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PollDueSeriesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task PollDueSeriesAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IPollingConfigRepository pollingConfigRepo =
            scope.ServiceProvider.GetRequiredService<IPollingConfigRepository>();
        IIcsService icsService = scope.ServiceProvider.GetRequiredService<IIcsService>();
        IPollerFactory pollerFactory = scope.ServiceProvider.GetRequiredService<IPollerFactory>();
        IManualEventRepository manualEventRepo =
            scope.ServiceProvider.GetRequiredService<IManualEventRepository>();
        INotificationService notificationService =
            scope.ServiceProvider.GetRequiredService<INotificationService>();
        IGoogleCalendarService googleCalendarService =
            scope.ServiceProvider.GetRequiredService<IGoogleCalendarService>();

        List<PollingConfig> configs = await pollingConfigRepo.GetAllEnabledAsync();
        DateTime now = DateTime.UtcNow;

        foreach (PollingConfig config in configs)
        {
            if (!IsDue(config, now)) continue;

            await PollSeriesAsync(config, pollingConfigRepo, icsService, pollerFactory, manualEventRepo, notificationService, googleCalendarService, now, ct);
        }
    }

    private static bool IsDue(PollingConfig config, DateTime now)
    {
        return !config.LastPolledAt.HasValue ||
               config.LastPolledAt.Value.AddHours(config.IntervalHours) <= now;
    }

    private async Task PollSeriesAsync(
        PollingConfig config,
        IPollingConfigRepository pollingConfigRepo,
        IIcsService icsService,
        IPollerFactory pollerFactory,
        IManualEventRepository manualEventRepo,
        INotificationService notificationService,
        IGoogleCalendarService googleCalendarService,
        DateTime now,
        CancellationToken ct)
    {
        Series series = config.Series;
        _logger.LogInformation("Polling series '{Name}' (id={Id})", series.Name, series.Id);

        try
        {
            IWebsitePoller poller = pollerFactory.Create(series.PollerType);
            List<Event> freshEvents = await poller.FetchEventsAsync(series);
            _logger.LogInformation("Fetched {Count} events for series '{Name}'", freshEvents.Count, series.Name);

            // Load manual events to merge with polled events
            List<ManualEventEntity> manualEntities = await manualEventRepo.GetBySeriesIdAsync(series.Id);
            List<Event> manualEvents = manualEntities.Select(e => e.ToDomainEvent()).ToList();

            bool isManualPoller = series.PollerType == nameof(ManualEventPoller);

            if (freshEvents.Count == 0 && !isManualPoller)
            {
                _logger.LogError(
                    "Poller returned 0 events for series '{Name}' (id={Id}) — skipping update to prevent data loss. " +
                    "This may indicate a website restructuring.", series.Name, series.Id);
                config.LastPolledAt = now;
                config.LastPollFailed = true;
                config.LastEventCount = 0;
                await pollingConfigRepo.UpdateAsync(config);
                return;
            }

            EventDiff diff = await icsService.DiffAsync(series.Id, freshEvents);

            config.LastPolledAt = now;
            config.LastPollFailed = false;
            config.LastEventCount = freshEvents.Count;

            if (diff.HasChanges || manualEvents.Count > 0)
            {
                await icsService.WriteAsync(series.Id, freshEvents, manualEvents);
                config.LastChangeAt = now;

                // Dispatch notifications for polled event changes
                if (diff.HasChanges)
                {
                    _ = notificationService.NotifyChangeAsync(series.Name, diff, series.Id);
                }

                // Sync to Google Calendar (if configured)
                if (!string.IsNullOrWhiteSpace(series.GoogleCalendarId))
                {
                    List<Event> mergedEvents = new(freshEvents);
                    mergedEvents.AddRange(manualEvents);
                    _ = googleCalendarService.SyncAsync(series.GoogleCalendarId, mergedEvents, series.Id);
                }
            }

            await pollingConfigRepo.UpdateAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling series '{Name}' (id={Id})", series.Name, series.Id);
            config.LastPolledAt = now;
            config.LastPollFailed = true;
            try { await pollingConfigRepo.UpdateAsync(config); }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to persist poll failure for series '{Name}' (id={Id})", series.Name, series.Id);
            }
        }
    }
}
