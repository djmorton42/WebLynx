using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebLynx.Models;
using WebLynx.Services;

namespace WebLynx.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaceController : ControllerBase
{
    private readonly ILogger<RaceController> _logger;
    private readonly RaceStateManager _raceStateManager;

    public RaceController(ILogger<RaceController> logger, RaceStateManager raceStateManager)
    {
        _logger = logger;
        _raceStateManager = raceStateManager;
    }

    [HttpGet("current")]
    public ActionResult<RaceData> GetCurrentRace()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            return Ok(raceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current race data");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("event")]
    public ActionResult<RaceEvent?> GetEvent()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            return Ok(raceData.Event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event data");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("racers")]
    public ActionResult<List<Racer>> GetRacers()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            return Ok(raceData.Racers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting racers data");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("racers/{lane}")]
    public ActionResult<Racer?> GetRacerByLane(int lane)
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            var racer = raceData.Racers.FirstOrDefault(r => r.Lane == lane);
            return Ok(racer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting racer data for lane {Lane}", lane);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("status")]
    public ActionResult<object> GetRaceStatus()
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            return Ok(new
            {
                Status = raceData.Status.ToString(),
                CurrentTime = raceData.CurrentTime?.ToString(@"mm\:ss\.f"),
                LastUpdated = raceData.LastUpdated,
                RacerCount = raceData.Racers.Count,
                EventName = raceData.Event?.EventName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting race status");
            return StatusCode(500, "Internal server error");
        }
    }



}
