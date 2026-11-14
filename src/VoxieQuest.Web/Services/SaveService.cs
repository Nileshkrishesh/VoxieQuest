using System.Text.Json;
using Microsoft.JSInterop;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements browser localStorage persistence via JavaScript interop with an in-memory fallback.
/// </summary>
public class SaveService : ISaveService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, string> _inMemoryFallback = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveService"/> class.
    /// </summary>
    /// <param name="jsRuntime">Blazor JavaScript runtime instance.</param>
    public SaveService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("voxieStorage.getItem", key);
            if (string.IsNullOrWhiteSpace(json))
            {
                if (_inMemoryFallback.TryGetValue(key, out var fallbackJson))
                {
                    return JsonSerializer.Deserialize<T>(fallbackJson, JsonOptions);
                }
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            // Fallback for pre-rendering or browsers where storage is inaccessible
            if (_inMemoryFallback.TryGetValue(key, out var fallbackJson))
            {
                return JsonSerializer.Deserialize<T>(fallbackJson, JsonOptions);
            }
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        _inMemoryFallback[key] = json;

        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieStorage.setItem", key, json);
        }
        catch
        {
            // Silently retain in-memory fallback if JS interop fails
        }
    }

    /// <inheritdoc />
    public async Task RemoveItemAsync(string key)
    {
        _inMemoryFallback.Remove(key);
        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieStorage.removeItem", key);
        }
        catch
        {
            // Ignore JS removal failures
        }
    }
}
