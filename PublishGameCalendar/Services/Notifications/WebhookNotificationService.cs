using System.Text;
using System.Text.Json;
using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Services.Notifications;

/// <summary>
/// Posts change notifications to the aqua-os-crew webhook endpoint,
/// which dispatches through Telegram, WhatsApp, and email channels.
/// </summary>
public class WebhookNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookNotificationService> _logger;
    private readonly string _webhookUrl;

    public WebhookNotificationService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<WebhookNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _webhookUrl = config["Notifications:WebhookUrl"]
                      ?? config["CREW_SERVICE_URL"] + "/notify/calendar-change"
                      ?? "http://aqua-os-crew:8001/notify/calendar-change";
    }

    public async Task NotifyChangeAsync(string seriesName, EventDiff diff, string seriesId)
    {
        if (!diff.HasChanges) return;

        var payload = new
        {
            series_name = seriesName,
            series_id = seriesId,
            summary = diff.BuildSummary(),
            added = diff.Added.Select(MapEvent),
            removed = diff.Removed.Select(MapEvent),
            modified = diff.Modified.Select(MapEvent),
            timestamp = DateTime.UtcNow.ToString("O")
        };

        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        try
        {
            StringContent content = new(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(_webhookUrl, content);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation(
                "Notification dispatched for series '{Name}': {Summary}",
                seriesName, diff.BuildSummary());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to dispatch notification for series '{Name}'. CrewAI service may be unavailable.",
                seriesName);
        }
    }

    private static object MapEvent(Event ev) => new
    {
        title = ev.Title,
        start = ev.Start.ToString("O"),
        end = ev.End.ToString("O"),
        location = ev.Location,
        description = ev.Description
    };
}