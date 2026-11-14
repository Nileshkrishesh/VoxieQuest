using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Child-safe educational streak management service with rest-day streak freezes,
/// milestone rewards, and local device date tracking.
/// </summary>
public interface IStreakService
{
    /// <summary>
    /// Current consecutive active streak days.
    /// </summary>
    int CurrentStreak { get; }

    /// <summary>
    /// Longest streak ever achieved by the child profile.
    /// </summary>
    int LongestStreak { get; }

    /// <summary>
    /// Total unique calendar days played.
    /// </summary>
    int TotalDaysPlayed { get; }

    /// <summary>
    /// Whether the weekly streak freeze (rest day) is currently available.
    /// </summary>
    bool IsFreezeAvailable { get; }

    /// <summary>
    /// Whether a streak freeze was used today to preserve the child's streak.
    /// </summary>
    bool WasFreezeUsedToday { get; }

    /// <summary>
    /// Positive, non-judgmental child encouragement message from Voxie.
    /// </summary>
    string GentleEncouragementMessage { get; }

    /// <summary>
    /// Fired whenever the streak or daily activity changes.
    /// </summary>
    event Action? OnStreakUpdated;

    /// <summary>
    /// Fired when a streak milestone (3, 7, 14, 30 days) is unlocked for the first time.
    /// </summary>
    event Action<StreakMilestoneEventArgs>? OnMilestoneReached;

    /// <summary>
    /// Fired when a streak freeze is consumed to protect the streak.
    /// </summary>
    event Action<string>? OnFreezeUsed;

    /// <summary>
    /// Initializes the streak state from localStorage for the active child profile.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Records a completed educational game round for the current day.
    /// </summary>
    Task RecordRoundPlayedAsync(int starsEarned = 1);

    /// <summary>
    /// Records activity for an explicit date (useful for unit tests and deterministic simulation).
    /// </summary>
    Task RecordPlayForDateAsync(DateOnly date, int rounds, int stars);

    /// <summary>
    /// Retrieves activity stats for a specific date.
    /// </summary>
    DailyActivity? GetActivityForDate(DateOnly date);

    /// <summary>
    /// Returns the complete read-only dictionary of played dates.
    /// </summary>
    IReadOnlyDictionary<string, DailyActivity> GetActivityHistory();

    /// <summary>
    /// Returns the 7 days of the current week (Monday to Sunday) with play status.
    /// </summary>
    List<StreakDayInfo> GetCurrentWeekDays();

    /// <summary>
    /// Returns the 12-week matrix (12 columns x 7 rows) for the GitHub-style streak calendar.
    /// </summary>
    List<StreakWeekColumn> GetTwelveWeeksMatrix();

    /// <summary>
    /// Computes child play consistency percentage for the last 4 weeks.
    /// </summary>
    double GetWeeklyConsistencyPercentage();

    /// <summary>
    /// Injects a custom date provider for unit testing without relying on system clock.
    /// </summary>
    void SetCurrentDateProvider(Func<DateOnly> provider);

    /// <summary>
    /// Resets the date provider to the local device date.
    /// </summary>
    void ResetToDefaultDateProvider();
}
