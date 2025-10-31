using WebLynx.Models;
using Microsoft.Extensions.Configuration;

namespace WebLynx.Services;

public class ViewDiscoveryService
{
    private readonly ILogger<ViewDiscoveryService> _logger;
    private readonly string _viewsPath;
    private readonly List<ViewMetadata> _discoveredViews = new();
    private readonly IConfiguration _configuration;
    private readonly KeyValueStoreService? _keyValueStore;

    public ViewDiscoveryService(ILogger<ViewDiscoveryService> logger, IConfiguration configuration, KeyValueStoreService? keyValueStore = null)
    {
        _logger = logger;
        _configuration = configuration;
        _keyValueStore = keyValueStore;
        _viewsPath = Path.Combine(Directory.GetCurrentDirectory(), "Views");
    }

    public List<ViewMetadata> DiscoveredViews => _discoveredViews.ToList();

    public void DiscoverViews()
    {
        _discoveredViews.Clear();
        
        if (!Directory.Exists(_viewsPath))
        {
            _logger.LogWarning("Views directory not found: {ViewsPath}", _viewsPath);
            return;
        }

        var viewDirectories = Directory.GetDirectories(_viewsPath)
            .Where(dir => !Path.GetFileName(dir).StartsWith("."))
            .OrderBy(dir => Path.GetFileName(dir));

        var keyValueHistory = new Dictionary<string, (string firstView, string firstValue)>();
        
        foreach (var viewDirectory in viewDirectories)
        {
            var viewName = Path.GetFileName(viewDirectory);
            var viewMetadata = ValidateViewDirectory(viewDirectory, viewName);
            _discoveredViews.Add(viewMetadata);
            
            if (viewMetadata.IsValid)
            {
                _logger.LogInformation("Discovered valid view: {ViewName} (http://localhost:{Port}/views/{ViewName})", viewName, GetHttpPort(), viewName);
                
                // Load view.properties if it exists
                LoadViewProperties(viewDirectory, viewName, keyValueHistory);
            }
            else
            {
                _logger.LogWarning("Invalid view directory: {ViewName}. Missing files: {MissingFiles}", 
                    viewName, string.Join(", ", viewMetadata.MissingFiles));
            }
        }

        _logger.LogInformation("View discovery completed. Found {ValidCount} valid views out of {TotalCount} directories", 
            _discoveredViews.Count(v => v.IsValid), _discoveredViews.Count);
    }

    public ViewMetadata? GetViewMetadata(string viewName)
    {
        return _discoveredViews.FirstOrDefault(v => v.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsValidView(string viewName)
    {
        var view = GetViewMetadata(viewName);
        return view?.IsValid ?? false;
    }

    private ViewMetadata ValidateViewDirectory(string viewDirectory, string viewName)
    {
        var metadata = new ViewMetadata
        {
            Name = viewName,
            DisplayName = FormatDisplayName(viewName),
            TemplatePath = Path.Combine(viewDirectory, "template.html"),
            StylesPath = Path.Combine(viewDirectory, "styles.css")
        };

        // Define required files
        metadata.RequiredFiles.Add("template.html");
        
        // Check for required files
        foreach (var requiredFile in metadata.RequiredFiles)
        {
            var filePath = Path.Combine(viewDirectory, requiredFile);
            if (!File.Exists(filePath))
            {
                metadata.MissingFiles.Add(requiredFile);
            }
        }

        // Check for optional files
        var optionalFiles = new[] { "styles.css" };
        foreach (var optionalFile in optionalFiles)
        {
            var filePath = Path.Combine(viewDirectory, optionalFile);
            if (File.Exists(filePath))
            {
                // Add to required files for this view if it exists
                if (!metadata.RequiredFiles.Contains(optionalFile))
                {
                    metadata.RequiredFiles.Add(optionalFile);
                }
            }
        }

        // Set description based on view name or try to read from a description file
        metadata.Description = GetViewDescription(viewDirectory, viewName);

        // View is valid if it has all required files
        metadata.IsValid = metadata.MissingFiles.Count == 0;

        return metadata;
    }

    private string FormatDisplayName(string viewName)
    {
        // Convert snake_case or kebab-case to Title Case
        return viewName
            .Replace("_", " ")
            .Replace("-", " ")
            .Split(' ')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower())
            .Aggregate((a, b) => a + " " + b);
    }

    private string GetViewDescription(string viewDirectory, string viewName)
    {
        // Try to read from a description.txt file
        var descriptionFile = Path.Combine(viewDirectory, "description.txt");
        if (File.Exists(descriptionFile))
        {
            try
            {
                return File.ReadAllText(descriptionFile).Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read description file for view {ViewName}", viewName);
            }
        }

        // No fallback - return empty string if no description.txt exists
        return string.Empty;
    }

    private int GetHttpPort()
    {
        return _configuration.GetValue<int>("HttpSettings:Port");
    }

    private void LoadViewProperties(string viewDirectory, string viewName, Dictionary<string, (string firstView, string firstValue)> keyValueHistory)
    {
        if (_keyValueStore == null)
        {
            return;
        }

        var propertiesFilePath = Path.Combine(viewDirectory, "view.properties");
        if (!File.Exists(propertiesFilePath))
        {
            return;
        }

        try
        {
            var lines = File.ReadAllLines(propertiesFilePath);
            foreach (var line in lines)
            {
                // Skip empty lines and comments (lines starting with #)
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // Parse key=value format
                var equalIndex = trimmedLine.IndexOf('=');
                if (equalIndex <= 0 || equalIndex >= trimmedLine.Length - 1)
                {
                    _logger.LogWarning("Invalid line format in view.properties for view {ViewName}: {Line}", viewName, trimmedLine);
                    continue;
                }

                var key = trimmedLine.Substring(0, equalIndex).Trim();
                var value = trimmedLine.Substring(equalIndex + 1).Trim();

                if (string.IsNullOrEmpty(key))
                {
                    _logger.LogWarning("Empty key in view.properties for view {ViewName}: {Line}", viewName, trimmedLine);
                    continue;
                }

                // Check for conflicts (if key was set by a different view with a different value)
                if (keyValueHistory.TryGetValue(key, out var history))
                {
                    if (history.firstValue != value)
                    {
                        _logger.LogWarning("Key-value conflict for key '{Key}': view '{FirstView}' set '{FirstValue}', view '{CurrentView}' set '{CurrentValue}' (using '{CurrentValue}' - last one wins)", 
                            key, history.firstView, history.firstValue, viewName, value, value);
                    }
                }
                else
                {
                    // First time this key is being set
                    keyValueHistory[key] = (viewName, value);
                }

                // Set the value (last one wins)
                _keyValueStore.SetValue(key, value);
                _logger.LogDebug("Loaded key-value from view {ViewName}: {Key} = {Value}", viewName, key, value);
            }

            _logger.LogInformation("Loaded view.properties for view: {ViewName}", viewName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load view.properties for view {ViewName}", viewName);
        }
    }
}
