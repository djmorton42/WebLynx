using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebLynx.Services;

namespace WebLynx.Controllers;

[Controller]
public class ViewsController : Controller
{
    private readonly ILogger<ViewsController> _logger;
    private readonly RaceStateManager _raceStateManager;
    private readonly TemplateService _templateService;

    public ViewsController(ILogger<ViewsController> logger, RaceStateManager raceStateManager, TemplateService templateService)
    {
        _logger = logger;
        _raceStateManager = raceStateManager;
        _templateService = templateService;
    }

    [HttpGet("views/in_race_livestream")]
    public IActionResult GetInRaceLivestream()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            var html = _templateService.ProcessLivestreamTemplate(raceData);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating livestream view");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("views/lap_counter")]
    public IActionResult GetLapCounter()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            var html = _templateService.ProcessLapCounterTemplate(raceData);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lap counter view");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("views/broadcast_results")]
    public IActionResult GetBroadcastResults()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            var html = _templateService.ProcessBroadcastResultsTemplate(raceData);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating broadcast results view");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("views/announcements")]
    public IActionResult GetAnnouncements()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            var html = _templateService.ProcessAnnouncementsTemplate(raceData);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating announcements view");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("views/broadcast_race_overlay")]
    public IActionResult GetBroadcastRaceOverlay()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            var html = _templateService.ProcessBroadcastRaceOverlayTemplate(raceData);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating broadcast race overlay view");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("views")]
    public IActionResult GetViewsIndex()
    {
        var html = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>WebLynx Views</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }
        .container {
            background-color: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            text-align: center;
            margin-bottom: 30px;
        }
        .views-list {
            list-style: none;
            padding: 0;
        }
        .views-list li {
            margin: 15px 0;
        }
        .views-list a {
            display: block;
            padding: 15px 20px;
            background-color: #007bff;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            transition: background-color 0.3s;
        }
        .views-list a:hover {
            background-color: #0056b3;
        }
        .description {
            color: #666;
            font-size: 14px;
            margin-top: 5px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>WebLynx Views</h1>
        <ul class=""views-list"">
            <li>
                <a href=""/views/in_race_livestream"">In-Race Livestream</a>
                <div class=""description"">Live race streaming view with real-time updates</div>
            </li>
            <li>
                <a href=""/views/lap_counter"">Lap Counter</a>
                <div class=""description"">Lap counting display for race tracking</div>
            </li>
            <li>
                <a href=""/views/broadcast_results"">Broadcast Results</a>
                <div class=""description"">Results display for broadcast purposes</div>
            </li>
            <li>
                <a href=""/views/announcements"">Announcements</a>
                <div class=""description"">Announcements and sponsor information display</div>
            </li>
            <li>
                <a href=""/views/broadcast_race_overlay"">Broadcast Race Overlay</a>
                <div class=""description"">Clean broadcast overlay with compact racer information and race clock</div>
            </li>
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
}

