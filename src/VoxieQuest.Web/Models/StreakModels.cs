namespace VoxieQuest.Web.Models;

/// <summary>
/// Represents activity metrics recorded for a single calendar day.
/// </summary>
public class DailyActivity
{
    public int RoundsPlayed { get; set; }
    public int StarsEarned { get; set; }
    public DateTime LastPlayedTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Child-safe profile streak data persisted in localStorage.
/// </summary>
public class ProfileStreakData
{
    public string ProfileId { get; set; } = "default";
    public Dictionary<string, DailyActivity> History { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalDaysPlayed { get; set; }
    public string? LastPlayedDate { get; set; } // yyyy-MM-dd
    public string? LastFreezeDate { get; set; } // yyyy-MM-dd
    public List<int> UnlockedMilestones { get; set; } = new();
}

/// <summary>
/// Event arguments delivered when a streak milestone is reached (e.g. 3, 7, 14, 30 days).
/// </summary>
public class StreakMilestoneEventArgs : EventArgs
{
    public int MilestoneDays { get; }
    public string BadgeId { get; }
    public string Title { get; }
    public string Message { get; }

    public StreakMilestoneEventArgs(int milestoneDays, string badgeId, string title, string message)
    {
        MilestoneDays = milestoneDays;
        BadgeId = badgeId;
        Title = title;
        Message = message;
    }
}

/// <summary>
/// Lightweight view model representing a single day in the weekly or 12-week calendar grid.
/// </summary>
public class StreakDayInfo
{
    public DateOnly Date { get; set; }
    public string DateKey => Date.ToString("yyyy-MM-dd");
    public int RoundsPlayed { get; set; }
    public int StarsEarned { get; set; }
    public bool IsPlayed => RoundsPlayed > 0;
    public bool IsToday { get; set; }
    public bool IsFreezeDay { get; set; }
    public int IntensityLevel => RoundsPlayed switch
    {
        0 => 0,
        1 or 2 => 1,
        >= 3 and <= 5 => 2,
        >= 6 and <= 9 => 3,
        _ => 4
    };
}

/// <summary>
/// Column representation for the 12-week GitHub-style calendar grid (7 days per week).
/// </summary>
public class StreakWeekColumn
{
    public int WeekNumber { get; set; }
    public string? MonthLabel { get; set; }
    public List<StreakDayInfo> Days { get; set; } = new();
}
