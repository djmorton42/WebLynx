using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebLynx.Models;

namespace WebLynx.Services;

public class LiveRaceFileWriter : BackgroundService
{
    private readonly ILogger<LiveRaceFileWriter> _logger;
    private readonly RaceStateManager _raceStateManager;
    private readonly string _outputFilePath;
    private readonly LoggingSettings _loggingSettings;

    public LiveRaceFileWriter(ILogger<LiveRaceFileWriter> logger, RaceStateManager raceStateManager, IOptions<LoggingSettings> loggingSettings)
    {
        _logger = logger;
        _raceStateManager = raceStateManager;
        _loggingSettings = loggingSettings.Value;
        
        // Create date-based filename
        var dateString = DateTime.Now.ToString("yyyy-MM-dd");
        var fileName = $"live_race_info.{dateString}.txt";
        _outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log", fileName);
        
        // Ensure the log directory exists
        var logDir = Path.GetDirectoryName(_outputFilePath);
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir!);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Live race file writer started. Writing to: {FilePath}", _outputFilePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WriteLiveRaceInfo();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing live race info to file");
                // Continue running even if there's an error
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Live race file writer stopped");
    }

    private async Task WriteLiveRaceInfo()
    {
        // Check if live race info logging is enabled
        if (!_loggingSettings.EnableLiveRaceInfoLogging)
        {
            return;
        }

        var raceData = _raceStateManager.GetCurrentRaceState();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var content = new StringBuilder();
        content.AppendLine($"=== Live Race Info - {timestamp} ===");
        
        // Add elapsed time
        if (raceData.CurrentTime.HasValue)
        {
            content.AppendLine($"Elapsed Time: {raceData.CurrentTime.Value:mm\\:ss\\.f}");
        }
        else
        {
            content.AppendLine("Elapsed Time: --:--.-");
        }

        // Add race status
        content.AppendLine($"Race Status: {raceData.Status}");
        
        // Add event name if available
        if (raceData.Event != null)
        {
            content.AppendLine($"Event: {raceData.Event.EventName}");
        }

        content.AppendLine();

        // Add racer standings
        if (raceData.Racers.Any())
        {
            content.AppendLine($"## Number of racers ##: {raceData.Racers.Count}");

            content.AppendLine("Current Standings:");
            content.AppendLine("Lane | Place | Last Split | Final Time | Laps Remaining | Name");
            content.AppendLine("-----|-------|------------|------------|----------------|-----");

            // Sort racers by place (1st, 2nd, 3rd, etc.)
            var sortedRacers = raceData.Racers
                .Where(r => r.Place > 0) // Only show racers with a place
                .OrderBy(r => r.Place)
                .ToList();

            // Add racers with places
            foreach (var racer in sortedRacers)
            {
                var lastSplit = racer.LastSplitTime?.ToString(@"mm\:ss\.fff") ?? "--:--.-";
                var finalTime = racer.FinalTime?.ToString(@"mm\:ss\.fff") ?? "--:--.-";
                var name = string.IsNullOrWhiteSpace(racer.Name) ? $"Racer {racer.Lane}" : racer.Name;
                
                content.AppendLine($"{racer.Lane,4} | {racer.Place,5} | {lastSplit,10} | {finalTime,10} | {racer.LapsRemaining,14} | {name}");
            }

            // Add racers without places (not yet started or no place assigned)
            var unplacedRacers = raceData.Racers
                .Where(r => r.Place <= 0)
                .OrderBy(r => r.Lane)
                .ToList();

            if (unplacedRacers.Any())
            {
                content.AppendLine();
                content.AppendLine("Unplaced Racers:");
                foreach (var racer in unplacedRacers)
                {
                    var lastSplit = racer.LastSplitTime?.ToString(@"mm\:ss\.f") ?? "--:--.-";
                    var finalTime = racer.FinalTime?.ToString(@"mm\:ss\.f") ?? "--:--.-";
                    var name = string.IsNullOrWhiteSpace(racer.Name) ? $"Racer {racer.Lane}" : racer.Name;
                    
                    content.AppendLine($"{racer.Lane,4} |   -- | {lastSplit,10} | {finalTime,10} | {racer.LapsRemaining,14} | {name}");
                }
            }
        }
        else
        {
            content.AppendLine("No racers in current race");
        }

        content.AppendLine();
        content.AppendLine("==========================================");
        content.AppendLine();

        // Write to file (append mode)
        await File.AppendAllTextAsync(_outputFilePath, content.ToString());
    }
}
