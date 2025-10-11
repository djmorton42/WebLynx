using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebLynx.Services;
using WebLynx.Models;

namespace WebLynx.Controllers;

[Controller]
public class ViewsController : Controller
{
    private readonly ILogger<ViewsController> _logger;
    private readonly RaceStateManager _raceStateManager;
    private readonly TemplateService _templateService;
    private readonly ViewDiscoveryService _viewDiscoveryService;
    private readonly ViewProperties _viewProperties;

    public ViewsController(ILogger<ViewsController> logger, RaceStateManager raceStateManager, TemplateService templateService, ViewDiscoveryService viewDiscoveryService, ViewProperties viewProperties)
    {
        _logger = logger;
        _raceStateManager = raceStateManager;
        _templateService = templateService;
        _viewDiscoveryService = viewDiscoveryService;
        _viewProperties = viewProperties;
    }

    [HttpGet("views/{viewName}")]
    public IActionResult GetView(string viewName)
    {
        try
        {
            // Check if the view exists and is valid
            if (!_viewDiscoveryService.IsValidView(viewName))
            {
                _logger.LogWarning("Requested view not found or invalid: {ViewName}", viewName);
                return NotFound($"View '{viewName}' not found or invalid");
            }

            var raceData = _raceStateManager.GetCurrentRaceState();
            var html = _templateService.ProcessTemplate(raceData, viewName);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating view {ViewName}", viewName);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("views")]
    public IActionResult GetViewsIndex()
    {
        var discoveredViews = _viewDiscoveryService.DiscoveredViews.Where(v => v.IsValid).ToList();
        
        var viewsListHtml = string.Join("\n", discoveredViews.Select(view => {
            var descriptionHtml = !string.IsNullOrEmpty(view.Description) 
                ? $@"<div class=""description"">{view.Description}</div>" 
                : "";
            return $@"
            <li>
                <a href=""/views/{view.Name}"">{view.DisplayName}</a>
                {descriptionHtml}
            </li>";
        }));

        var html = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>WebLynx Views</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            background-color: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        h1 {{
            color: #333;
            text-align: center;
            margin-bottom: 30px;
        }}
        .views-list {{
            list-style: none;
            padding: 0;
        }}
        .views-list li {{
            margin: 15px 0;
        }}
        .views-list a {{
            display: block;
            padding: 15px 20px;
            background-color: #007bff;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            transition: background-color 0.3s;
        }}
        .views-list a:hover {{
            background-color: #0056b3;
        }}
        .description {{
            color: #666;
            font-size: 14px;
            margin-top: 5px;
        }}
        .no-views {{
            text-align: center;
            color: #666;
            font-style: italic;
            padding: 40px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>WebLynx Views</h1>
        <ul class=""views-list"">
            {(discoveredViews.Any() ? viewsListHtml : "<li class=\"no-views\">No valid views found. Create directories in the Views folder with template.html files.</li>")}
        </ul>
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }


    [HttpGet("test-race-update")]
    public IActionResult TestRaceUpdate()
    {
        // Manually trigger the race update event by processing a test message
        _raceStateManager.ProcessMessageAsync(new byte[0], "Test Update");
        
        return Ok("Test race update triggered");
    }

    [HttpGet("api/view-properties")]
    public IActionResult GetViewProperties()
    {
        try
        {
            return Ok(_viewProperties.Properties);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving view properties");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("api/view-properties/{propertyName}")]
    public IActionResult GetViewProperty(string propertyName)
    {
        try
        {
            if (!_viewProperties.HasProperty(propertyName))
            {
                return NotFound($"Property '{propertyName}' not found");
            }

            var value = _viewProperties.Properties[propertyName];
            return Ok(new { property = propertyName, value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving view property {PropertyName}", propertyName);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("api/views/refresh")]
    public IActionResult RefreshViews()
    {
        try
        {
            _logger.LogInformation("Refreshing view discovery...");
            _viewDiscoveryService.DiscoverViews();
            
            var discoveredViews = _viewDiscoveryService.DiscoveredViews.Where(v => v.IsValid).ToList();
            _logger.LogInformation("View refresh completed. Found {Count} valid views", discoveredViews.Count);
            
            return Ok(new { 
                message = "Views refreshed successfully", 
                viewCount = discoveredViews.Count,
                views = discoveredViews.Select(v => new { v.Name, v.DisplayName, v.Description })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing views");
            return StatusCode(500, "Internal server error");
        }
    }
}

