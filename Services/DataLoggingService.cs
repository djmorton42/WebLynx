using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using WebLynx.Models;

namespace WebLynx.Services;

public class DataLoggingService : IDisposable
{
    private readonly ILogger<DataLoggingService> _logger;
    private readonly string _logFilePath;
    private readonly LoggingSettings _loggingSettings;
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
    private readonly TimeSpan _semaphoreTimeout = TimeSpan.FromSeconds(5);
    private volatile bool _loggingDisabled = false;
    private int _consecutiveFailures = 0;
    private const int MaxConsecutiveFailures = 5;

    public DataLoggingService(ILogger<DataLoggingService> logger, IOptions<LoggingSettings> loggingSettings)
    {
        _logger = logger;
        _loggingSettings = loggingSettings.Value;
        
        // Create date-based filename
        var dateString = DateTime.Now.ToString("yyyy-MM-dd");
        var fileName = $"received_data.{dateString}.log";
        _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log", fileName);
        
        // Ensure the log directory exists
        var logDir = Path.GetDirectoryName(_logFilePath);
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir!);
        }
    }

    public void LogDataAsync(byte[] data, string clientInfo)
    {
        // Check if data logging is enabled or if we've disabled it due to failures
        if (!_loggingSettings.EnableDataLogging || _loggingDisabled)
        {
            return;
        }

        // Fire and forget - don't await this to prevent blocking the main application
        _ = Task.Run(async () =>
        {
            try
            {
                await LogDataInternalAsync(data, clientInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background logging for {ClientInfo}", clientInfo);
            }
        });
    }

    private async Task LogDataInternalAsync(byte[] data, string clientInfo)
    {
        // Try to acquire semaphore with timeout
        if (!await _fileSemaphore.WaitAsync(_semaphoreTimeout))
        {
            _logger.LogWarning("Timeout waiting for file access, skipping log entry for {ClientInfo}", clientInfo);
            return;
        }

        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = new StringBuilder();
            
            logEntry.AppendLine($"=== Data received at {timestamp} from {clientInfo} ===");
            logEntry.AppendLine($"Data length: {data.Length} bytes");
            
            // Log raw bytes in hex format for binary data
            logEntry.AppendLine("Raw data (hex):");
            for (int i = 0; i < data.Length; i += 16)
            {
                var line = new StringBuilder();
                line.Append($"{i:X4}: ");
                
                // Hex representation
                for (int j = 0; j < 16 && i + j < data.Length; j++)
                {
                    line.Append($"{data[i + j]:X2} ");
                }
                
                // Pad if needed
                while (line.Length < 60)
                {
                    line.Append(" ");
                }
                
                // ASCII representation (printable characters only)
                line.Append(" |");
                for (int j = 0; j < 16 && i + j < data.Length; j++)
                {
                    var b = data[i + j];
                    line.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }
                line.Append("|");
                
                logEntry.AppendLine(line.ToString());
            }
            
            // Try to interpret as text if it looks like ASCII
            var textData = TryDecodeAsText(data);
            if (textData != null)
            {
                logEntry.AppendLine();
                logEntry.AppendLine("Text interpretation:");
                logEntry.AppendLine(textData);
            }
            
            logEntry.AppendLine("=== End of data ===");
            logEntry.AppendLine();
            
            await WriteToFileWithRetryAsync(logEntry.ToString());
            
            // Reset failure counter on success
            Interlocked.Exchange(ref _consecutiveFailures, 0);
            
            //_logger.LogInformation("Logged {Length} bytes from {ClientInfo}", data.Length, clientInfo);
        }
        catch (Exception ex)
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            _logger.LogError(ex, "Error logging data from {ClientInfo} (failure #{Failures})", clientInfo, failures);
            
            // Disable logging if we have too many consecutive failures
            if (failures >= MaxConsecutiveFailures)
            {
                _loggingDisabled = true;
                _logger.LogError("Data logging disabled due to {Failures} consecutive failures", failures);
            }
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private string? TryDecodeAsText(byte[] data)
    {
        try
        {
            // First try UTF-16 (little-endian) - this is what FinishLynx uses
            if (data.Length % 2 == 0) // UTF-16 requires even number of bytes
            {
                var utf16Text = Encoding.Unicode.GetString(data);
                var utf16PrintableCount = utf16Text.Count(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c));
                var utf16PrintableRatio = (double)utf16PrintableCount / utf16Text.Length;
                
                if (utf16PrintableRatio > 0.7) // If more than 70% are printable characters
                {
                    return utf16Text;
                }
            }
            
            // Fallback to UTF-8
            var utf8PrintableCount = data.Count(b => b >= 32 && b <= 126);
            var utf8PrintableRatio = (double)utf8PrintableCount / data.Length;
            
            if (utf8PrintableRatio > 0.8) // If more than 80% are printable characters
            {
                return Encoding.UTF8.GetString(data);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    public void LogStartListSummaryAsync(RaceEvent eventData, List<Racer> racers, string clientInfo)
    {
        // Check if data logging is enabled or if we've disabled it due to failures
        if (!_loggingSettings.EnableDataLogging || _loggingDisabled)
        {
            return;
        }

        // Fire and forget - don't await this to prevent blocking the main application
        _ = Task.Run(async () =>
        {
            try
            {
                await LogStartListSummaryInternalAsync(eventData, racers, clientInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background StartList logging for {ClientInfo}", clientInfo);
            }
        });
    }

    private async Task LogStartListSummaryInternalAsync(RaceEvent eventData, List<Racer> racers, string clientInfo)
    {
        // Try to acquire semaphore with timeout
        if (!await _fileSemaphore.WaitAsync(_semaphoreTimeout))
        {
            _logger.LogWarning("Timeout waiting for file access, skipping StartList log entry for {ClientInfo}", clientInfo);
            return;
        }

        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = new StringBuilder();
            
            logEntry.AppendLine($"=== StartList Summary at {timestamp} from {clientInfo} ===");
            logEntry.AppendLine($"Event: {eventData.EventName}");
            logEntry.AppendLine($"Event Number: {eventData.EventNumber}");
            logEntry.AppendLine($"Round: {eventData.RoundNumber}, Heat: {eventData.HeatNumber}");
            logEntry.AppendLine($"Wind: {eventData.Wind}");
            logEntry.AppendLine($"Start Type: {eventData.StartType}");
            logEntry.AppendLine($"Official: {(eventData.IsOfficial ? "Yes" : "No")}");
            logEntry.AppendLine($"Number of Racers: {racers.Count}");
            logEntry.AppendLine();
            
            if (racers.Any())
            {
                logEntry.AppendLine("Racer List:");
                logEntry.AppendLine("Lane | ID   | Name                           | Affiliation");
                logEntry.AppendLine("-----|------|-------------------------------|----------------------------");
                
                foreach (var racer in racers.OrderBy(r => r.Lane))
                {
                    var name = racer.Name.Length > 30 ? racer.Name.Substring(0, 30) + "..." : racer.Name.PadRight(30);
                    var affiliation = racer.Affiliation.Length > 25 ? racer.Affiliation.Substring(0, 25) + "..." : racer.Affiliation.PadRight(25);
                    
                    logEntry.AppendLine($"{racer.Lane,4} | {racer.Id,4} | {name} | {affiliation}");
                }
            }
            
            logEntry.AppendLine("=== End of StartList Summary ===");
            logEntry.AppendLine();
            
            await WriteToFileWithRetryAsync(logEntry.ToString());
            
            // Reset failure counter on success
            Interlocked.Exchange(ref _consecutiveFailures, 0);
            
            _logger.LogInformation("Logged StartList summary with {Count} racers from {ClientInfo}", racers.Count, clientInfo);
        }
        catch (Exception ex)
        {
            var failures = Interlocked.Increment(ref _consecutiveFailures);
            _logger.LogError(ex, "Error logging StartList summary from {ClientInfo} (failure #{Failures})", clientInfo, failures);
            
            // Disable logging if we have too many consecutive failures
            if (failures >= MaxConsecutiveFailures)
            {
                _loggingDisabled = true;
                _logger.LogError("Data logging disabled due to {Failures} consecutive failures", failures);
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
                await File.AppendAllTextAsync(_logFilePath, content);
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
        throw new IOException($"Failed to write to log file after {maxRetries} attempts: {_logFilePath}");
    }

    public void Dispose()
    {
        _fileSemaphore?.Dispose();
    }
}
