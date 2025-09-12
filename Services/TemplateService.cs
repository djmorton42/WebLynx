using WebLynx.Models;

namespace WebLynx.Services;

public class TemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly string _templatePath;
    private readonly string _cssPath;

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
        _templatePath = Path.Combine("Views", "in_race_livestream", "template.html");
        _cssPath = Path.Combine("Views", "in_race_livestream", "styles.css");
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
            
            // Generate race clock
            var raceClock = raceData.CurrentTime?.ToString(@"mm\:ss\.f") ?? "--:--.-";
            
            // Generate racer rows
            var racerRows = GenerateRacerRows(raceData);
            
            // Replace template variables
            var processedHtml = template
                .Replace("{{RACE_CLOCK}}", raceClock)
                .Replace("{{RACER_ROWS}}", racerRows);

            return processedHtml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing livestream template");
            return GenerateFallbackHtml(raceData);
        }
    }

    private string GenerateRacerRows(RaceData raceData)
    {
        var laneColors = new Dictionary<int, string>
        {
            { 1, "#ffff00" }, // Yellow
            { 2, "#000000" }, // Black
            { 3, "#00ffff" }, // Light Blue
            { 4, "#ff0000" }, // Red
            { 5, "#ffffff" }, // White
            { 6, "#000080" }, // Navy Blue
            { 7, "#808080" }, // Grey
            { 8, "#ffc0cb" }, // Pink
            { 9, "#ffa500" }, // Orange
            { 10, "#008000" } // Green
        };

        var racers = raceData.Racers.OrderBy(r => r.Lane).ToList();
        var racerRows = new List<string>();

        // Group racers into rows of 4
        for (int i = 0; i < racers.Count; i += 4)
        {
            var rowRacers = racers.Skip(i).Take(4).ToList();
            var racerCards = new List<string>();

            foreach (var racer in rowRacers)
            {
                var laneColor = laneColors.GetValueOrDefault(racer.Lane, "#333333");
                var cumulativeTime = racer.CumulativeSplitTime?.ToString(@"mm\:ss\.fff") ?? "--:--.---";
                var lapsRemaining = racer.LapsRemaining.ToString();

                racerCards.Add($@"
                <div class=""racer-card"">
                  <div class=""position"">{racer.Place}</div>
                  <div class=""color-indicator"" style=""background-color: {laneColor};""></div>
                  <div class=""racer-info"">
                    <div class=""racer-name"">{racer.Name}</div>
                    <div class=""affiliation"">{racer.Affiliation}</div>
                  </div>
                  <div class=""time"">{cumulativeTime}</div>
                  <div class=""lap-count"">{lapsRemaining}</div>
                </div>");
            }

            // Pad with empty cards if needed
            while (racerCards.Count < 4)
            {
                racerCards.Add(@"
                <div class=""racer-card"">
                  <div class=""position"">-</div>
                  <div class=""color-indicator"" style=""background-color: #333;""></div>
                  <div class=""racer-info"">
                    <div class=""racer-name"">-</div>
                    <div class=""affiliation"">-</div>
                  </div>
                  <div class=""time"">--:--.---</div>
                  <div class=""lap-count"">-</div>
                </div>");
            }

            racerRows.Add($@"
            <div class=""racer-row"">
              {string.Join("", racerCards)}
            </div>");
        }

        return string.Join("", racerRows);
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
  </body>
</html>";
    }
}

