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

    public string ProcessTemplate(RaceData raceData, string viewName)
    {
        try
        {
            var templatePath = Path.Combine("Views", viewName, "template.html");
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Template file not found: {TemplatePath}", templatePath);
                return GenerateFallbackHtml(raceData, viewName);
            }

            var template = File.ReadAllText(templatePath);
            
            // Apply common template replacements
            template = ApplyCommonReplacements(template);
            
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template for view {ViewName}", viewName);
            return GenerateFallbackHtml(raceData, viewName);
        }
    }

    private string ApplyCommonReplacements(string template)
    {
        // Replace configuration placeholders with actual values
        template = template.Replace("{{MEET_TITLE}}", _broadcastSettings.MeetTitle);
        template = template.Replace("{{EVENT_SUBTITLE}}", _broadcastSettings.EventSubtitle);
        template = template.Replace("{{UNOFFICIAL_RESULTS_TEXT}}", _broadcastSettings.UnofficialResultsText);
        
        return template;
    }

    private string GenerateFallbackHtml(RaceData raceData, string viewName)
    {
        var stylesPath = $"/views/{viewName}/styles.css";
        var currentTime = raceData.CurrentTime?.ToString(@"mm\:ss\.f") ?? "--:--.-";
        
        return $@"<!DOCTYPE html>
<html>
  <head>
    <link rel=""stylesheet"" href=""{stylesPath}"">
  </head>
  <body>
    <div class=""race-clock"">{currentTime}</div>
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

    public string ProcessLivestreamTemplate(RaceData raceData)
    {
        return ProcessTemplate(raceData, "in_race_livestream");
    }

    public string ProcessLapCounterTemplate(RaceData raceData)
    {
        return ProcessTemplate(raceData, "lap_counter");
    }

    public string ProcessBroadcastResultsTemplate(RaceData raceData)
    {
        return ProcessTemplate(raceData, "broadcast_results");
    }

    public string ProcessAnnouncementsTemplate(RaceData raceData)
    {
        return ProcessTemplate(raceData, "announcements");
    }

    public string ProcessBroadcastRaceOverlayTemplate(RaceData raceData)
    {
        return ProcessTemplate(raceData, "broadcast_race_overlay");
    }


    public List<Racer> SortRacers(List<Racer> racers, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "place" => racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList(),
            "lane" => racers.OrderBy(r => r.Lane).ToList(),
            _ => racers.OrderBy(r => r.Place).ThenBy(r => r.Lane).ToList()
        };
    }



}

