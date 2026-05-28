using System.ComponentModel.DataAnnotations;

namespace AquaOs.Calendar.DTOs;

public class UpdatePollingConfigRequest
{
    [Range(1, 8760)] public int IntervalHours { get; set; }

    public bool Enabled { get; set; }
}