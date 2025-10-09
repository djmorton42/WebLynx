using System.Text.Json;

namespace WebLynx.Models;

public class ViewProperties
{
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    public T GetProperty<T>(string key, T defaultValue = default(T))
    {
        if (Properties.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
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
        Properties[key] = value;
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
