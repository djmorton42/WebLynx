using WebLynx.Models;

namespace WebLynx.Services;

public class TemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly string _templatePath;

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
        _templatePath = Path.Combine("Views", "in_race_livestream", "template.html");
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
}

