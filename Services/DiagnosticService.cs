using Microsoft.Extensions.Logging;
using System.Text;

namespace WebLynx.Services;

public class DiagnosticService
{
    private readonly ILogger<DiagnosticService> _logger;

    public DiagnosticService(ILogger<DiagnosticService> logger)
    {
        _logger = logger;
    }

    public void AnalyzeData(byte[] data, string clientInfo)
    {
        try
        {
            // Try to decode as UTF-16 first
            if (data.Length % 2 == 0)
            {
                var utf16Text = Encoding.Unicode.GetString(data);

                if (utf16Text.Contains("Running time")) {
                    return;
                }

                _logger.LogInformation("=== DIAGNOSTIC: Data from {ClientInfo} ===", clientInfo);
                _logger.LogInformation("Data length: {Length} bytes", data.Length);
                _logger.LogInformation("UTF-16 decoded: {Text}", utf16Text);
                
                // Check for specific FinishLynx patterns
                var detectedSections = new List<string>();
                
                if (utf16Text.Contains("Running time"))
                {
                    detectedSections.Add("TimeRunning");
                }
                if (utf16Text.Contains("Stopped time"))
                {
                    detectedSections.Add("TimeStopped");
                }
                if (utf16Text.Contains("StartListHeader"))
                {
                    detectedSections.Add("StartListHeader");
                }
                if (utf16Text.Contains("StartedHeader"))
                {
                    detectedSections.Add("StartedHeader");
                }
                if (utf16Text.Contains("ResultsHeader"))
                {
                    detectedSections.Add("ResultsHeader");
                }
                if (utf16Text.Contains("Gun time"))
                {
                    detectedSections.Add("TimeGun");
                }
                if (utf16Text.Contains("Break time"))
                {
                    detectedSections.Add("TimeBreak");
                }
                if (utf16Text.Contains("Wind"))
                {
                    detectedSections.Add("Wind");
                }
                
                if (detectedSections.Any())
                {
                    _logger.LogInformation("✓ Detected sections: {Sections}", string.Join(", ", detectedSections));
                }
                else
                {
                    _logger.LogInformation("⚠ No recognized FinishLynx sections detected");
                }
                
                _logger.LogInformation("=== END DIAGNOSTIC ===");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in diagnostic analysis");
        }
    }
}
