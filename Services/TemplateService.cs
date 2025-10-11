using WebLynx.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace WebLynx.Services;

public class TemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly string _templatePath;
    private readonly BroadcastSettings _broadcastSettings;
    private readonly ViewProperties _viewProperties;

    public TemplateService(ILogger<TemplateService> logger, IOptions<BroadcastSettings> broadcastSettings, ViewProperties viewProperties)
    {
        _logger = logger;
        _templatePath = Path.Combine("Views", "in_race_livestream", "template.html");
        _broadcastSettings = broadcastSettings.Value;
        _viewProperties = viewProperties;
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
        
        // Inject view properties as JavaScript configuration
        template = InjectViewProperties(template);
        
        return template;
    }

    private string InjectViewProperties(string template)
    {
        try
        {
            // Convert Properties dictionary to camelCase for JavaScript
            var camelCaseProperties = ConvertToCamelCase(_viewProperties.Properties);

            // Serialize with camelCase property names
            var viewConfigJson = JsonSerializer.Serialize(camelCaseProperties, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Create the JavaScript configuration with dynamic properties
            var viewConfigScript = $@"
      <script>
        // View Properties Configuration - Dynamically loaded from appsettings.json
        const VIEW_CONFIG = {viewConfigJson};
        
        // Helper functions for backward compatibility
        function getLaneColor(lane) {{
          return VIEW_CONFIG.laneColors?.[lane] || VIEW_CONFIG.defaultLaneColor || '#333333';
        }}
        
        function getStrokeColor(lane) {{
          return VIEW_CONFIG.laneStrokeColors?.[lane] || VIEW_CONFIG.defaultStrokeColor || '#ffffff';
        }}
      </script>";

            // Replace the placeholder or inject before the closing head tag
            if (template.Contains("{{VIEW_PROPERTIES}}"))
            {
                template = template.Replace("{{VIEW_PROPERTIES}}", viewConfigScript);
            }
            else
            {
                // Inject before the closing head tag, or before the first script tag
                var headCloseIndex = template.IndexOf("</head>");
                if (headCloseIndex > 0)
                {
                    template = template.Insert(headCloseIndex, viewConfigScript);
                }
                else
                {
                    // If no head tag, inject at the beginning of the body
                    var bodyOpenIndex = template.IndexOf("<body>");
                    if (bodyOpenIndex > 0)
                    {
                        var bodyCloseIndex = template.IndexOf(">", bodyOpenIndex) + 1;
                        template = template.Insert(bodyCloseIndex, viewConfigScript);
                    }
                }
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error injecting view properties into template");
            return template;
        }
    }

    private static object ConvertToCamelCase(object value)
    {
        if (value is Dictionary<string, object> dict)
        {
            var camelCaseDict = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                var camelCaseKey = ToCamelCase(kvp.Key);
                camelCaseDict[camelCaseKey] = ConvertToCamelCase(kvp.Value);
            }
            return camelCaseDict;
        }
        return value;
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            return input;

        return char.ToLowerInvariant(input[0]) + input.Substring(1);
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

