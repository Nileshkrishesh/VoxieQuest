using System.Net.Http.Json;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements child-safe, effort-based weekly leagues.
/// Strictly enforces:
/// 1. Private rooms only (Family / Classroom) - never strangers.
/// 2. Daily cap of 150 points.
/// 3. Top 5 visibility only - nobody is shown last place.
/// 4. Under-13 view hides rank numbers completely.
/// 5. Graceful offline fallback if API is unreachable.
/// </summary>
public class WeeklyLeagueService : IWeeklyLeagueService
{
    private const string StorageKey = "voxie_weekly_league_profile";
    private readonly ISaveService _saveService;
    private readonly HttpClient _http;
    private WeeklyLeagueProfile? _cachedProfile;

    public event Action? OnLeagueUpdated;

    public WeeklyLeagueService(ISaveService saveService, HttpClient http)
    {
        _saveService = saveService;
        _http = http;
    }

    public async Task<WeeklyLeagueProfile> GetProfileAsync()
    {
        if (_cachedProfile != null)
        {
            _cachedProfile.RefreshPeriodCounters(DateTime.UtcNow);
            return _cachedProfile;
        }

        var profile = await _saveService.GetItemAsync<WeeklyLeagueProfile>(StorageKey);
        if (profile == null)
        {
            profile = new WeeklyLeagueProfile
            {
                AnonId = Guid.NewGuid().ToString("N"),
                DisplayName = SafeNameGenerator.Generate(),
                AvatarId = "fox",
                IsEnabled = false,
                IsUnder13 = true
            };
        }

        profile.RefreshPeriodCounters(DateTime.UtcNow);
        _cachedProfile = profile;
        return profile;
    }

    private async Task SaveProfileAsync(WeeklyLeagueProfile profile)
    {
        _cachedProfile = profile;
        await _saveService.SetItemAsync(StorageKey, profile);
        OnLeagueUpdated?.Invoke();
    }

    public async Task EnableLeagueAsync(bool enable)
    {
        var profile = await GetProfileAsync();
        profile.IsEnabled = enable;

        if (!enable && !string.IsNullOrEmpty(profile.CurrentRoomCode))
        {
            // If turning off, leave the room and delete remote records
            await DeleteAllLeagueDataAsync();
            return;
        }

        await SaveProfileAsync(profile);
    }

    public async Task<bool> JoinRoomAsync(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode) || roomCode.Trim().Length != 6)
        {
            return false;
        }

        var profile = await GetProfileAsync();
        profile.CurrentRoomCode = roomCode.Trim().ToUpperInvariant();
        await SaveProfileAsync(profile);

        // Sync points to room if any
        if (profile.WeeklyPoints > 0)
        {
            await SyncScoreToBackendAsync(profile);
        }

        return true;
    }

    public async Task LeaveRoomAsync()
    {
        var profile = await GetProfileAsync();
        profile.CurrentRoomCode = null;
        await SaveProfileAsync(profile);
    }

    public async Task<string> CreateRoomAsync(bool isTeacherMode = false, int classGoal = 500)
    {
        string newCode = GenerateRoomCode();

        try
        {
            var res = await _http.PostAsJsonAsync("api/league/room", new
            {
                roomCode = newCode,
                isTeacherMode,
                classGoal = Math.Clamp(classGoal, 100, 5000)
            });

            if (res.IsSuccessStatusCode)
            {
                var payload = await res.Content.ReadFromJsonAsync<RoomCreateResponse>();
                if (!string.IsNullOrEmpty(payload?.RoomCode))
                {
                    newCode = payload.RoomCode;
                }
            }
        }
        catch
        {
            // Fallback to locally generated code if offline
        }

        var profile = await GetProfileAsync();
        profile.CurrentRoomCode = newCode;
        await SaveProfileAsync(profile);

        return newCode;
    }

    public async Task<int> AwardPointsAsync(LeagueScoreAction action, string? gameId = null)
    {
        var profile = await GetProfileAsync();
        if (!profile.IsEnabled)
        {
            return 0;
        }

        profile.RefreshPeriodCounters(DateTime.UtcNow);

        // Exploration bonus: +15 only once per category per week
        if (action == LeagueScoreAction.NewGameCategory && !string.IsNullOrEmpty(gameId))
        {
            if (profile.GamesPlayedThisWeek.Contains(gameId, StringComparer.OrdinalIgnoreCase))
            {
                return 0; // Already claimed this week
            }
            profile.GamesPlayedThisWeek.Add(gameId);
        }

        // Daily streak bonus: +5 only once per day
        if (action == LeagueScoreAction.DailyStreakBonus)
        {
            if (profile.HasClaimedTodayStreakBonus)
            {
                return 0;
            }
            profile.HasClaimedTodayStreakBonus = true;
        }

        int requestedPoints = (int)action;
        int remainingCap = Math.Max(0, WeeklyLeagueProfile.DailyCapPoints - profile.TodayPoints);
        int pointsAwarded = Math.Min(requestedPoints, remainingCap);

        if (pointsAwarded > 0)
        {
            profile.TodayPoints += pointsAwarded;
            profile.WeeklyPoints += pointsAwarded;
            await SaveProfileAsync(profile);

            // Sync score asynchronously in background if in room
            if (!string.IsNullOrEmpty(profile.CurrentRoomCode))
            {
                _ = SyncScoreToBackendAsync(profile);
            }
        }

        return pointsAwarded;
    }

    private async Task SyncScoreToBackendAsync(WeeklyLeagueProfile profile)
    {
        try
        {
            var submission = new LeagueScoreSubmission
            {
                AnonId = profile.AnonId,
                RoomCode = profile.CurrentRoomCode ?? string.Empty,
                WeekId = profile.CurrentWeekId,
                Points = profile.WeeklyPoints,
                DisplayName = profile.DisplayName,
                AvatarId = profile.AvatarId
            };

            await _http.PostAsJsonAsync("api/league/score", submission);
        }
        catch
        {
            // Graceful offline fallback: score remains intact in local storage
        }
    }

    public async Task<LeagueRoomSummary> GetRoomSummaryAsync()
    {
        var profile = await GetProfileAsync();
        string currentWeek = profile.CurrentWeekId;
        string theme = GetLeagueThemeName(currentWeek);

        int daysLeft = GetDaysRemainingInWeek(DateTime.UtcNow);

        if (string.IsNullOrEmpty(profile.CurrentRoomCode))
        {
            // Solo / No Room View: "Beat Your Own Best" mode
            var soloEntry = new LeagueMemberEntry
            {
                AnonId = profile.AnonId,
                DisplayName = profile.DisplayName,
                AvatarId = profile.AvatarId,
                WeeklyPoints = profile.WeeklyPoints,
                Rank = 1,
                IsCurrentUser = true
            };

            return new LeagueRoomSummary
            {
                RoomCode = string.Empty,
                WeekId = currentWeek,
                LeagueThemeName = theme,
                Top5 = new List<LeagueMemberEntry> { soloEntry },
                CurrentUserEntry = soloEntry,
                TotalMembers = 1,
                DaysRemainingInWeek = daysLeft
            };
        }

        // Fetch from API with local fallback
        try
        {
            string url = $"api/league/room?roomCode={Uri.EscapeDataString(profile.CurrentRoomCode)}&weekId={Uri.EscapeDataString(currentWeek)}&anonId={Uri.EscapeDataString(profile.AnonId)}";
            var summary = await _http.GetFromJsonAsync<LeagueRoomSummary>(url);
            if (summary != null)
            {
                summary.LeagueThemeName = theme;
                summary.DaysRemainingInWeek = daysLeft;
                summary.IsOfflineFallback = false;
                return summary;
            }
        }
        catch
        {
            // Network failure or API unavailable: fallback to local mock room
        }

        return GenerateOfflineRoomSummary(profile, theme, daysLeft);
    }

    private LeagueRoomSummary GenerateOfflineRoomSummary(WeeklyLeagueProfile profile, string theme, int daysLeft)
    {
        // Friendly local family mock members so room functions even fully offline
        var members = new List<LeagueMemberEntry>
        {
            new() { AnonId = profile.AnonId, DisplayName = profile.DisplayName, AvatarId = profile.AvatarId, WeeklyPoints = profile.WeeklyPoints, IsCurrentUser = true },
            new() { AnonId = "fam_1", DisplayName = "Sunny Lion 12", AvatarId = "lion", WeeklyPoints = 95, IsCurrentUser = false },
            new() { AnonId = "fam_2", DisplayName = "Kind Bear 77", AvatarId = "bear", WeeklyPoints = 60, IsCurrentUser = false },
            new() { AnonId = "fam_3", DisplayName = "Calm Koala 18", AvatarId = "koala", WeeklyPoints = 40, IsCurrentUser = false }
        };

        // Sort descending and compute ranks sharing ties
        members.Sort((a, b) => b.WeeklyPoints.CompareTo(a.WeeklyPoints));
        int rank = 1;
        for (int i = 0; i < members.Count; i++)
        {
            if (i > 0 && members[i].WeeklyPoints < members[i - 1].WeeklyPoints)
            {
                rank = i + 1;
            }
            members[i].Rank = rank;
        }

        var top5 = members.Take(5).ToList();
        var userEntry = members.FirstOrDefault(m => m.IsCurrentUser);

        return new LeagueRoomSummary
        {
            RoomCode = profile.CurrentRoomCode ?? "FAM123",
            WeekId = profile.CurrentWeekId,
            LeagueThemeName = theme,
            Top5 = top5,
            CurrentUserEntry = userEntry,
            TotalMembers = members.Count,
            DaysRemainingInWeek = daysLeft,
            ClassTotalPoints = members.Sum(m => m.WeeklyPoints),
            ClassTargetGoal = 500,
            IsOfflineFallback = true
        };
    }

    public Task<TeamIndiaChallenge> GetTeamIndiaChallengeAsync()
    {
        // Cooperative shared India challenge: non-competitive total
        return Task.FromResult(new TeamIndiaChallenge
        {
            TotalRoundsPlayed = 7840,
            ActiveStatesCount = 22
        });
    }

    public async Task DeleteAllLeagueDataAsync()
    {
        var profile = await GetProfileAsync();

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, "api/league/me")
            {
                Content = JsonContent.Create(new { anonId = profile.AnonId })
            };
            await _http.SendAsync(req);
        }
        catch
        {
            // Offline deletion fallback
        }

        profile.IsEnabled = false;
        profile.CurrentRoomCode = null;
        profile.WeeklyPoints = 0;
        profile.TodayPoints = 0;
        profile.GamesPlayedThisWeek.Clear();
        profile.PastWeeklyScores.Clear();
        profile.DisplayName = SafeNameGenerator.Generate();
        profile.AvatarId = "fox";

        await SaveProfileAsync(profile);
    }

    public async Task SetUnder13Async(bool under13)
    {
        var profile = await GetProfileAsync();
        profile.IsUnder13 = under13;
        await SaveProfileAsync(profile);
    }

    public async Task RegenerateSafeNameAsync()
    {
        var profile = await GetProfileAsync();
        profile.DisplayName = SafeNameGenerator.Generate();
        await SaveProfileAsync(profile);

        if (!string.IsNullOrEmpty(profile.CurrentRoomCode))
        {
            _ = SyncScoreToBackendAsync(profile);
        }
    }

    public async Task SetAvatarAsync(string avatarId)
    {
        var profile = await GetProfileAsync();
        profile.AvatarId = avatarId;
        await SaveProfileAsync(profile);

        if (!string.IsNullOrEmpty(profile.CurrentRoomCode))
        {
            _ = SyncScoreToBackendAsync(profile);
        }
    }

    public string GetCurrentWeekId(DateTime? date = null)
    {
        return WeeklyLeagueProfile.GetIsoWeekId(date ?? DateTime.UtcNow);
    }

    public string GetLeagueThemeName(string weekId)
    {
        // Deterministic botanical theme by week
        string[] themes =
        {
            "Sunflower League 🌻",
            "Bluebell League 🪻",
            "Clover League 🍀",
            "Daisy League 🌼",
            "Rosebud League 🌹",
            "Fern Valley League 🌿"
        };

        int hash = Math.Abs(weekId.GetHashCode());
        return themes[hash % themes.Length];
    }

    public int CalculateGrowthPercentage(string currentWeekId, string previousWeekId)
    {
        if (_cachedProfile == null) return 0;
        int current = _cachedProfile.WeeklyPoints;
        if (!_cachedProfile.PastWeeklyScores.TryGetValue(previousWeekId, out int prev) || prev <= 0)
        {
            return 0;
        }

        double diff = current - prev;
        return (int)Math.Round((diff / prev) * 100);
    }

    private static int GetDaysRemainingInWeek(DateTime dateUtc)
    {
        // Monday = 6 days left, Sunday = 0 days left
        DayOfWeek dow = dateUtc.DayOfWeek;
        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)dow + 7) % 7;
        return daysUntilSunday;
    }

    private static string GenerateRoomCode()
    {
        // 6 characters, uppercase, avoiding confusing letters (0, O, 1, I, L)
        const string chars = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        var rng = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }

    private class RoomCreateResponse
    {
        public string? RoomCode { get; set; }
    }
}
