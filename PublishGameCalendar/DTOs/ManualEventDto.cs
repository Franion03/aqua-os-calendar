namespace AquaOs.Calendar.DTOs;

public class CreateManualEventRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    /// <summary>"match", "training", "tournament", "team_event", "other"</summary>
    public string Category { get; set; } = "other";
}

public class UpdateManualEventRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = "other";
}

public class ManualEventDto
{
    public string Uid { get; set; } = string.Empty;
    public string SeriesId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = "other";
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
