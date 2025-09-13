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


    [HttpGet("test-race-update")]
    public IActionResult TestRaceUpdate()
    {
        // Manually trigger the race update event by processing a test message
        _ = Task.Run(async () =>
        {
            await _raceStateManager.ProcessMessageAsync(new byte[0], "Test Update");
        });
        
        return Ok("Test race update triggered");
    }
}

