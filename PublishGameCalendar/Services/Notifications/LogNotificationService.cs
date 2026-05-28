using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Services.Notifications;

/// <summary>
/// Fallback notification service that logs changes to the console.
/// Used when no webhook is configured.
/// </summary>
public class LogNotificationService : INotificationService
{
    private readonly ILogger<LogNotificationService> _logger;

    public LogNotificationService(ILogger<LogNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyChangeAsync(string seriesName, EventDiff diff, string seriesId)
    {
        if (!diff.HasChanges) return Task.CompletedTask;

        _logger.LogInformation(
            "📅 Calendar Change — {Series}: {Summary}",
            seriesName, diff.BuildSummary());

        foreach (Event added in diff.Added)
            _logger.LogInformation("  + {Title} @ {Start}", added.Title, added.Start);

        foreach (Event removed in diff.Removed)
            _logger.LogInformation("  - {Title}", removed.Title);

        foreach (Event modified in diff.Modified)
            _logger.LogInformation("  ~ {Title}", modified.Title);

        return Task.CompletedTask;
    }
}