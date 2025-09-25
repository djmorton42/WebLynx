using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WebLynx.Models;
using WebLynx.Utilities;

namespace WebLynx.Services;

public class MessageParser
{
    private readonly ILogger<MessageParser> _logger;
    private readonly Dictionary<string, StringBuilder> _messageBuffers = new();
    private readonly Dictionary<string, DateTime> _bufferTimestamps = new();
    private readonly TimeSpan _bufferTimeout = TimeSpan.FromSeconds(5);

    public MessageParser(ILogger<MessageParser> logger)
    {
        _logger = logger;
    }

    public MessageType DetectMessageType(string text)
    {
        if (text.Contains("Running time:"))
            return MessageType.RunningTime;
        if (text.Contains("*** StartListHeader ***"))
            return MessageType.StartListHeader;
        if (text.Contains("*** StartedHeader ***"))
            return MessageType.StartedHeader;
        if (text.Contains("*** ResultsHeader"))
            return MessageType.ResultsHeader;
        if (text.Contains("Message Header") && text.Contains("Message Trailer"))
            return MessageType.Announcement;
        
        return MessageType.Unknown;
    }

    public (MessageType messageType, string completeText) ProcessMessage(string text, string clientInfo)
    {
        // Clean up expired buffers
        CleanupExpiredBuffers();

        // Check if this looks like a continuation of a StartList message
        if (IsStartListContinuation(text))
        {
            var bufferKey = GetBufferKey(clientInfo);
            
            // If we have a buffer for this client, append to it
            if (_messageBuffers.ContainsKey(bufferKey))
            {
                _logger.LogDebug("Appending continuation to buffered StartList message for {ClientInfo} (length: {Length})", 
                    clientInfo, text.Length);
                
                _messageBuffers[bufferKey].Append(text);
                _bufferTimestamps[bufferKey] = DateTime.UtcNow;
                
                var completeText = _messageBuffers[bufferKey].ToString();
                
                // Check if we now have a complete StartList message
                if (IsCompleteStartListMessage(completeText))
                {
                    _logger.LogInformation("Completed buffered StartList message for {ClientInfo} (total length: {Length})", 
                        clientInfo, completeText.Length);
                    
                    // Visual feedback for completion
                    Console.WriteLine($"✅ Completed buffered StartList message from {clientInfo} (total length: {completeText.Length})");
                    
                    _messageBuffers.Remove(bufferKey);
                    _bufferTimestamps.Remove(bufferKey);
                    return (MessageType.StartListHeader, completeText);
                }
                
                // Still incomplete, return Unknown to indicate we're waiting
                return (MessageType.Unknown, string.Empty);
            }
            else
            {
                _logger.LogWarning("Received StartList continuation for {ClientInfo} but no buffer exists", clientInfo);
            }
        }

        // Check if this is a new StartList message that might be incomplete
        if (text.Contains("*** StartListHeader ***"))
        {
            if (!IsCompleteStartListMessage(text))
            {
                var bufferKey = GetBufferKey(clientInfo);
                _messageBuffers[bufferKey] = new StringBuilder(text);
                _bufferTimestamps[bufferKey] = DateTime.UtcNow;
                
                _logger.LogInformation("Buffering incomplete StartList message for {ClientInfo} (length: {Length})", 
                    clientInfo, text.Length);
                
                // Visual feedback for buffering
                Console.WriteLine($"📥 Buffering incomplete StartList message from {clientInfo} (waiting for completion...)");
                
                return (MessageType.Unknown, string.Empty);
            }
            else
            {
                // Complete StartList message - process normally
                return (MessageType.StartListHeader, text);
            }
        }

        // Regular message processing for non-StartList messages
        var messageType = DetectMessageType(text);
        return (messageType, text);
    }

    private bool IsStartListContinuation(string text)
    {
        // Check if this looks like a continuation of StartList data
        // It should contain racer data (numbers at start of lines) or the trailer
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check for racer data pattern: starts with number, space, number (lane, id)
            if (Regex.IsMatch(trimmedLine, @"^\d+\s+\d+\s+"))
            {
                return true;
            }
            
            // Check for trailer
            if (trimmedLine.Contains("*** StartList/Started/ResultsTrailer ***"))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsCompleteStartListMessage(string text)
    {
        // A complete StartList message should have both header and trailer
        return text.Contains("*** StartListHeader ***") && 
               text.Contains("*** StartList/Started/ResultsTrailer ***");
    }

    private string GetBufferKey(string clientInfo)
    {
        // Use client info as buffer key - this assumes one active connection per client
        return clientInfo;
    }

    private void CleanupExpiredBuffers()
    {
        var expiredKeys = _bufferTimestamps
            .Where(kvp => DateTime.UtcNow - kvp.Value > _bufferTimeout)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _logger.LogWarning("Cleaning up expired message buffer for {ClientInfo}", key);
            _messageBuffers.Remove(key);
            _bufferTimestamps.Remove(key);
        }
    }

    public Dictionary<string, (int bufferLength, DateTime lastUpdated)> GetBufferStatus()
    {
        return _messageBuffers.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.Length, _bufferTimestamps[kvp.Key])
        );
    }

    public TimeSpan? ParseRunningTime(string text)
    {
        try
        {
            // Pattern to match "Running time: 1:23.4" or "Running time: 23.4"
            var match = Regex.Match(text, @"Running time:\s*(\d+:)?(\d+\.\d+)");
            if (match.Success)
            {
                var minutes = match.Groups[1].Success ? 
                    int.Parse(match.Groups[1].Value.TrimEnd(':')) : 0;
                var seconds = double.Parse(match.Groups[2].Value);
                
                return TimeSpan.FromMinutes(minutes).Add(TimeSpan.FromSeconds(seconds));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing running time from: {Text}", text);
        }
        
        return null;
    }

    public RaceEvent? ParseStartListHeader(string text)
    {
        try
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var eventData = new RaceEvent();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("OFFICIAL/UNOFFICIAL:"))
                {
                    eventData.IsOfficial = trimmedLine.Contains("OFFICIAL");
                }
                else if (trimmedLine.StartsWith("Event name"))
                {
                    eventData.EventName = ExtractValue(trimmedLine);
                }
                else if (trimmedLine.StartsWith("Wind"))
                {
                    eventData.Wind = ExtractValue(trimmedLine);
                }
                else if (trimmedLine.StartsWith("Event number"))
                {
                    eventData.EventNumber = ExtractValue(trimmedLine);
                }
                else if (trimmedLine.StartsWith("Round number"))
                {
                    eventData.RoundNumber = int.Parse(ExtractValue(trimmedLine));
                }
                else if (trimmedLine.StartsWith("Heat number"))
                {
                    eventData.HeatNumber = int.Parse(ExtractValue(trimmedLine));
                }
                else if (trimmedLine.StartsWith("EEE-R-HH Name"))
                {
                    eventData.EeeRhhName = ExtractValue(trimmedLine);
                }
                else if (trimmedLine.StartsWith("AUTO/MANUAL start"))
                {
                    eventData.StartType = ExtractValue(trimmedLine).ToUpper() == "AUTO" ? 
                        StartType.Auto : StartType.Manual;
                }
                else if (trimmedLine.StartsWith("Number of results"))
                {
                    eventData.NumberOfResults = int.Parse(ExtractValue(trimmedLine));
                }
            }

            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing StartListHeader from: {Text}", text);
            return null;
        }
    }

    public List<Racer> ParseRacersFromStartList(string text)
    {
        var racers = new List<Racer>();
        
        try
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool inRacerSection = false;

            _logger.LogDebug("Parsing StartList with {LineCount} lines", lines.Length);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.Contains("Ln") && trimmedLine.Contains("Id") && trimmedLine.Contains("Name"))
                {
                    inRacerSection = true;
                    _logger.LogDebug("Found racer section header: {Header}", trimmedLine);
                    continue;
                }
                
                if (trimmedLine.StartsWith("---"))
                {
                    continue;
                }
                
                if (trimmedLine.StartsWith("*** StartList/Started/ResultsTrailer ***"))
                {
                    _logger.LogDebug("Found trailer, stopping racer parsing");
                    break;
                }
                
                if (inRacerSection && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    var racer = ParseRacerFromStartListLine(trimmedLine);
                    if (racer != null)
                    {
                        racers.Add(racer);
                        _logger.LogInformation("Added racer: Lane {Lane}, ID {Id}, Name {Name}", racer.Lane, racer.Id, racer.Name);
                    }
                }
            }
            
            _logger.LogDebug("Parsed {RacerCount} racers from StartList", racers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing racers from StartList: {Text}", text);
        }
        
        return racers;
    }

    public List<Racer> ParseRacersFromStarted(string text)
    {
        var racers = new List<Racer>();
        
        try
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            bool inRacerSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("Plc Ln") && trimmedLine.Contains("ReacTime"))
                {
                    inRacerSection = true;
                    continue;
                }
                
                if (trimmedLine.StartsWith("---"))
                {
                    continue;
                }
                
                if (trimmedLine.StartsWith("*** StartList/Started/ResultsTrailer ***"))
                {
                    break;
                }
                
                if (inRacerSection && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    var racer = ParseRacerFromStartedLine(line);
                    if (racer != null)
                    {
                        racers.Add(racer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing racers from Started: {Text}", text);
        }

        _logger.LogInformation("Parsed {Count} racers from Started", racers.Count);
        return racers;
    }

    public List<Racer> ParseRacersFromResults(string text)
    {
        var racers = new List<Racer>();
        
        try
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            bool inRacerSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("Plc Ln  Id") && trimmedLine.Contains("Name"))
                {
                    inRacerSection = true;
                    continue;
                }
                
                if (trimmedLine.StartsWith("---"))
                {
                    continue;
                }
                
                if (trimmedLine.StartsWith("*** StartList/Started/ResultsTrailer ***"))
                {
                    break;
                }
                
                if (inRacerSection && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    var racer = ParseRacerFromResultsLine(line);
                    if (racer != null)
                    {
                        racers.Add(racer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing racers from Results: {Text}", text);
        }
        
        return racers;
    }

    public decimal ParseLaps(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0m;
        }

        var trimmedText = text.Trim();
        _logger.LogDebug("ParseLaps input: '{Text}' -> trimmed: '{TrimmedText}'", text, trimmedText);

        // Handle "1/2" format for half laps
        if (trimmedText.Contains("1/2"))
        {
            // Extract the number before "1/2" and add 0.5
            var numberPart = trimmedText.Replace("1/2", "").Trim();
            _logger.LogDebug("ParseLaps half lap - numberPart: '{NumberPart}'", numberPart);
            if (int.TryParse(numberPart, out var wholeNumber))
            {
                var result = wholeNumber + 0.5m;
                _logger.LogDebug("ParseLaps half lap result: {Result}", result);
                return result;
            }
            _logger.LogDebug("ParseLaps half lap - just 1/2, returning 0.5");
            return 0.5m; // Just "1/2" means 0.5 laps
        }

        // Handle regular decimal parsing
        if (decimal.TryParse(trimmedText, out var decimalResult))
        {
            _logger.LogDebug("ParseLaps decimal result: {Result}", decimalResult);
            return decimalResult;
        }

        _logger.LogDebug("ParseLaps - invalid input, returning 0");
        return 0m;
    }

    public Racer? ParseRacerFromStartListLine(string line)
    {
        try
        {
            if (line.Length >= 9)
            {
                //0         1         2         3         4         5         6         7         8         9
                //0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789            
                //Ln  Id   Name                                               Affiliation                    Laps
                //--- ---- -------------------------------------------------- ------------------------------ ------
                //1   100  Person mcPersonFace                                Ottawa SSC"                    5

                var lane = FixedWidthParser.TrimParse(line, 0, 3, int.Parse);
                var id = FixedWidthParser.TrimParse(line, 4, 4, int.Parse);
                var name = FixedWidthParser.TrimParse(line, 9, 50, "Unknown");
                var affiliation = FixedWidthParser.TrimParse(line, 60, 30, s => s.TrimEnd('"'), "Unknown");
                var lapsText = FixedWidthParser.TrimParse(line, 91, 6, "");
                var laps = ParseLaps(lapsText);
                
                _logger.LogDebug("Parsed racer: Lane={Lane}, ID={Id}, Name='{Name}', Affiliation='{Affiliation}'", 
                    lane, id, name, affiliation);
                
                return new Racer
                {
                    Lane = lane,
                    Id = id,
                    Name = name ?? string.Empty,
                    Affiliation = affiliation ?? string.Empty,
                    LapsRemaining = laps
                };
            }
            else
            {
                _logger.LogDebug("Line too short for racer parsing: {Length} chars", line.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing racer from StartList line: {Line}", line);
        }
        
        return null;
    }

    public Racer? ParseRacerFromStartedLine(string line)
    {
        try
        {
// 0         1         2         3         4         5         6         7         8
// 01234567890123456789012345678901234567890123456789012345678901234567890123456789
// Plc Ln  ReacTime Cum ST   Last ST  Best ST  Laps   Speed  Pace  ^M
// --- --- -------- -------- -------- -------- ------ ------ ------^M
// 1   3            56.4     12.1     10.2     0             11.280
// 1   1            56.4     12.1     10.2     4 1/2         11.280
// 2   2            58.2     13.5     11.8     13 1/2        12.450

                        
            var placeText = FixedWidthParser.TrimParse(line, 0, 3, string.Empty);
            var place = new PlaceData(placeText);
            var lane = FixedWidthParser.TrimParse(line, 4, 3, int.Parse);
            var reactionTime = FixedWidthParser.TrimParse(line, 8, 8, TimeSpanParser.Parse);
            var cumulativeSplitTime = FixedWidthParser.TrimParse(line, 17, 8, TimeSpanParser.Parse);
            var lastSplitTime = FixedWidthParser.TrimParse(line, 26, 8, TimeSpanParser.Parse);
            var bestSplitTime = FixedWidthParser.TrimParse(line, 35, 8, TimeSpanParser.Parse);
            var lapsText = FixedWidthParser.TrimParse(line, 44, 6, "");
            var lapsRemaining = ParseLaps(lapsText);

            decimal? pace = FixedWidthParser.TrimParse(line, 58, 6, decimal.Parse, -1);
            decimal? speed = FixedWidthParser.TrimParse(line, 51, 6, decimal.Parse, -1);
            
            return new Racer
            {
                Place = place,
                Lane = lane,
                ReactionTime = reactionTime,
                CumulativeSplitTime = cumulativeSplitTime,
                LastSplitTime = lastSplitTime,
                BestSplitTime = bestSplitTime,
                LapsRemaining = lapsRemaining,
                Speed = speed,
                Pace = pace
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing racer from Started line: {Line}", line);
        }
        
        return null;
    }

    public Racer? ParseRacerFromResultsLine(string line)
    {
        try
        {
            //0         1         2         3         4         5         6         7         8         9         10        11        12        13
            //01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
            //Plc Ln  Id   Name                                               Affiliation                    Time     Delta    ReacTime^M
            //--- --- ---- -------------------------------------------------- ------------------------------ -------- -------- --------^M
            //1   4   168  Some Person                                        SSC"                           12.1     12.100

            if (line.Length >= 60) // Minimum length check
            {
                var placeText = FixedWidthParser.TrimParse(line, 0, 3, string.Empty);
                var placeData = new PlaceData(placeText);
                if (!placeData.HasPlaceData)
                {
                    return null; // Skip lines without place (unfinished racers)
                }

                var lane = FixedWidthParser.TrimParse(line, 4, 3, int.Parse, 0);
                var id = FixedWidthParser.TrimParse(line, 8, 4, int.Parse, 0);
                var name = FixedWidthParser.TrimParse(line, 13, 50, "Unknown");
                var affiliation = FixedWidthParser.TrimParse(line, 64, 30, s => s.TrimEnd('"'), "Unknown");

                TimeSpan? finalTime = null;
                TimeSpan? deltaTime = null;
                TimeSpan? reactionTime = null;

                var times = FixedWidthParser.TrimParse(line, 95, 26, string.Empty);
                var parts = times?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                if (parts.Length >= 1)
                {
                    finalTime = TimeSpanParser.Parse(parts[0]);
                }
                if (parts.Length >= 2)
                {
                    deltaTime = TimeSpanParser.Parse(parts[1]);
                }
                if (parts.Length >= 3)
                {
                    reactionTime = TimeSpanParser.Parse(parts[2]);
                }

                return new Racer
                {
                    Place = placeData,
                    Lane = lane,
                    Id = id,
                    Name = name ?? string.Empty,
                    Affiliation = affiliation ?? string.Empty,
                    FinalTime = finalTime,
                    DeltaTime = deltaTime,
                    ReactionTime = reactionTime,
                    HasFinished = true
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing racer from Results line: {Line}", line);
        }
        
        return null;
    }

    public string? ParseAnnouncementMessage(string text)
    {
        try
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var messageLines = new List<string>();
            bool inMessage = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.Contains("Message Header"))
                {
                    inMessage = true;
                    continue;
                }
                
                if (trimmedLine.Contains("Message Trailer"))
                {
                    break;
                }
                
                if (inMessage && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    messageLines.Add(trimmedLine);
                }
            }

            if (messageLines.Any())
            {
                var message = string.Join(" ", messageLines);
                _logger.LogInformation("Parsed announcement message: {Message}", message);
                return message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing announcement message from: {Text}", text);
        }
        
        return null;
    }

    public bool IsLapCountOnlyUpdate(List<Racer> racers)
    {
        // Check if ALL racers have no split/place data but do have lap counts
        return racers.All(r => 
            !r.Place.HasPlaceData && 
            r.ReactionTime == null && 
            r.CumulativeSplitTime == null && 
            r.LastSplitTime == null && 
            r.BestSplitTime == null &&
            r.LapsRemaining > 0);
    }

    private string ExtractValue(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < line.Length - 1)
        {
            return line.Substring(colonIndex + 1).Trim();
        }
        return string.Empty;
    }
}
