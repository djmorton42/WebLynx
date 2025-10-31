using System.Collections.Concurrent;

namespace WebLynx.Services;

public class KeyValueStoreService
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    /// <summary>
    /// Sets a key-value pair. If value is null or empty, removes the key.
    /// </summary>
    public void SetValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _store.TryRemove(key, out _);
        }
        else
        {
            _store.AddOrUpdate(key, value, (_, _) => value);
        }
    }

    /// <summary>
    /// Gets the value for a key, or null if not found.
    /// </summary>
    public string? GetValue(string key)
    {
        return _store.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets all key-value pairs as a dictionary.
    /// </summary>
    public Dictionary<string, string> GetAllValues()
    {
        return new Dictionary<string, string>(_store);
    }

    /// <summary>
    /// Checks if a key exists.
    /// </summary>
    public bool HasKey(string key)
    {
        return _store.ContainsKey(key);
    }

    /// <summary>
    /// Removes a key-value pair.
    /// </summary>
    public bool RemoveKey(string key)
    {
        return _store.TryRemove(key, out _);
    }
}

