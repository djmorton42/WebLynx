using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebLynx.Models;
using WebLynx.Services;

namespace WebLynx.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaceController : ControllerBase
{
    private readonly ILogger<RaceController> _logger;
    private readonly RaceStateManager _raceStateManager;
    private readonly TemplateService _templateService;
    private readonly LapCounterSettings _lapCounterSettings;

    public RaceController(ILogger<RaceController> logger, RaceStateManager raceStateManager, TemplateService templateService, IOptions<LapCounterSettings> lapCounterSettings)
    {
        _logger = logger;
        _raceStateManager = raceStateManager;
        _templateService = templateService;
        _lapCounterSettings = lapCounterSettings.Value;
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


    [HttpGet("race-data")]
    public ActionResult<RaceDataApiResponse> GetRaceData([FromQuery] string sortBy = "place")
    {
        try
        {
            var raceData = _raceStateManager.GetCurrentRaceState();
            var sortedRacers = _templateService.SortRacers(raceData.Racers, sortBy);
            
            var response = new RaceDataApiResponse
            {
                CurrentTime = raceData.CurrentTime,
                Event = raceData.Event,
                Status = raceData.Status,
                LastUpdated = raceData.LastUpdated,
                AnnouncementMessage = raceData.AnnouncementMessage,
                HalfLapModeEnabled = _lapCounterSettings.HalfLapModeEnabled,
                Racers = sortedRacers.Select(r => new RacerApiResponse
                {
                    Lane = r.Lane,
                    Id = r.Id,
                    Name = r.Name,
                    Affiliation = r.Affiliation,
                    PlaceText = r.Place.PlaceText,
                    HasPlaceData = r.Place.HasPlaceData,
                    ReactionTime = r.ReactionTime,
                    CumulativeSplitTime = r.CumulativeSplitTime,
                    LastSplitTime = r.LastSplitTime,
                    BestSplitTime = r.BestSplitTime,
                    LapsRemaining = r.LapsRemaining,
                    DelayedLapsRemaining = r.GetDelayedLapsRemaining(_lapCounterSettings.DelayedDisplaySeconds),
                    LapCountLastChanged = r.LapCountLastChanged,
                    Speed = r.Speed,
                    Pace = r.Pace,
                    FinalTime = r.FinalTime,
                    DeltaTime = r.DeltaTime,
                    HasFinished = r.HasFinished,
                    HasFirstCrossing = r.CumulativeSplitTime.HasValue || r.LastSplitTime.HasValue
                }).ToList()
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting race data for API");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("test-announcement")]
    public IActionResult TestAnnouncement([FromBody] TestAnnouncementRequest request)
    {
        try
        {
            // Convert the message to UTF-16 bytes (like the real system would send)
            var messageBytes = System.Text.Encoding.Unicode.GetBytes(request.Message);
            _raceStateManager.ProcessMessageAsync(messageBytes, "Test Announcement");
            
            return Ok("Test announcement processed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing test announcement");
            return StatusCode(500, "Internal server error");
        }
    }

    public class TestAnnouncementRequest
    {
        public string Message { get; set; } = string.Empty;
    }

}
