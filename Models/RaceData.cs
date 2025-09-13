using System.Text;

namespace WebLynx.Models;

public class RaceData
{
    public RaceEvent? Event { get; set; }
    public List<Racer> Racers { get; set; } = new();
    public TimeSpan? CurrentTime { get; set; }
    public RaceStatus Status { get; set; } = RaceStatus.NotStarted;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
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
    public int Lane { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public int Place { get; set; }
    public TimeSpan? ReactionTime { get; set; }
    public TimeSpan? CumulativeSplitTime { get; set; }
    public TimeSpan? LastSplitTime { get; set; }
    public TimeSpan? BestSplitTime { get; set; }
    public int LapsRemaining { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Pace { get; set; }
    public TimeSpan? FinalTime { get; set; }
    public TimeSpan? DeltaTime { get; set; }
    public bool HasFinished { get; set; }

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
    Unknown
}

public class RaceDataApiResponse
{
    public TimeSpan? CurrentTime { get; set; }
    public RaceEvent? Event { get; set; }
    public RaceStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<RacerApiResponse> Racers { get; set; } = new();
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
    public int LapsRemaining { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Pace { get; set; }
    public TimeSpan? FinalTime { get; set; }
    public TimeSpan? DeltaTime { get; set; }
    public bool HasFinished { get; set; }
}

