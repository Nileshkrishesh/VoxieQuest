using System.Globalization;

namespace VoxieQuest.Web.Models;

/// <summary>
/// Effort-based scoring actions for the Weekly League.
/// Speed and accuracy do not alter points to ensure equal accessibility.
/// </summary>
public enum LeagueScoreAction
{
    RoundCompleted = 10,
    LevelFinished = 25,
    DailyStreakBonus = 5,
    NewGameCategory = 15,
    HintUsed = 10
}

/// <summary>
/// Presets of friendly animal avatars available for selection.
/// </summary>
public static class PresetAvatars
{
    public static readonly IReadOnlyList<AvatarOption> All = new List<AvatarOption>
    {
        new("fox", "🦊", "Voxie Fox", "#FF9F43"),
        new("panda", "🐼", "Brave Panda", "#4ECDC4"),
        new("lion", "🦁", "Sunny Lion", "#FFD166"),
        new("bunny", "🐰", "Swift Bunny", "#FF6B6B"),
        new("owl", "🦉", "Wise Owl", "#6BCB77"),
        new("bear", "🐻", "Kind Bear", "#4D96FF"),
        new("koala", "🐨", "Calm Koala", "#A8DADC"),
        new("tiger", "🐯", "Gentle Tiger", "#F4A261")
    };

    public static AvatarOption GetById(string? id) =>
        All.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}

public record AvatarOption(string Id, string Emoji, string Name, string ColorHex);

/// <summary>
/// Safe random display name generator producing non-identifiable names (e.g. 'Brave Panda 42').
/// </summary>
public static class SafeNameGenerator
{
    private static readonly string[] Adjectives =
    {
        "Brave", "Sunny", "Swift", "Happy", "Kind", "Clever",
        "Calm", "Gentle", "Curious", "Bright", "Playful", "Quiet"
    };

    private static readonly string[] Animals =
    {
        "Panda", "Fox", "Koala", "Bunny", "Lion", "Owl",
        "Bear", "Tiger", "Dolphin", "Otter", "Robin", "Deer"
    };

    public static string Generate(Random? rng = null)
    {
        rng ??= Random.Shared;
        string adj = Adjectives[rng.Next(Adjectives.Length)];
        string animal = Animals[rng.Next(Animals.Length)];
        int num = rng.Next(10, 99);
        return $"{adj} {animal} {num}";
    }

    public static bool IsSafeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;
        return Adjectives.Contains(parts[0], StringComparer.OrdinalIgnoreCase) &&
               Animals.Contains(parts[1], StringComparer.OrdinalIgnoreCase) &&
               int.TryParse(parts[2], out int n) && n >= 10 && n <= 99;
    }
}

/// <summary>
/// Child-safe weekly league profile persisted locally in browser localStorage.
/// </summary>
public class WeeklyLeagueProfile
{
    public const int DailyCapPoints = 150;

    /// <summary>Whether the child participates in weekly leagues (off by default).</summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>Anonymous install ID unlinked from real child profile.</summary>
    public string AnonId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>6-character private family/classroom room code (null if not in room).</summary>
    public string? CurrentRoomCode { get; set; }

    /// <summary>Auto-generated child display name from safe word list.</summary>
    public string DisplayName { get; set; } = SafeNameGenerator.Generate();

    /// <summary>Preset avatar identifier.</summary>
    public string AvatarId { get; set; } = "fox";

    /// <summary>Under-13 child view hide rank numbers and shows only stars.</summary>
    public bool IsUnder13 { get; set; } = true;

    /// <summary>Current ISO week identifier (e.g. 2026-W46).</summary>
    public string CurrentWeekId { get; set; } = GetIsoWeekId(DateTime.UtcNow);

    /// <summary>Points accumulated today (max 150 daily cap).</summary>
    public int TodayPoints { get; set; } = 0;

    /// <summary>Date corresponding to TodayPoints ('yyyy-MM-dd').</summary>
    public string TodayDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    /// <summary>Total points accumulated this week.</summary>
    public int WeeklyPoints { get; set; } = 0;

    /// <summary>List of mini-game category IDs played this week for the +15 exploration bonus.</summary>
    public List<string> GamesPlayedThisWeek { get; set; } = new();

    /// <summary>Whether the daily streak bonus (+5) was awarded today.</summary>
    public bool HasClaimedTodayStreakBonus { get; set; } = false;

    /// <summary>History of past 8 weeks: weekId -> points.</summary>
    public Dictionary<string, int> PastWeeklyScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Personal best weekly score ever achieved.</summary>
    public int PersonalBestWeekPoints { get; set; } = 0;

    /// <summary>Last celebrated week identifier.</summary>
    public string? LastCelebratedWeekId { get; set; }

    /// <summary>Computes the ISO 8601 week ID (e.g., 2026-W46) starting Monday.</summary>
    public static string GetIsoWeekId(DateTime date)
    {
        var cal = CultureInfo.InvariantCulture.Calendar;
        var day = cal.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        int week = cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{date.Year}-W{week:D2}";
    }

    /// <summary>
    /// Checks and rolls over daily and weekly point counters.
    /// </summary>
    public void RefreshPeriodCounters(DateTime nowUtc)
    {
        string today = nowUtc.ToString("yyyy-MM-dd");
        if (TodayDate != today)
        {
            TodayPoints = 0;
            TodayDate = today;
            HasClaimedTodayStreakBonus = false;
        }

        string currentWeek = GetIsoWeekId(nowUtc);
        if (CurrentWeekId != currentWeek)
        {
            if (!string.IsNullOrEmpty(CurrentWeekId) && WeeklyPoints > 0)
            {
                PastWeeklyScores[CurrentWeekId] = WeeklyPoints;
                if (WeeklyPoints > PersonalBestWeekPoints)
                {
                    PersonalBestWeekPoints = WeeklyPoints;
                }
            }
            CurrentWeekId = currentWeek;
            WeeklyPoints = 0;
            GamesPlayedThisWeek.Clear();
        }
    }
}

/// <summary>
/// A single member row in a private room.
/// </summary>
public class LeagueMemberEntry
{
    public string AnonId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarId { get; set; } = "fox";
    public int WeeklyPoints { get; set; }
    public int Rank { get; set; }
    public bool IsCurrentUser { get; set; }
    public string StarRating => WeeklyPoints switch
    {
        >= 300 => "⭐⭐⭐",
        >= 150 => "⭐⭐",
        >= 50 => "⭐",
        _ => "🌱"
    };
}

/// <summary>
/// Aggregated summary of a private league room.
/// Hides all positions except Top 5 and caller's own position.
/// </summary>
public class LeagueRoomSummary
{
    public string RoomCode { get; set; } = string.Empty;
    public string WeekId { get; set; } = string.Empty;
    public string LeagueThemeName { get; set; } = "Sunflower League";
    public List<LeagueMemberEntry> Top5 { get; set; } = new();
    public LeagueMemberEntry? CurrentUserEntry { get; set; }
    public int TotalMembers { get; set; }
    public int DaysRemainingInWeek { get; set; }
    public int ClassTargetGoal { get; set; } = 500;
    public int ClassTotalPoints { get; set; }
    public bool IsClassGoalMet => ClassTotalPoints >= ClassTargetGoal;
    public bool IsOfflineFallback { get; set; } = false;
}

/// <summary>
/// Cooperative national challenge metrics across all opted-in Indian states.
/// Non-competitive, shared goal for all children.
/// </summary>
public class TeamIndiaChallenge
{
    public const int NationalTargetRounds = 10000;
    public int TotalRoundsPlayed { get; set; } = 7420;
    public int TargetRounds => NationalTargetRounds;
    public int ProgressPercentage => Math.Min(100, (int)Math.Round((double)TotalRoundsPlayed / TargetRounds * 100));
    public bool IsGoalUnlocked => TotalRoundsPlayed >= TargetRounds;
    public int ActiveStatesCount { get; set; } = 18;
}

/// <summary>
/// Weekly score submission payload for POST /api/league/score.
/// Only allowed fields are transmitted.
/// </summary>
public class LeagueScoreSubmission
{
    public string AnonId { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string WeekId { get; set; } = string.Empty;
    public int Points { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarId { get; set; } = "fox";
}
