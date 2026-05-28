using System.ComponentModel.DataAnnotations;

namespace AquaOs.Calendar.DTOs;

public class UpdateSeriesRequest
{
    [Required] public string Name { get; set; } = string.Empty;

    [Required] [Url] public string SourceUrl { get; set; } = string.Empty;

    [Required] public string PollerType { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    /// <summary>Optional Google Calendar ID (email) to sync events to.</summary>
    public string? GoogleCalendarId { get; set; }
}