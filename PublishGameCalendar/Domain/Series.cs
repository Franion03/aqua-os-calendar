using Amazon.DynamoDBv2.DataModel;

namespace AquaOs.Calendar.Domain;

[DynamoDBTable("series")]
public class Series
{
    [DynamoDBHashKey("id")]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty("name")]
    public string Name { get; set; } = string.Empty;

    [DynamoDBProperty("source_url")]
    public string SourceUrl { get; set; } = string.Empty;

    [DynamoDBProperty("poller_type")]
    public string PollerType { get; set; } = string.Empty;

    [DynamoDBProperty("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional Google Calendar ID to sync events to.
    /// When set, events are mirrored to this Google Calendar via the API
    /// in addition to the .ics file.
    /// Format: calendar-email@group.calendar.google.com
    /// </summary>
    [DynamoDBProperty("google_calendar_id")]
    public string? GoogleCalendarId { get; set; }

    [DynamoDBProperty("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    [DynamoDBIgnore]
    public PollingConfig? PollingConfig { get; set; }
}
