using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service managing the static, private "Voxie Across India" exploration map.
/// Zero backend, zero user tracking, zero data transmission.
/// Children explore Indian states, learn fun facts, and collect landmark stickers in their passport.
/// </summary>
public interface IIndiaMapService
{
    /// <summary>Loads the child's local India Sticker Passport from browser local storage.</summary>
    Task<IndiaPassportProgress> GetPassportProgressAsync();

    /// <summary>Unlocks a state landmark sticker after solving the state mini-game.</summary>
    Task<bool> UnlockStateStickerAsync(string stateCode);

    /// <summary>Checks whether a specific state sticker has been unlocked.</summary>
    Task<bool> IsStateUnlockedAsync(string stateCode);

    /// <summary>Resets passport progress back to 0 stickers.</summary>
    Task ResetPassportAsync();

    /// <summary>Retrieves exploration metadata for a specific state code.</summary>
    IndiaStateDefinition? GetStateDetails(string stateCode);

    /// <summary>Retrieves all 36 Indian states and union territories.</summary>
    IReadOnlyList<IndiaStateDefinition> GetAllStates();

    /// <summary>Fired when passport stickers are earned or updated.</summary>
    event Action? OnPassportChanged;
}
