using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service contract for child-safe, effort-based Weekly Leagues.
/// Operates strictly within private rooms with zero stranger rankings and 150 daily point caps.
/// </summary>
public interface IWeeklyLeagueService
{
    /// <summary>Fired when league points or room configuration changes.</summary>
    event Action? OnLeagueUpdated;

    /// <summary>Retrieves the current player's league profile.</summary>
    Task<WeeklyLeagueProfile> GetProfileAsync();

    /// <summary>Enables or disables league participation (parent-gated).</summary>
    Task EnableLeagueAsync(bool enable);

    /// <summary>Joins a private room using a 6-character code.</summary>
    Task<bool> JoinRoomAsync(string roomCode);

    /// <summary>Leaves the current room.</summary>
    Task LeaveRoomAsync();

    /// <summary>Generates a new private room code for family or classroom.</summary>
    Task<string> CreateRoomAsync(bool isTeacherMode = false, int classGoal = 500);

    /// <summary>Awards effort points for an action, respecting the 150 daily cap.</summary>
    Task<int> AwardPointsAsync(LeagueScoreAction action, string? gameId = null);

    /// <summary>Fetches the room leaderboard (Top 5 + user's position only).</summary>
    Task<LeagueRoomSummary> GetRoomSummaryAsync();

    /// <summary>Fetches the cooperative Team India national challenge metrics.</summary>
    Task<TeamIndiaChallenge> GetTeamIndiaChallengeAsync();

    /// <summary>Permanently deletes all stored league records for this install.</summary>
    Task DeleteAllLeagueDataAsync();

    /// <summary>Toggles whether the under-13 simplified star view applies.</summary>
    Task SetUnder13Async(bool under13);

    /// <summary>Regenerates a safe display name (e.g. 'Swift Koala 42').</summary>
    Task RegenerateSafeNameAsync();

    /// <summary>Selects a preset avatar icon.</summary>
    Task SetAvatarAsync(string avatarId);

    /// <summary>Gets the current ISO week ID (e.g. 2026-W46).</summary>
    string GetCurrentWeekId(DateTime? date = null);

    /// <summary>Maps a week ID to a friendly botanical league theme (e.g. 'Sunflower League').</summary>
    string GetLeagueThemeName(string weekId);

    /// <summary>Calculates week-over-week percentage points growth.</summary>
    int CalculateGrowthPercentage(string currentWeekId, string previousWeekId);
}
