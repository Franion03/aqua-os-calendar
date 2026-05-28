using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Services.GoogleCalendar;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly ServiceAccountCredential? _credential;
    private readonly string? _serviceAccountEmail;

    public GoogleCalendarService(IConfiguration config, ILogger<GoogleCalendarService> logger)
    {
        _logger = logger;

        string? credsPath = config["Google:ServiceAccountKeyPath"];
        if (string.IsNullOrWhiteSpace(credsPath) || !File.Exists(credsPath))
        {
            _logger.LogWarning("Google Calendar: no service account key found at '{Path}'. Google Calendar sync disabled.", credsPath);
            return;
        }

        try
        {
            using var stream = new FileStream(credsPath, FileMode.Open, FileAccess.Read);
            _credential = ServiceAccountCredential.FromServiceAccountData(stream);
            _serviceAccountEmail = _credential.Id;
            _logger.LogInformation("Google Calendar: service account loaded ({Email})", _serviceAccountEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Calendar: failed to load service account key. Sync disabled.");
        }
    }

    /// <inheritdoc/>
    public async Task<(int created, int updated, int deleted)> SyncAsync(
        string googleCalendarId, List<Event> events, string seriesId)
    {
        if (_credential == null)
            return (0, 0, 0);

        CalendarService service = CreateService();

        // Fetch existing Google Calendar events for this series
        List<Google.Apis.Calendar.v3.Data.Event> existing = await ListSeriesEventsAsync(service, googleCalendarId, seriesId);

        // Index by UID (stored in extended property)
        Dictionary<string, Google.Apis.Calendar.v3.Data.Event> existingByUid = new();
        foreach (var ge in existing)
        {
            string? uid = ge.ExtendedProperties?.Private?["uid"];
            if (uid != null) existingByUid[uid] = ge;
        }

        Dictionary<string, Event> freshByUid = events.ToDictionary(e => e.Uid);

        int created = 0, updated = 0, deleted = 0;

        // Create or update
        foreach (Event ev in events)
        {
            if (existingByUid.TryGetValue(ev.Uid, out var ge))
            {
                if (EventChanged(ge, ev))
                {
                    await UpdateEventAsync(service, googleCalendarId, ge, ev, seriesId);
                    updated++;
                }
            }
            else
            {
                await CreateEventAsync(service, googleCalendarId, ev, seriesId);
                created++;
            }
        }

        // Delete removed events
        foreach (var (uid, ge) in existingByUid)
        {
            if (!freshByUid.ContainsKey(uid))
            {
                await DeleteEventAsync(service, googleCalendarId, ge);
                deleted++;
            }
        }

        if (created > 0 || updated > 0 || deleted > 0)
            _logger.LogInformation(
                "Google Calendar sync → {CalId}: +{Created} ~{Updated} -{Deleted}",
                googleCalendarId, created, updated, deleted);

        return (created, updated, deleted);
    }

    /// <inheritdoc/>
    public string GetShareUrl(string googleCalendarId)
    {
        string encoded = Uri.EscapeDataString(googleCalendarId);
        return $"https://calendar.google.com/calendar/embed?src={encoded}";
    }

    // ── Private Helpers ──────────────────────────────────────────

    private CalendarService CreateService()
    {
        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "AquaOS-Calendar",
        });
    }

    private async Task<List<Google.Apis.Calendar.v3.Data.Event>> ListSeriesEventsAsync(
        CalendarService service, string calendarId, string seriesId)
    {
        List<Google.Apis.Calendar.v3.Data.Event> all = new();
        string? pageToken = null;

        do
        {
            var request = service.Events.List(calendarId);
            request.PrivateExtendedProperty = $"seriesId={seriesId}";
            request.PageToken = pageToken;
            request.MaxResults = 2500;

            Events response = await request.ExecuteAsync();
            if (response.Items != null)
                all.AddRange(response.Items);
            pageToken = response.NextPageToken;
        }
        while (pageToken != null);

        return all;
    }

    private async Task CreateEventAsync(
        CalendarService service, string calendarId, Event ev, string seriesId)
    {
        var ge = MapToGoogleEvent(ev, seriesId);
        var request = service.Events.Insert(ge, calendarId);
        await request.ExecuteAsync();
    }

    private async Task UpdateEventAsync(
        CalendarService service, string calendarId,
        Google.Apis.Calendar.v3.Data.Event existing, Event ev, string seriesId)
    {
        var ge = MapToGoogleEvent(ev, seriesId);
        var request = service.Events.Update(ge, calendarId, existing.Id);
        await request.ExecuteAsync();
    }

    private async Task DeleteEventAsync(
        CalendarService service, string calendarId, Google.Apis.Calendar.v3.Data.Event ge)
    {
        var request = service.Events.Delete(calendarId, ge.Id);
        await request.ExecuteAsync();
    }

    private static Google.Apis.Calendar.v3.Data.Event MapToGoogleEvent(Event ev, string seriesId)
    {
        return new Google.Apis.Calendar.v3.Data.Event
        {
            Summary = ev.Title,
            Description = ev.Description,
            Location = ev.Location,
            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(ev.Start, TimeSpan.Zero),
                TimeZone = "UTC",
            },
            End = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(ev.End, TimeSpan.Zero),
                TimeZone = "UTC",
            },
            ExtendedProperties = new Event.ExtendedPropertiesData
            {
                Private__ = new Dictionary<string, string>
                {
                    ["uid"] = ev.Uid,
                    ["seriesId"] = seriesId,
                }
            }
        };
    }

    private static bool EventChanged(Google.Apis.Calendar.v3.Data.Event ge, Event ev)
    {
        DateTime? geStart = ge.Start?.DateTimeDateTimeOffset?.UtcDateTime;
        DateTime? geEnd = ge.End?.DateTimeDateTimeOffset?.UtcDateTime;

        return ge.Summary != ev.Title
            || ge.Description != (ev.Description ?? "")
            || ge.Location != (ev.Location ?? "")
            || geStart != ev.Start
            || geEnd != ev.End;
    }
}