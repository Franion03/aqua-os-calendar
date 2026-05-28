using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Services.Notifications;

/// <summary>
/// Dispatches notifications when calendar events change.
/// Implementations can route through webhooks, email, SMS, etc.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Notify subscribers that a calendar series has changed.
    /// </summary>
    /// <param name="seriesName">Human-readable series name.</param>
    /// <param name="diff">The diff describing what changed.</param>
    /// <param name="seriesId">The series identifier (used for deeplinks).</param>
    Task NotifyChangeAsync(string seriesName, EventDiff diff, string seriesId);
}