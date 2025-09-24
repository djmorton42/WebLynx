using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebLynx.Models;

namespace WebLynx.Services;

public class RaceStateManager
{
    private readonly ILogger<RaceStateManager> _logger;
    private readonly MessageParser _messageParser;
    private readonly DataLoggingService _dataLoggingService;
    private readonly LapCounterSettings _lapCounterSettings;
    
    public RaceData CurrentRace { get; private set; } = new();

    public event EventHandler<RaceData>? RaceUpdated;

    public RaceStateManager(
        ILogger<RaceStateManager> logger, 
        MessageParser messageParser,
        DataLoggingService dataLoggingService,
        IOptions<LapCounterSettings> lapCounterSettings)
    {
        _logger = logger;
        _messageParser = messageParser;
        _dataLoggingService = dataLoggingService;
        _lapCounterSettings = lapCounterSettings.Value;
    }

    public void ProcessMessageAsync(byte[] data, string clientInfo)
    {
        try
        {
            // Log the raw data
            _dataLoggingService.LogDataAsync(data, clientInfo);

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
                        ProcessStartListHeaderMessage(completeText);
                        break;
                    case MessageType.StartedHeader:
                        ProcessStartedHeaderMessage(completeText);
                        break;
                    case MessageType.ResultsHeader:
                        ProcessResultsHeaderMessage(completeText);
                        break;
                    case MessageType.Announcement:
                        ProcessAnnouncementMessage(completeText);
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
            
            // If race isn't already running, start it when we get the first running time
            if (CurrentRace.Status == RaceStatus.NotStarted)
            {
                CurrentRace.Status = RaceStatus.Running;
                _logger.LogInformation("Race started - status changed to Running (triggered by running time)");
                Console.WriteLine($"🏁 Race started - status changed to Running (triggered by running time)");
                
                // Handle half-lap mode logic for race start
                HandleHalfLapModeRaceStart();
            }
            
            //_logger.LogInformation("Updated running time to {Time}", runningTime.Value);
        }
    }

    private void ProcessStartListHeaderMessage(string text)
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
        
        // Initialize delayed lap counts and timestamps
        foreach (var racer in racers)
        {
            racer.InitializeDelayedLapCount();
        }
        
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
            _dataLoggingService.LogStartListSummaryAsync(eventData, racers, "StartList Complete");
            
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
        // Check if this is a new race start
        bool wasNotStarted = CurrentRace.Status == RaceStatus.NotStarted;
        
        // StartedHeader indicates the race has started
        CurrentRace.Status = RaceStatus.Running;
        _logger.LogInformation("Race started - status changed to Running (triggered by StartedHeader)");
        Console.WriteLine($"🏁 Race started - status changed to Running (triggered by StartedHeader)");
        
        // Handle half-lap mode logic for race start (if this was a new start)
        if (wasNotStarted)
        {
            HandleHalfLapModeRaceStart();
        }
        
        var eventData = _messageParser.ParseStartListHeader(text);
        if (eventData != null)
        {
            CurrentRace.Event = eventData;
        }

        var racers = _messageParser.ParseRacersFromStarted(text);
        if (racers.Any())
        {
            // Check if this is a lap-count-only update (no split/place data)
            bool isLapCountOnlyUpdate = _messageParser.IsLapCountOnlyUpdate(racers);
            
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
                    
                    // Update lap count with conditional delay behavior
                    existingRacer.UpdateLapsRemaining(racer.LapsRemaining, skipDelay: isLapCountOnlyUpdate);
                    
                    existingRacer.Speed = racer.Speed;
                    existingRacer.Pace = racer.Pace;
                    
                    // Handle half-lap mode first crossing logic
                    HandleHalfLapModeFirstCrossing(existingRacer);
                }
                else
                {
                    // Add new racer if not found
                    racer.InitializeDelayedLapCount();
                    CurrentRace.Racers.Add(racer);
                    
                    // Handle half-lap mode first crossing logic for new racer
                    HandleHalfLapModeFirstCrossing(racer);
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

    private void ProcessAnnouncementMessage(string text)
    {
        _logger.LogInformation("Processing Announcement message");
        
        var announcementMessage = _messageParser.ParseAnnouncementMessage(text);
        CurrentRace.AnnouncementMessage = announcementMessage; // Always update, even if null/empty
        
        if (!string.IsNullOrEmpty(announcementMessage))
        {
            _logger.LogInformation("Updated announcement message: {Message}", announcementMessage);
        }
        else
        {
            _logger.LogInformation("Cleared announcement message");
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

    private bool HasHalfLapLaps()
    {
        return CurrentRace.Racers.Any(r => r.LapsRemaining % 1 == 0.5m);
    }

    private void HandleHalfLapModeRaceStart()
    {
        if (!_lapCounterSettings.HalfLapModeEnabled)
            return;

        // Check if race has whole number of laps
        if (HasHalfLapLaps())
        {
            // For half-lap races, timing software handles the lap count changes
            // We just need to update the timestamp so UI knows race has started
            _logger.LogInformation("Half-lap mode: Half-lap race detected, timing software handles lap counts");
            foreach (var racer in CurrentRace.Racers)
            {
                racer.LapCountLastChanged = DateTime.UtcNow;
            }
        }
        else
        {
            // For whole-lap races, add 1 to delayed value so UI shows current value for 5 seconds
            _logger.LogInformation("Half-lap mode: Adding 1 to delayedLapsRemaining for whole-lap race at race start");
            foreach (var racer in CurrentRace.Racers)
            {
                var currentLaps = racer.LapsRemaining;
                racer.DelayedLapsRemaining = currentLaps + 1;
                racer.LapCountLastChanged = DateTime.UtcNow;
            }
        }
    }

    private void HandleHalfLapModeFirstCrossing(Racer racer)
    {
        if (!_lapCounterSettings.HalfLapModeEnabled)
            return;

        // Check if race has half-lap laps
        if (!HasHalfLapLaps())
            return; // Skip if race doesn't have half-lap laps

        // For half-lap races, the timing software changes what it sends after first crossing
        // So we don't need to add 1 here - the timing software handles it
        // This method is kept for potential future use but currently does nothing
        _logger.LogDebug("Half-lap mode: Timing software handles lap count changes for half-lap races");
    }

}
