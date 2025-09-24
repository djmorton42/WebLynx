using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebLynx.Models;

namespace WebLynx.Services;

public class LiveRaceFileWriter : BackgroundService, IDisposable
{
    private readonly ILogger<LiveRaceFileWriter> _logger;
    private readonly RaceStateManager _raceStateManager;
    private readonly string _outputFilePath;
    private readonly LoggingSettings _loggingSettings;
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
    private readonly TimeSpan _semaphoreTimeout = TimeSpan.FromSeconds(5);
    private volatile bool _loggingDisabled = false;
    private int _consecutiveFailures = 0;
    private const int MaxConsecutiveFailures = 5;

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
        // Check if live race info logging is enabled or if we've disabled it due to failures
        if (!_loggingSettings.EnableLiveRaceInfoLogging || _loggingDisabled)
        {
            return;
        }

        // Try to acquire semaphore with timeout
        if (!await _fileSemaphore.WaitAsync(_semaphoreTimeout))
        {
            _logger.LogWarning("Timeout waiting for file access, skipping live race info update");
            return;
        }

        try
        {
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

            // Write to file with retry logic
            await WriteToFileWithRetryAsync(content.ToString());
            
            // Reset failure counter on success
            Interlocked.Exchange(ref _consecutiveFailures, 0);
        }
        catch (Exception ex)
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            _logger.LogError(ex, "Error writing live race info (failure #{Failures})", failures);
            
            // Disable logging if we have too many consecutive failures
            if (failures >= MaxConsecutiveFailures)
            {
                _loggingDisabled = true;
                _logger.LogError("Live race info logging disabled due to {Failures} consecutive failures", failures);
            }
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private async Task WriteToFileWithRetryAsync(string content)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await File.AppendAllTextAsync(_outputFilePath, content);
                return; // Success, exit the retry loop
            }
            catch (IOException ex) when (attempt < maxRetries - 1)
            {
                // Check if it's a file locking issue
                if (ex.Message.Contains("being used by another process") || 
                    ex.Message.Contains("The process cannot access the file"))
                {
                    var delay = baseDelayMs * (int)Math.Pow(2, attempt); // Exponential backoff
                    _logger.LogWarning("File access failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms: {Message}", 
                        attempt + 1, maxRetries, delay, ex.Message);
                    await Task.Delay(delay);
                }
                else
                {
                    // Not a file locking issue, rethrow immediately
                    throw;
                }
            }
        }

        // If we get here, all retries failed
        throw new IOException($"Failed to write to live race info file after {maxRetries} attempts: {_outputFilePath}");
    }

    public override void Dispose()
    {
        _fileSemaphore?.Dispose();
        base.Dispose();
    }
}

