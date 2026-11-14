using System.Net.Http.Json;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Handles Voxie Across India state aggregation, anonymous presence submission,
/// parent opt-in consent persistence, and offline fallback resiliency.
/// </summary>
public class IndiaMapService : IIndiaMapService
{
    private const string StorageKey = "voxie_india_presence_consent";
    private readonly ISaveService _saveService;
    private readonly HttpClient _httpClient;
    private readonly IAudioService _audioService;

    private IndiaPresenceConsent? _cachedConsent;
    private IndiaMapResponse? _cachedMapResponse;
    private DateTime _lastFetchTime = DateTime.MinValue;

    public event Action? OnPresenceChanged;

    public IndiaMapService(ISaveService saveService, HttpClient httpClient, IAudioService audioService)
    {
        _saveService = saveService;
        _httpClient = httpClient;
        _audioService = audioService;
    }

    public async Task<IndiaPresenceConsent> GetConsentAsync()
    {
        if (_cachedConsent != null)
        {
            return _cachedConsent;
        }

        try
        {
            var saved = await _saveService.GetItemAsync<IndiaPresenceConsent>(StorageKey);
            if (saved != null)
            {
                if (string.IsNullOrEmpty(saved.AnonymousInstallId))
                {
                    saved.AnonymousInstallId = Guid.NewGuid().ToString("N");
                }
                _cachedConsent = saved;
                return saved;
            }
        }
        catch
        {
            // Fallback gracefully on fresh device
        }

        _cachedConsent = new IndiaPresenceConsent();
        return _cachedConsent;
    }

    public async Task SaveConsentAsync(bool isEnabled, string? stateCode, string? ageBand)
    {
        var consent = await GetConsentAsync();
        bool wasEnabled = consent.IsEnabled;
        string? oldState = consent.StateCode;

        consent.IsEnabled = isEnabled;
        consent.StateCode = isEnabled ? IndiaStateRegistry.NormalizeCode(stateCode ?? "PB") : null;
        consent.AgeBand = isEnabled ? (string.IsNullOrWhiteSpace(ageBand) ? "6-8" : ageBand) : null;

        await _saveService.SetItemAsync(StorageKey, consent);

        // Sync with serverless API
        if (isEnabled && !string.IsNullOrEmpty(consent.StateCode))
        {
            await PushPresencePayloadAsync(consent.StateCode, consent.AgeBand ?? "6-8", consent.AnonymousInstallId);
            consent.LastSyncedUtc = DateTime.UtcNow;
            await _saveService.SetItemAsync(StorageKey, consent);
        }
        else if (wasEnabled && !isEnabled)
        {
            await DeletePresenceRecordAsync(consent.AnonymousInstallId);
        }

        OnPresenceChanged?.Invoke();
    }

    public async Task DeleteContributionAsync()
    {
        var consent = await GetConsentAsync();
        var anonId = consent.AnonymousInstallId;

        consent.IsEnabled = false;
        consent.StateCode = null;
        consent.AgeBand = null;
        consent.LastSyncedUtc = null;

        await _saveService.SetItemAsync(StorageKey, consent);
        await DeletePresenceRecordAsync(anonId);

        OnPresenceChanged?.Invoke();
    }

    public async Task<IndiaMapResponse> GetMapTotalsAsync(bool forceRefresh = false)
    {
        // Cache map totals in-memory for up to 5 minutes during client session
        if (!forceRefresh && _cachedMapResponse != null && (DateTime.UtcNow - _lastFetchTime).TotalMinutes < 5)
        {
            return _cachedMapResponse;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<IndiaMapResponse>("api/map");
            if (response != null && response.Success)
            {
                _cachedMapResponse = response;
                _lastFetchTime = DateTime.UtcNow;
                return response;
            }
        }
        catch
        {
            // API unreachable or offline - fall back gracefully
        }

        // Return resilient seeded fallback
        var fallback = CreateOfflineFallback();
        _cachedMapResponse = fallback;
        return fallback;
    }

    public async Task<bool> WaveToIndiaAsync()
    {
        var consent = await GetConsentAsync();
        consent.HasWaved = true;
        await _saveService.SetItemAsync(StorageKey, consent);
        await _audioService.PlaySoundAsync(SoundEffect.SuccessChime);
        OnPresenceChanged?.Invoke();
        return true;
    }

    private async Task PushPresencePayloadAsync(string stateCode, string ageBand, string anonId)
    {
        try
        {
            // Strictly 3 privacy-vetted fields only: stateCode, ageBand, anonId
            var payload = new IndiaPresencePayload
            {
                StateCode = stateCode,
                AgeBand = ageBand,
                AnonId = anonId
            };

            await _httpClient.PostAsJsonAsync("api/presence", payload);
        }
        catch
        {
            // Offline resiliency: network failures do not halt gameplay
        }
    }

    private async Task DeletePresenceRecordAsync(string anonId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/presence?anonId={Uri.EscapeDataString(anonId)}");
            await _httpClient.SendAsync(request);
        }
        catch
        {
            // Ignore API network errors on delete
        }
    }

    private static IndiaMapResponse CreateOfflineFallback()
    {
        // Pre-seeded baseline aggregate totals showing active learning across 14 diverse states
        var fallback = new IndiaMapResponse
        {
            Success = true,
            Timestamp = DateTime.UtcNow,
            StatesWithActiveLearners = 14,
            IsOfflineFallback = true,
            States = new Dictionary<string, IndiaStateApiItem>(StringComparer.OrdinalIgnoreCase)
            {
                ["PB"] = new() { CountText = "100+", Band = "Growing" },
                ["MH"] = new() { CountText = "500+", Band = "Busy" },
                ["KA"] = new() { CountText = "250+", Band = "Busy" },
                ["DL"] = new() { CountText = "200+", Band = "Busy" },
                ["TN"] = new() { CountText = "150+", Band = "Growing" },
                ["GJ"] = new() { CountText = "100+", Band = "Growing" },
                ["WB"] = new() { CountText = "80+", Band = "Growing" },
                ["UP"] = new() { CountText = "200+", Band = "Busy" },
                ["RJ"] = new() { CountText = "90+", Band = "Growing" },
                ["KL"] = new() { CountText = "120+", Band = "Growing" },
                ["TG"] = new() { CountText = "100+", Band = "Growing" },
                ["MP"] = new() { CountText = "60+", Band = "Growing" },
                ["AS"] = new() { CountText = "30+", Band = "Growing" },
                ["OD"] = new() { CountText = "40+", Band = "Growing" },
                ["GA"] = new() { CountText = "Just starting here!", Band = "JustStarting" },
                ["HP"] = new() { CountText = "Just starting here!", Band = "JustStarting" }
            }
        };

        return fallback;
    }
}
