using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Services.GoogleCalendar;

/// <summary>
/// Syncs calendar events to a Google Calendar.
/// Uses a Google service account for server-to-server auth.
/// </summary>
public interface IGoogleCalendarService
{
    /// <summary>
    /// Mirror the given events into the target Google Calendar.
    /// Creates new events, updates changed ones, and deletes removed ones.
    /// </summary>
    /// <param name="googleCalendarId">The Google Calendar ID (email address of the calendar).</param>
    /// <param name="events">The full set of events that should be present.</param>
    /// <param name="seriesId">Series identifier (used for extended property matching).</param>
    /// <returns>The number of created, updated, and deleted events.</returns>
    Task<(int created, int updated, int deleted)> SyncAsync(
        string googleCalendarId, List<Event> events, string seriesId);

    /// <summary>
    /// Returns the public share URL for a Google Calendar.
    /// </summary>
    string GetShareUrl(string googleCalendarId);
}