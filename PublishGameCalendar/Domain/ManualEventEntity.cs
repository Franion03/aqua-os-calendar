using Amazon.DynamoDBv2.DataModel;

namespace AquaOs.Calendar.Domain;

/// <summary>
/// A manually-created event stored in DynamoDB. These are merged with polled events
/// when generating .ics files, so coaches can add training sessions, tournaments,
/// and other non-scraped events alongside auto-polled match fixtures.
/// </summary>
[DynamoDBTable("manual_events")]
public class ManualEventEntity
{
    /// <summary>Composite: "{seriesId}#{eventUid}"</summary>
    [DynamoDBHashKey("pk")]
    public string Pk { get; set; } = string.Empty;

    /// <summary>The series this event belongs to.</summary>
    [DynamoDBProperty("series_id")]
    public string SeriesId { get; set; } = string.Empty;

    /// <summary>Unique event identifier within the series.</summary>
    [DynamoDBProperty("uid")]
    public string Uid { get; set; } = string.Empty;

    [DynamoDBProperty("title")]
    public string Title { get; set; } = string.Empty;

    [DynamoDBProperty("start")]
    public DateTime Start { get; set; }

    [DynamoDBProperty("end")]
    public DateTime End { get; set; }

    [DynamoDBProperty("location")]
    public string? Location { get; set; }

    [DynamoDBProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Event category: "match", "training", "tournament", "team_event", "other".
    /// Used by CrewAI agents to distinguish match days from training sessions.
    /// </summary>
    [DynamoDBProperty("category")]
    public string Category { get; set; } = "other";

    [DynamoDBProperty("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    [DynamoDBProperty("updated_at")]
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    /// <summary>Convert to the domain Event model (used by IcsService merge).</summary>
    public Event ToDomainEvent()
    {
        return new Event
        {
            Uid = $"manual-{Uid}",
            Title = Title,
            Start = Start,
            End = End,
            Location = Location,
            Description = Description
        };
    }

    /// <summary>Convert from a domain Event model.</summary>
    public static ManualEventEntity FromDomainEvent(string seriesId, string uid, Event ev, string category = "other")
    {
        return new ManualEventEntity
        {
            Pk = $"{seriesId}#{uid}",
            SeriesId = seriesId,
            Uid = uid,
            Title = ev.Title,
            Start = ev.Start,
            End = ev.End,
            Location = ev.Location,
            Description = ev.Description,
            Category = category
        };
    }
}