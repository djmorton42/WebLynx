using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace WebLynx.Models;

public class ViewProperties
{
    private readonly IConfiguration _configuration;
    
    // Dynamic properties dictionary that can hold any configuration from appsettings.json
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    public ViewProperties(IConfiguration configuration)
    {
        _configuration = configuration;
        LoadPropertiesFromConfiguration();
    }

    private void LoadPropertiesFromConfiguration()
    {
        var viewPropertiesSection = _configuration.GetSection("ViewProperties");
        
        foreach (var child in viewPropertiesSection.GetChildren())
        {
            var value = GetValueFromConfiguration(child);
            Properties[child.Key] = value;
        }
    }

    private object GetValueFromConfiguration(IConfigurationSection section)
    {
        // If this section has children, it's an object
        if (section.GetChildren().Any())
        {
            var dict = new Dictionary<string, object>();
            foreach (var child in section.GetChildren())
            {
                dict[child.Key] = GetValueFromConfiguration(child);
            }
            return dict;
        }
        
        // Otherwise, it's a primitive value
        var stringValue = section.Value;
        
        // Try to parse as different types
        if (int.TryParse(stringValue, out var intValue))
            return intValue;
        
        if (bool.TryParse(stringValue, out var boolValue))
            return boolValue;
        
        if (double.TryParse(stringValue, out var doubleValue))
            return doubleValue;
        
        // Default to string
        return stringValue ?? string.Empty;
    }

    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (Properties.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue!;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void SetProperty<T>(string key, T value)
    {
        Properties[key] = value!;
    }

    public bool HasProperty(string key)
    {
        return Properties.ContainsKey(key);
    }

    public Dictionary<string, T> GetDictionaryProperty<T>(string key)
    {
        var value = GetProperty<Dictionary<string, T>>(key);
        return value ?? new Dictionary<string, T>();
    }
}
