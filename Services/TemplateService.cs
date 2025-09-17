using WebLynx.Models;
using Microsoft.Extensions.Options;

namespace WebLynx.Services;

public class TemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly string _templatePath;
    private readonly BroadcastSettings _broadcastSettings;

    public TemplateService(ILogger<TemplateService> logger, IOptions<BroadcastSettings> broadcastSettings)
    {
        _logger = logger;
        _templatePath = Path.Combine("Views", "in_race_livestream", "template.html");
        _broadcastSettings = broadcastSettings.Value;
    }

    public string ProcessLivestreamTemplate(RaceData raceData)
    {
        try
        {
            if (!File.Exists(_templatePath))
            {
                _logger.LogError("Template file not found: {TemplatePath}", _templatePath);
                return GenerateFallbackHtml(raceData);
            }

            var template = File.ReadAllText(_templatePath);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing livestream template");
            return GenerateFallbackHtml(raceData);
        }
    }

    public string ProcessLapCounterTemplate(RaceData raceData)
    {
        try
        {
            var lapCounterTemplatePath = Path.Combine("Views", "lap_counter", "template.html");
            if (!File.Exists(lapCounterTemplatePath))
            {
                _logger.LogError("Lap counter template file not found: {TemplatePath}", lapCounterTemplatePath);
                return GenerateLapCounterFallbackHtml(raceData);
            }

            var template = File.ReadAllText(lapCounterTemplatePath);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing lap counter template");
            return GenerateLapCounterFallbackHtml(raceData);
        }
    }

    public string ProcessBroadcastResultsTemplate(RaceData raceData)
    {
        try
        {
            var broadcastResultsTemplatePath = Path.Combine("Views", "broadcast_results", "template.html");
            if (!File.Exists(broadcastResultsTemplatePath))
            {
                _logger.LogError("Broadcast results template file not found: {TemplatePath}", broadcastResultsTemplatePath);
                return GenerateBroadcastResultsFallbackHtml(raceData);
            }

            var template = File.ReadAllText(broadcastResultsTemplatePath);
            
            // Replace configuration placeholders with actual values
            template = template.Replace("{{MEET_TITLE}}", _broadcastSettings.MeetTitle);
            template = template.Replace("{{EVENT_SUBTITLE}}", _broadcastSettings.EventSubtitle);
            template = template.Replace("{{UNOFFICIAL_RESULTS_TEXT}}", _broadcastSettings.UnofficialResultsText);
            
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing broadcast results template");
            return GenerateBroadcastResultsFallbackHtml(raceData);
        }
    }

    public string ProcessAnnouncementsTemplate(RaceData raceData)
    {
        try
        {
            var announcementsTemplatePath = Path.Combine("Views", "announcements", "template.html");
            if (!File.Exists(announcementsTemplatePath))
            {
                _logger.LogError("Announcements template file not found: {TemplatePath}", announcementsTemplatePath);
                return GenerateAnnouncementsFallbackHtml(raceData);
            }

            var template = File.ReadAllText(announcementsTemplatePath);
            
            // Replace configuration placeholders with actual values
            template = template.Replace("{{MEET_TITLE}}", _broadcastSettings.MeetTitle);
            template = template.Replace("{{EVENT_SUBTITLE}}", _broadcastSettings.EventSubtitle);
            
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing announcements template");
            return GenerateAnnouncementsFallbackHtml(raceData);
        }
    }

    public string ProcessBroadcastRaceOverlayTemplate(RaceData raceData)
    {
        try
        {
            var broadcastRaceOverlayTemplatePath = Path.Combine("Views", "broadcast_race_overlay", "template.html");
            if (!File.Exists(broadcastRaceOverlayTemplatePath))
            {
                _logger.LogError("Broadcast race overlay template file not found: {TemplatePath}", broadcastRaceOverlayTemplatePath);
                return GenerateBroadcastRaceOverlayFallbackHtml(raceData);
            }

            var template = File.ReadAllText(broadcastRaceOverlayTemplatePath);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing broadcast race overlay template");
            return GenerateBroadcastRaceOverlayFallbackHtml(raceData);
        }
    }


    public List<Racer> SortRacers(List<Racer> racers, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "place" => racers.OrderBy(r => r.Place > 0 ? r.Place : int.MaxValue).ThenBy(r => r.Lane).ToList(),
            "lane" => racers.OrderBy(r => r.Lane).ToList(),
            _ => racers.OrderBy(r => r.Place > 0 ? r.Place : int.MaxValue).ThenBy(r => r.Lane).ToList()
        };
    }



    private string GenerateFallbackHtml(RaceData raceData)
    {
        return $@"<!DOCTYPE html>
<html>
  <head>
    <link rel=""stylesheet"" href=""/views/in_race_livestream/styles.css"">
  </head>
  <body>
    <div class=""race-clock"">{raceData.CurrentTime?.ToString(@"mm\:ss\.f") ?? "--:--.-"}</div>
    <div class=""racer-grid"">
      <div class=""racer-row"">
        <div class=""racer-card"">
          <div class=""position"">-</div>
          <div class=""color-indicator"" style=""background-color: #333;""></div>
          <div class=""racer-info"">
            <div class=""racer-name"">No Race Data</div>
            <div class=""affiliation"">-</div>
          </div>
          <div class=""time"">--:--.---</div>
          <div class=""lap-count"">-</div>
        </div>
      </div>
    </div>
    <script>
      // Simple auto-refresh every 250ms
      setInterval(() => {{
        window.location.reload();
      }}, 250);
    </script>
  </body>
</html>";
    }

    private string GenerateLapCounterFallbackHtml(RaceData raceData)
    {
        return $@"<!DOCTYPE html>
<html>
  <head>
    <link rel=""stylesheet"" href=""/views/lap_counter/styles.css"">
  </head>
  <body>
    <div class=""lap-counter-grid"">
      <div class=""lap-counter-row"">
        <div class=""lap-counter-card"">
          <div class=""lane-number"">-</div>
          <div class=""skater-name"">No Race Data</div>
          <div class=""laps-remaining"">-</div>
        </div>
      </div>
    </div>
    <script>
      // Simple auto-refresh every 250ms
      setInterval(() => {{
        window.location.reload();
      }}, 250);
    </script>
  </body>
</html>";
    }

    private string GenerateBroadcastResultsFallbackHtml(RaceData raceData)
    {
        return $@"<!DOCTYPE html>
<html>
  <head>
    <link rel=""stylesheet"" href=""/views/broadcast_results/styles.css"">
  </head>
  <body>
    <div class=""broadcast-overlay"">
      <div class=""header-section"">
        <div class=""meet-title"">{_broadcastSettings.MeetTitle}</div>
        <div class=""event-subtitle"">{_broadcastSettings.EventSubtitle}</div>
        <div class=""unofficial-indicator"">{_broadcastSettings.UnofficialResultsText}</div>
      </div>
      <div class=""race-details-bar"">No Race Data</div>
      <div class=""results-container"">
        <div class=""result-row"">
          <div class=""position"">-</div>
          <div class=""racer-info"">
            <div class=""racer-name"">No Results Available</div>
            <div class=""affiliation"">-</div>
          </div>
          <div class=""lane-color"" style=""background-color: #333;""></div>
          <div class=""final-time"">--:--.---</div>
        </div>
      </div>
    </div>
    <script>
      // Simple auto-refresh every 1000ms
      setInterval(() => {{
        window.location.reload();
      }}, 1000);
    </script>
  </body>
</html>";
    }

    private string GenerateAnnouncementsFallbackHtml(RaceData raceData)
    {
        return $@"<!DOCTYPE html>
<html>
  <head>
    <link rel=""stylesheet"" href=""/views/announcements/styles.css"">
  </head>
  <body>
    <div class=""announcement-overlay"">
      <div class=""header-section"">
        <div class=""meet-info"">
          <div class=""meet-title"">{_broadcastSettings.MeetTitle}</div>
          <div class=""event-subtitle"">{_broadcastSettings.EventSubtitle}</div>
        </div>
        <div class=""sso-logo"">
          <img src=""/views/announcements/sso-logo.png"" alt=""SSO Logo"" />
        </div>
      </div>
      <div class=""announcement-content"">
        <div class=""announcement-message"" style=""display: none;"">
        </div>
      </div>
    </div>
    <script>
      // Simple auto-refresh every 2000ms
      setInterval(() => {{
        window.location.reload();
      }}, 2000);
    </script>
  </body>
</html>";
    }

    private string GenerateBroadcastRaceOverlayFallbackHtml(RaceData raceData)
    {
        var currentTime = raceData.CurrentTime?.ToString(@"mm\:ss\.f") ?? "--:--.-";
        return $@"<!DOCTYPE html>
<html>
  <head>
    <link rel=""stylesheet"" href=""/views/broadcast_race_overlay/styles.css"">
  </head>
  <body>
    <div class=""sso-logo"">
      <img src=""/views/broadcast_race_overlay/sso-logo.png"" alt=""SSO Logo"">
    </div>
    <div class=""race-clock"">{currentTime}</div>
    <div class=""event-name"">Event Name</div>
    <div class=""racer-stack"">
      <div class=""racer-block"">
        <div class=""lane-color"" style=""background-color: #333;""></div>
        <div class=""racer-info"">
          <div class=""racer-name"">No Race Data</div>
          <div class=""affiliation"">-</div>
        </div>
        <div class=""time"">--:--.---</div>
        <div class=""lap-count"">-</div>
      </div>
    </div>
    <script>
      // Simple auto-refresh every 250ms
      setInterval(() => {{
        window.location.reload();
      }}, 250);
    </script>
  </body>
</html>";
    }
}

