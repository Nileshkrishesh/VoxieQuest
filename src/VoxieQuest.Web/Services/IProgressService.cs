using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service managing child profiles, badges, stars, skill accuracies, and play durations.
/// Supports up to 4 local child profiles saved in browser localStorage.
/// </summary>
public interface IProgressService
{
    /// <summary>
    /// Gets all registered child profiles (maximum 4).
    /// </summary>
    IReadOnlyList<ChildProfile> Profiles { get; }

    /// <summary>
    /// Gets the currently active child profile.
    /// </summary>
    ChildProfile? ActiveProfile { get; }

    /// <summary>
    /// Event triggered when profiles or active profile state changes.
    /// </summary>
    event Action? OnProfileChanged;

    /// <summary>
    /// Loads profiles from local storage or seeds an initial child profile.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Creates a new child profile if fewer than 4 profiles exist.
    /// </summary>
    /// <param name="name">Child's name.</param>
    /// <param name="avatar">Selected avatar.</param>
    /// <param name="age">Child's age.</param>
    /// <returns>The newly created profile.</returns>
    Task<ChildProfile> CreateProfileAsync(string name, AvatarType avatar, int age);

    /// <summary>
    /// Selects the active profile by identifier.
    /// </summary>
    /// <param name="profileId">Profile unique identifier.</param>
    Task SelectProfileAsync(string profileId);

    /// <summary>
    /// Updates the attributes of an existing profile and persists changes.
    /// </summary>
    /// <param name="profile">Updated profile object.</param>
    Task UpdateProfileAsync(ChildProfile profile);

    /// <summary>
    /// Deletes a profile by identifier.
    /// </summary>
    /// <param name="profileId">Profile to remove.</param>
    Task DeleteProfileAsync(string profileId);

    /// <summary>
    /// Records an educational challenge attempt against the active child profile.
    /// </summary>
    /// <param name="category">The educational skill realm.</param>
    /// <param name="isCorrect">Whether the child succeeded.</param>
    /// <param name="starsEarned">Number of stars to grant on success.</param>
    Task RecordChallengeAttemptAsync(SkillCategory category, bool isCorrect, int starsEarned = 1);

    /// <summary>
    /// Increments the total playtime for the active profile.
    /// </summary>
    /// <param name="seconds">Elapsed seconds to add.</param>
    Task AddPlayTimeSecondsAsync(long seconds);

    /// <summary>
    /// Grants a badge to the active profile if not already unlocked.
    /// </summary>
    /// <param name="badgeId">Badge identifier code.</param>
    Task AwardBadgeAsync(string badgeId);
}
