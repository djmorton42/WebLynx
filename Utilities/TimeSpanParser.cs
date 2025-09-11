namespace WebLynx.Utilities;

public class TimeSpanParser
{
    public static TimeSpan? Parse(string timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString) || timeString == "0.00" || timeString == "0")
            return null;

        try
        {
            // Handle formats like "1:23.45", "23.45", "10.2"
            if (timeString.Contains(':'))
            {
                var parts = timeString.Split(':');
                var minutes = int.Parse(parts[0]);
                var seconds = double.Parse(parts[1]);
                return TimeSpan.FromMinutes(minutes).Add(TimeSpan.FromSeconds(seconds));
            }
            else
            {
                var seconds = double.Parse(timeString);
                return TimeSpan.FromSeconds(seconds);
            }
        }
        catch
        {
            return null;
        }
    }
}