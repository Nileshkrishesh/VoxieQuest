namespace VoxieQuest.Web.Services;

/// <summary>
/// Child-safe local persistence service wrapping browser localStorage via JS interop.
/// </summary>
public interface ISaveService
{
    /// <summary>
    /// Retrieves a typed object from local storage.
    /// </summary>
    /// <typeparam name="T">Target deserialization type.</typeparam>
    /// <param name="key">Unique storage key name.</param>
    /// <returns>Deserialized instance or default value if missing.</returns>
    Task<T?> GetItemAsync<T>(string key);

    /// <summary>
    /// Stores an object as serialized JSON in local storage.
    /// </summary>
    /// <typeparam name="T">Source type.</typeparam>
    /// <param name="key">Unique storage key name.</param>
    /// <param name="value">Object payload to persist.</param>
    Task SetItemAsync<T>(string key, T value);

    /// <summary>
    /// Removes a key from local storage.
    /// </summary>
    /// <param name="key">Key to remove.</param>
    Task RemoveItemAsync(string key);
}
