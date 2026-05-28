using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Services.Pollers;

/// <summary>
/// Stub poller for water polo league websites. Extend this with actual HTML scraping
/// logic for specific federation or league pages (e.g., RFEN, LEN, local league tables).
/// </summary>
public class WaterPoloLeaguePoller : IWebsitePoller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WaterPoloLeaguePoller> _logger;

    public WaterPoloLeaguePoller(IHttpClientFactory httpClientFactory, ILogger<WaterPoloLeaguePoller> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<Event>> FetchEventsAsync(Series series)
    {
        _logger.LogInformation("WaterPoloLeaguePoller: fetching from {Url}", series.SourceUrl);

        HttpClient client = _httpClientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(series.SourceUrl);
        response.EnsureSuccessStatusCode();

        // ── Stub: parse league fixtures from HTML ──
        // TODO: Implement actual HTML scraping for the target league website.
        // Use AngleSharp to parse the page and extract match rows:
        //
        //   var html = await response.Content.ReadAsStringAsync();
        //   var document = await AngleSharp.Html.Parser.HtmlParser.ParseAsync(html);
        //   var rows = document.QuerySelectorAll(".fixture-row");
        //   foreach (var row in rows) { ... }
        //
        // For now, return an empty list so the poller doesn't break the pipeline.
        _logger.LogWarning("WaterPoloLeaguePoller is a stub — returning 0 events. Implement scraping for {Url}", series.SourceUrl);

        return new List<Event>();
    }
}