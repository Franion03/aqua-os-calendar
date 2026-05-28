using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AquaOs.Calendar.Domain;
using AquaOs.Calendar.DTOs;
using AquaOs.Calendar.Repositories;
using AquaOs.Calendar.Services.GoogleCalendar;
using AquaOs.Calendar.Services.Ics;

namespace AquaOs.Calendar.Controllers;

[ApiController]
[Route("api/calendar")]
public class SeriesController : ControllerBase
{
    private readonly IIcsService _icsService;
    private readonly ISeriesRepository _seriesRepo;
    private readonly IManualEventRepository _manualEventRepo;
    private readonly IGoogleCalendarService _googleCalendarService;

    public SeriesController(
        ISeriesRepository seriesRepo,
        IIcsService icsService,
        IManualEventRepository manualEventRepo,
        IGoogleCalendarService googleCalendarService)
    {
        _seriesRepo = seriesRepo;
        _icsService = icsService;
        _manualEventRepo = manualEventRepo;
        _googleCalendarService = googleCalendarService;
    }

    // ── Calendar Sources ──────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<List<SeriesDto>>> GetAll()
    {
        List<Series> series = await _seriesRepo.GetAllAsync();
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        List<SeriesDto> dtos = series.Select(s => new SeriesDto
        {
            Id = s.Id,
            Name = s.Name,
            IcsUrl = $"{baseUrl}/api/calendar/{s.Id}/calendar.ics",
            GoogleCalendarUrl = s.GoogleCalendarId is not null
                ? _googleCalendarService.GetShareUrl(s.GoogleCalendarId)
                : null,
            Enabled = s.Enabled,
            LastPolledAt = s.PollingConfig?.LastPolledAt,
            LastChangeAt = s.PollingConfig?.LastChangeAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}/calendar.ics")]
    public async Task<IActionResult> GetIcsFile(string id)
    {
        Series? series = await _seriesRepo.GetByIdAsync(id);
        if (series is null) return NotFound();

        string path = _icsService.GetIcsFilePath(id);
        if (!System.IO.File.Exists(path))
            return NotFound("No calendar file available yet for this series.");

        return PhysicalFile(path, "text/calendar", $"{series.Name}.ics");
    }

    // ── Events (merged: polled + manual) ──────────────────────────

    /// <summary>
    /// Returns all events for a series — both polled (from .ics) and manual (from DB).
    /// Add ?upcoming=true to filter to future events only (used by CrewAI agents).
    /// </summary>
    [HttpGet("{seriesId}/events")]
    public async Task<ActionResult<List<ManualEventDto>>> GetEvents(string seriesId, [FromQuery] bool upcoming = false)
    {
        Series? series = await _seriesRepo.GetByIdAsync(seriesId);
        if (series is null) return NotFound();

        // Polled events from .ics
        List<Event> polledEvents = await _icsService.ParseAsync(seriesId);

        // Manual events from DB
        List<ManualEventEntity> manualEntities = await _manualEventRepo.GetBySeriesIdAsync(seriesId);
        List<ManualEventDto> dtos = manualEntities.Select(MapToDto).ToList();

        // Merge polled events as DTOs (no category for polled events)
        foreach (Event ev in polledEvents)
        {
            dtos.Add(new ManualEventDto
            {
                Uid = ev.Uid,
                SeriesId = seriesId,
                Title = ev.Title,
                Start = ev.Start,
                End = ev.End,
                Location = ev.Location,
                Description = ev.Description,
                Category = "polled"
            });
        }

        // Sort by start date
        dtos = dtos.OrderBy(e => e.Start).ToList();

        if (upcoming)
        {
            DateTime now = DateTime.UtcNow;
            dtos = dtos.Where(e => e.Start >= now).ToList();
        }

        return Ok(dtos);
    }

    // ── Manual Event CRUD (protected) ─────────────────────────────

    [HttpPost("{seriesId}/events")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ManualEventDto>> CreateEvent(string seriesId, [FromBody] CreateManualEventRequest request)
    {
        Series? series = await _seriesRepo.GetByIdAsync(seriesId);
        if (series is null) return NotFound();

        string uid = Guid.NewGuid().ToString();
        ManualEventEntity entity = new ManualEventEntity
        {
            SeriesId = seriesId,
            Uid = uid,
            Title = request.Title,
            Start = request.Start,
            End = request.End,
            Location = request.Location,
            Description = request.Description,
            Category = request.Category
        };

        ManualEventEntity created = await _manualEventRepo.CreateAsync(entity);

        // Regenerate .ics to include the new manual event
        await RegenerateIcsAsync(seriesId);

        return CreatedAtAction(nameof(GetEvents), new { seriesId }, MapToDto(created));
    }

    [HttpPut("{seriesId}/events/{eventUid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEvent(string seriesId, string eventUid, [FromBody] UpdateManualEventRequest request)
    {
        ManualEventEntity? entity = await _manualEventRepo.GetByIdAsync(seriesId, eventUid);
        if (entity is null) return NotFound();

        entity.Title = request.Title;
        entity.Start = request.Start;
        entity.End = request.End;
        entity.Location = request.Location;
        entity.Description = request.Description;
        entity.Category = request.Category;

        await _manualEventRepo.UpdateAsync(entity);
        await RegenerateIcsAsync(seriesId);

        return Ok(MapToDto(entity));
    }

    [HttpDelete("{seriesId}/events/{eventUid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEvent(string seriesId, string eventUid)
    {
        ManualEventEntity? entity = await _manualEventRepo.GetByIdAsync(seriesId, eventUid);
        if (entity is null) return NotFound();

        await _manualEventRepo.DeleteAsync(seriesId, eventUid);
        await RegenerateIcsAsync(seriesId);

        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────

    private async Task RegenerateIcsAsync(string seriesId)
    {
        List<Event> polledEvents = await _icsService.ParseAsync(seriesId);
        List<ManualEventEntity> manualEntities = await _manualEventRepo.GetBySeriesIdAsync(seriesId);
        List<Event> manualEvents = manualEntities.Select(e => e.ToDomainEvent()).ToList();
        await _icsService.WriteAsync(seriesId, polledEvents, manualEvents);
    }

    private static ManualEventDto MapToDto(ManualEventEntity entity)
    {
        return new ManualEventDto
        {
            Uid = entity.Uid,
            SeriesId = entity.SeriesId,
            Title = entity.Title,
            Start = entity.Start,
            End = entity.End,
            Location = entity.Location,
            Description = entity.Description,
            Category = entity.Category,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}