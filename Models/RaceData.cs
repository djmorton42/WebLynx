using System.Text;

namespace WebLynx.Models;

public class RaceData
{
    public RaceEvent? Event { get; set; }
    public List<Racer> Racers { get; set; } = new();
    public TimeSpan? CurrentTime { get; set; }
    public RaceStatus Status { get; set; } = RaceStatus.NotStarted;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? AnnouncementMessage { get; set; }
}

public class RaceEvent
{
    public string EventName { get; set; } = string.Empty;
    public string Wind { get; set; } = string.Empty;
    public string EventNumber { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public int HeatNumber { get; set; }
    public string EeeRhhName { get; set; } = string.Empty;
    public StartType StartType { get; set; }
    public bool IsOfficial { get; set; }
    public int NumberOfResults { get; set; }
}

public class Racer
{
    private decimal _lapsRemaining;
    private decimal _delayedLapsRemaining;
    private DateTime? _lapCountLastChanged;

    public int Lane { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public int Place { get; set; }
    public TimeSpan? ReactionTime { get; set; }
    public TimeSpan? CumulativeSplitTime { get; set; }
    public TimeSpan? LastSplitTime { get; set; }
    public TimeSpan? BestSplitTime { get; set; }
    
    public decimal LapsRemaining 
    { 
        get => _lapsRemaining;
        set
        {
            if (_lapsRemaining != value)
            {
                // Store the previous value as the delayed value
                _delayedLapsRemaining = _lapsRemaining;
                // Update the timestamp when lap count changes
                _lapCountLastChanged = DateTime.UtcNow;
                _lapsRemaining = value;
                
                // If this is the first time setting laps remaining, initialize delayed value
                if (_delayedLapsRemaining == 0 && value > 0)
                {
                    _delayedLapsRemaining = value;
                }
            }
        }
    }

    public decimal DelayedLapsRemaining
    {
        get => _delayedLapsRemaining;
        set
        {
            _delayedLapsRemaining = value;
            _lapCountLastChanged = DateTime.UtcNow;
        }
    }
    
    
    public DateTime? LapCountLastChanged 
    { 
        get => _lapCountLastChanged;
        set => _lapCountLastChanged = value;
    }
    
    public decimal? Speed { get; set; }
    public decimal? Pace { get; set; }
    public TimeSpan? FinalTime { get; set; }
    public TimeSpan? DeltaTime { get; set; }
    public bool HasFinished { get; set; }

    public decimal GetDelayedLapsRemaining(int delaySeconds = 5)
    {
        if (_lapCountLastChanged == null)
        {
            return _lapsRemaining;
        }

        var timeSinceChange = DateTime.UtcNow - _lapCountLastChanged.Value;
        if (timeSinceChange.TotalSeconds >= delaySeconds)
        {
            // Enough time has passed, return the current lap count
            return _lapsRemaining;
        }

        // Not enough time has passed, return the delayed count (previous value)
        return _delayedLapsRemaining;
    }

    public void InitializeDelayedLapCount()
    {
        _delayedLapsRemaining = _lapsRemaining;
        _lapCountLastChanged = DateTime.UtcNow;
        
        // If delayed value is 0 but current value is not, fix it
        if (_delayedLapsRemaining == 0 && _lapsRemaining > 0)
        {
            _delayedLapsRemaining = _lapsRemaining;
        }
    }

    public void UpdateLapsRemaining(decimal newValue, bool skipDelay = false)
    {
        if (skipDelay)
        {
            // Direct update without delay - set both current and delayed to the same value
            _lapsRemaining = newValue;
            _delayedLapsRemaining = newValue;
            _lapCountLastChanged = DateTime.UtcNow;
        }
        else
        {
            // Use the normal setter with delay behavior
            LapsRemaining = newValue;
        }
    }

    public override string ToString()
    {
        return $"Racer: Lane={Lane}, Id={Id}, Name='{Name}', Affiliation='{Affiliation}', Place={Place}, " +
               $"ReactionTime={ReactionTime}, CumulativeSplitTime={CumulativeSplitTime}, LastSplitTime={LastSplitTime}, " +
               $"BestSplitTime={BestSplitTime}, LapsRemaining={LapsRemaining}, Speed={Speed}, Pace={Pace}, " +
               $"FinalTime={FinalTime}, DeltaTime={DeltaTime}, HasFinished={HasFinished}";
    }
}

public enum RaceStatus
{
    NotStarted,
    Running,
    Paused,
    Finished
}

public enum StartType
{
    Auto,
    Manual
}

public enum MessageType
{
    RunningTime,
    StartListHeader,
    StartedHeader,
    ResultsHeader,
    Announcement,
    Unknown
}

public class RaceDataApiResponse
{
    public TimeSpan? CurrentTime { get; set; }
    public RaceEvent? Event { get; set; }
    public RaceStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<RacerApiResponse> Racers { get; set; } = new();
    public string? AnnouncementMessage { get; set; }
    public bool HalfLapModeEnabled { get; set; }
}

public class RacerApiResponse
{
    public int Lane { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public int Place { get; set; }
    public TimeSpan? ReactionTime { get; set; }
    public TimeSpan? CumulativeSplitTime { get; set; }
    public TimeSpan? LastSplitTime { get; set; }
    public TimeSpan? BestSplitTime { get; set; }
    public decimal LapsRemaining { get; set; }
    public decimal DelayedLapsRemaining { get; set; }
    public DateTime? LapCountLastChanged { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Pace { get; set; }
    public TimeSpan? FinalTime { get; set; }
    public TimeSpan? DeltaTime { get; set; }
    public bool HasFinished { get; set; }
    public bool HasFirstCrossing { get; set; }
}

