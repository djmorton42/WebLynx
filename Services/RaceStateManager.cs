using System.Text;
using Microsoft.Extensions.Logging;
using WebLynx.Models;

namespace WebLynx.Services;

public class RaceStateManager
{
    private readonly ILogger<RaceStateManager> _logger;
    private readonly MessageParser _messageParser;
    private readonly DataLoggingService _dataLoggingService;
    
    public RaceData CurrentRace { get; private set; } = new();

    public event EventHandler<RaceData>? RaceUpdated;

    public RaceStateManager(
        ILogger<RaceStateManager> logger, 
        MessageParser messageParser,
        DataLoggingService dataLoggingService)
    {
        _logger = logger;
        _messageParser = messageParser;
        _dataLoggingService = dataLoggingService;
    }

    public async Task ProcessMessageAsync(byte[] data, string clientInfo)
    {
        try
        {
            // Log the raw data
            await _dataLoggingService.LogDataAsync(data, clientInfo);

            // Decode the message
            var text = DecodeMessage(data);
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Received empty or invalid message from {ClientInfo}", clientInfo);
                return;
            }

            // Process message with buffering support
            var (messageType, completeText) = _messageParser.ProcessMessage(text, clientInfo);
            
            // Skip processing if message is being buffered (Unknown with empty text)
            if (messageType == MessageType.Unknown && string.IsNullOrEmpty(completeText))
            {
                _logger.LogDebug("Message buffered for {ClientInfo}, waiting for completion", clientInfo);
                return;
            }

            if (messageType != MessageType.RunningTime)
            {
                _logger.LogInformation("Processing {MessageType} message from {ClientInfo}", messageType, clientInfo);
            }

                switch (messageType)
                {
                    case MessageType.RunningTime:
                        ProcessRunningTimeMessage(completeText);
                        break;
                    case MessageType.StartListHeader:
                        // StartListHeader clears all existing state and loads new race
                        await ProcessStartListHeaderMessage(completeText);
                        break;
                    case MessageType.StartedHeader:
                        ProcessStartedHeaderMessage(completeText);
                        break;
                    case MessageType.ResultsHeader:
                        ProcessResultsHeaderMessage(completeText);
                        break;
                    default:
                        _logger.LogWarning("Unknown message type from {ClientInfo}: {Text}", clientInfo, completeText);
                        break;
                }

            // Update last updated timestamp
            CurrentRace.LastUpdated = DateTime.UtcNow;

            // Notify subscribers
            _logger.LogDebug("Notifying subscribers of race update");
            RaceUpdated?.Invoke(this, CurrentRace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {ClientInfo}", clientInfo);
        }
    }

    private void ProcessRunningTimeMessage(string text)
    {
        var runningTime = _messageParser.ParseRunningTime(text);
        if (runningTime.HasValue)
        {
            CurrentRace.CurrentTime = runningTime.Value;
            CurrentRace.Status = RaceStatus.Running;
            //_logger.LogInformation("Updated running time to {Time}", runningTime.Value);
        }
    }

    private async Task ProcessStartListHeaderMessage(string text)
    {
        // StartListHeader indicates a new race has been loaded - clear existing state
        _logger.LogInformation("StartListHeader received - clearing existing race state and loading new race");
        
        // Reset the race state
        CurrentRace = new RaceData();
        
        var eventData = _messageParser.ParseStartListHeader(text);
        if (eventData != null)
        {
            CurrentRace.Event = eventData;
            _logger.LogInformation("Loaded new event data: {EventName}", eventData.EventName);
        }

        var racers = _messageParser.ParseRacersFromStartList(text);
        _logger.LogDebug("Parsed {Count} racers from StartList text", racers.Count);
        
        CurrentRace.Racers = racers;
        
        if (racers.Any())
        {
            _logger.LogInformation("Loaded new racer list with {Count} racers", racers.Count);
        }
        else
        {
            _logger.LogWarning("No racers parsed from StartList message");
        }
        
        // Log a summary of the complete StartList data
        if (eventData != null)
        {
            await _dataLoggingService.LogStartListSummaryAsync(eventData, racers, "StartList Complete");
            
            // Also output a visual summary to console
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("🏁 COMPLETE STARTLIST RECEIVED 🏁");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Event: {eventData.EventName}");
            Console.WriteLine($"Event Number: {eventData.EventNumber} | Round: {eventData.RoundNumber} | Heat: {eventData.HeatNumber}");
            Console.WriteLine($"Wind: {eventData.Wind} | Start: {eventData.StartType} | Official: {(eventData.IsOfficial ? "Yes" : "No")}");
            Console.WriteLine($"Total Racers: {racers.Count}");
            Console.WriteLine();
            
            if (racers.Any())
            {
                Console.WriteLine("Racer List:");
                Console.WriteLine("Lane | ID   | Name                           | Affiliation");
                Console.WriteLine("-----|------|--------------------------------|----------------------------");
                
                foreach (var racer in racers.OrderBy(r => r.Lane))
                {
                    var name = racer.Name.Length > 30 ? racer.Name.Substring(0, 30) + "..." : racer.Name.PadRight(30);
                    var affiliation = racer.Affiliation.Length > 25 ? racer.Affiliation.Substring(0, 25) + "..." : racer.Affiliation.PadRight(25);
                    Console.WriteLine($"{racer.Lane,4} | {racer.Id,4} | {name} | {affiliation}");
                }
            }
            else
            {
                Console.WriteLine("⚠️  No racers found in StartList message");
            }
            
            Console.WriteLine(new string('=', 60));
            Console.WriteLine();
        }
        
        // Update timestamp
        CurrentRace.LastUpdated = DateTime.UtcNow;
    }

    private void ProcessStartedHeaderMessage(string text)
    {
        var eventData = _messageParser.ParseStartListHeader(text);
        if (eventData != null)
        {
            CurrentRace.Event = eventData;
        }

        var racers = _messageParser.ParseRacersFromStarted(text);
        if (racers.Any())
        {
            // Update existing racers with new data
            foreach (var racer in racers)
            {
                var existingRacer = CurrentRace.Racers.FirstOrDefault(r => r.Lane == racer.Lane);
                if (existingRacer != null)
                {
                    // Update existing racer with new data
                    existingRacer.Place = racer.Place;
                    existingRacer.ReactionTime = racer.ReactionTime;
                    existingRacer.CumulativeSplitTime = racer.CumulativeSplitTime;
                    existingRacer.LastSplitTime = racer.LastSplitTime;
                    existingRacer.BestSplitTime = racer.BestSplitTime;
                    existingRacer.LapsRemaining = racer.LapsRemaining;
                    existingRacer.Speed = racer.Speed;
                    existingRacer.Pace = racer.Pace;
                }
                else
                {
                    // Add new racer if not found
                    CurrentRace.Racers.Add(racer);
                }
            }
            
            _logger.LogInformation("Updated racer progress data for {Count} racers", racers.Count);
        }
    }

    private void ProcessResultsHeaderMessage(string text)
    {
        _logger.LogInformation("Processing ResultsHeader message");
        
        var eventData = _messageParser.ParseStartListHeader(text);
        if (eventData != null)
        {
            CurrentRace.Event = eventData;
        }

        var racers = _messageParser.ParseRacersFromResults(text);
        _logger.LogInformation("Parsed {Count} racers from Results message", racers.Count);
        
        if (racers.Any())
        {
            // Update existing racers with final results
            foreach (var racer in racers)
            {
                var existingRacer = CurrentRace.Racers.FirstOrDefault(r => r.Lane == racer.Lane);
                if (existingRacer != null)
                {
                    _logger.LogInformation("Updating racer {Lane} with final time {FinalTime}", racer.Lane, racer.FinalTime);
                    // Update existing racer with final results
                    existingRacer.Place = racer.Place;
                    existingRacer.FinalTime = racer.FinalTime;
                    existingRacer.DeltaTime = racer.DeltaTime;
                    existingRacer.ReactionTime = racer.ReactionTime;
                    existingRacer.HasFinished = racer.HasFinished;
                }
                else
                {
                    // Add new racer if not found
                    CurrentRace.Racers.Add(racer);
                }
            }
            
            CurrentRace.Status = RaceStatus.Finished;
            _logger.LogInformation("Updated final results for {Count} racers", racers.Count);
        }
    }

    private string DecodeMessage(byte[] data)
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
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding message data");
            return string.Empty;
        }
    }

    public void ResetRace()
    {
        CurrentRace = new RaceData();
        _logger.LogInformation("Race state manually reset - all data cleared");
    }

    public RaceData GetCurrentRaceState()
    {
        return CurrentRace;
    }
}
