using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service managing the privacy-first Voxie Across India map,
/// anonymous presence registration, and aggregated state totals.
/// </summary>
public interface IIndiaMapService
{
    /// <summary>Loads active parent consent preferences from browser local storage.</summary>
    Task<IndiaPresenceConsent> GetConsentAsync();

    /// <summary>
    /// Updates parent consent, selected state, and developmental age band.
    /// Synchronizes presence to /api/presence or deletes it if opted out.
    /// </summary>
    Task SaveConsentAsync(bool isEnabled, string? stateCode, string? ageBand);

    /// <summary>Explicitly deletes the child's presence contribution from the backend store and opts out.</summary>
    Task DeleteContributionAsync();

    /// <summary>
    /// Fetches aggregated, thresholded, rounded totals per state from GET /api/map.
    /// Falls back gracefully to offline mode if unreachable.
    /// </summary>
    Task<IndiaMapResponse> GetMapTotalsAsync(bool forceRefresh = false);

    /// <summary>Child waves to India, unlocking the special explorer sticker (no chat or social messaging).</summary>
    Task<bool> WaveToIndiaAsync();

    /// <summary>Fired when parent consent or map data changes.</summary>
    event Action? OnPresenceChanged;
}
