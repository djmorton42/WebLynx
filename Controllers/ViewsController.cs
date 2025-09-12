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

    [HttpGet("views/in_race_livestream/events")]
    public async Task GetInRaceLivestreamEvents()
    {
        _logger.LogInformation("SSE connection started from {RemoteIpAddress}", HttpContext.Connection.RemoteIpAddress);
        
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["Access-Control-Allow-Origin"] = "*";

        var tcs = new TaskCompletionSource<bool>();
        var cancellationToken = HttpContext.RequestAborted;

        // Subscribe to race updates
        void OnRaceUpdated(object? sender, Models.RaceData raceData)
        {
            _logger.LogDebug("Race update received, processing SSE update");
            _ = Task.Run(async () =>
            {
                try
                {
                    var html = _templateService.ProcessLivestreamTemplate(raceData);
                    // Extract just the body content for SSE
                    var bodyStart = html.IndexOf("<body>") + 6;
                    var bodyEnd = html.IndexOf("</body>");
                    if (bodyStart > 5 && bodyEnd > bodyStart)
                    {
                        var bodyContent = html.Substring(bodyStart, bodyEnd - bodyStart);
                        var data = $"data: {bodyContent}\n\n";
                        await Response.WriteAsync(data, cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                        _logger.LogDebug("Sent SSE update for race data");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending SSE update");
                    tcs.SetResult(true);
                }
            });
        }

        _raceStateManager.RaceUpdated += OnRaceUpdated;

        // Send initial data
        var initialRaceData = _raceStateManager.GetCurrentRaceState();
        var initialHtml = _templateService.ProcessLivestreamTemplate(initialRaceData);
        var bodyStart = initialHtml.IndexOf("<body>") + 6;
        var bodyEnd = initialHtml.IndexOf("</body>");
        if (bodyStart > 5 && bodyEnd > bodyStart)
        {
            var bodyContent = initialHtml.Substring(bodyStart, bodyEnd - bodyStart);
            var initialData = $"data: {bodyContent}\n\n";
            await Response.WriteAsync(initialData, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        // Wait for cancellation
        cancellationToken.Register(() => {
            _logger.LogInformation("SSE connection cancelled from {RemoteIpAddress}", HttpContext.Connection.RemoteIpAddress);
            tcs.SetResult(true);
        });
        
        try
        {
            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection cancelled due to application shutdown from {RemoteIpAddress}", HttpContext.Connection.RemoteIpAddress);
        }
        finally
        {
            _raceStateManager.RaceUpdated -= OnRaceUpdated;
            _logger.LogInformation("SSE connection ended from {RemoteIpAddress}", HttpContext.Connection.RemoteIpAddress);
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

