using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Static, local-only exploration service for the Voxie Across India map.
/// Manages landmark sticker collections, fun facts, and state learning progress
/// strictly inside the user's browser with zero backend data transmission.
/// </summary>
public class IndiaMapService : IIndiaMapService
{
    private const string StorageKey = "voxie_india_explorer_passport";
    private readonly ISaveService _saveService;
    private readonly IAudioService _audioService;

    private IndiaPassportProgress? _cachedPassport;

    public event Action? OnPassportChanged;

    public IndiaMapService(ISaveService saveService, IAudioService audioService)
    {
        _saveService = saveService;
        _audioService = audioService;
    }

    public async Task<IndiaPassportProgress> GetPassportProgressAsync()
    {
        if (_cachedPassport != null)
        {
            return _cachedPassport;
        }

        try
        {
            var saved = await _saveService.GetItemAsync<IndiaPassportProgress>(StorageKey);
            if (saved != null)
            {
                _cachedPassport = saved;
                return saved;
            }
        }
        catch
        {
            // Graceful fallback on fresh session
        }

        _cachedPassport = new IndiaPassportProgress();
        return _cachedPassport;
    }

    public async Task<bool> UnlockStateStickerAsync(string stateCode)
    {
        var cleanCode = IndiaStateRegistry.NormalizeCode(stateCode);
        if (!IndiaStateRegistry.States.ContainsKey(cleanCode))
        {
            return false;
        }

        var passport = await GetPassportProgressAsync();
        bool isNewUnlock = passport.UnlockedStateCodes.Add(cleanCode);

        passport.LastExploredStateCode = cleanCode;
        passport.LastUnlockedUtc = DateTime.UtcNow;

        await _saveService.SetItemAsync(StorageKey, passport);

        if (isNewUnlock)
        {
            await _audioService.PlaySoundAsync(SoundEffect.SuccessChime);
        }

        OnPassportChanged?.Invoke();
        return isNewUnlock;
    }

    public async Task<bool> IsStateUnlockedAsync(string stateCode)
    {
        var passport = await GetPassportProgressAsync();
        var cleanCode = IndiaStateRegistry.NormalizeCode(stateCode);
        return passport.UnlockedStateCodes.Contains(cleanCode);
    }

    public async Task ResetPassportAsync()
    {
        _cachedPassport = new IndiaPassportProgress();
        await _saveService.SetItemAsync(StorageKey, _cachedPassport);
        OnPassportChanged?.Invoke();
    }

    public IndiaStateDefinition? GetStateDetails(string stateCode)
    {
        var cleanCode = IndiaStateRegistry.NormalizeCode(stateCode);
        return IndiaStateRegistry.States.TryGetValue(cleanCode, out var state) ? state : null;
    }

    public IReadOnlyList<IndiaStateDefinition> GetAllStates()
    {
        return IndiaStateRegistry.States.Values.ToList();
    }
}
