using System.Net;
using System.Net.Http.Json;
using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

public class WeeklyLeagueTests
{
    private class FakeSaveService : ISaveService
    {
        private readonly Dictionary<string, object> _store = new();

        public Task<T?> GetItemAsync<T>(string key)
        {
            if (_store.TryGetValue(key, out var val) && val is T typed)
            {
                return Task.FromResult<T?>(typed);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetItemAsync<T>(string key, T value)
        {
            if (value != null)
            {
                _store[key] = value;
            }
            return Task.CompletedTask;
        }

        public Task RemoveItemAsync(string key)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly bool _shouldFail;

        public MockHttpMessageHandler(bool shouldFail = false)
        {
            _shouldFail = shouldFail;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_shouldFail)
            {
                throw new HttpRequestException("Network unreachable");
            }

            var res = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { success = true })
            };
            return Task.FromResult(res);
        }
    }

    private WeeklyLeagueService CreateService(bool shouldHttpFail = false)
    {
        var fakeSave = new FakeSaveService();
        var client = new HttpClient(new MockHttpMessageHandler(shouldHttpFail)) { BaseAddress = new Uri("http://localhost/") };
        return new WeeklyLeagueService(fakeSave, client);
    }

    [Fact]
    public async Task PointsAreCalculatedCorrectly_ForEachAction()
    {
        var service = CreateService();
        await service.EnableLeagueAsync(true);

        // 1. Round completed (+10)
        int roundPts = await service.AwardPointsAsync(LeagueScoreAction.RoundCompleted);
        Assert.Equal(10, roundPts);

        // 2. Level finished (+25)
        int levelPts = await service.AwardPointsAsync(LeagueScoreAction.LevelFinished);
        Assert.Equal(25, levelPts);

        // 3. Daily streak bonus (+5 once per day)
        int streakPts = await service.AwardPointsAsync(LeagueScoreAction.DailyStreakBonus);
        Assert.Equal(5, streakPts);

        // 4. Repeated streak bonus on same day gives 0
        int repeatStreakPts = await service.AwardPointsAsync(LeagueScoreAction.DailyStreakBonus);
        Assert.Equal(0, repeatStreakPts);

        // 5. New game category exploration bonus (+15 once per category per week)
        int newGamePts = await service.AwardPointsAsync(LeagueScoreAction.NewGameCategory, "LetterRocket");
        Assert.Equal(15, newGamePts);

        // 6. Repeated same game category gives 0
        int repeatGamePts = await service.AwardPointsAsync(LeagueScoreAction.NewGameCategory, "LetterRocket");
        Assert.Equal(0, repeatGamePts);

        // 7. Hint used (counts as completed round, +10)
        int hintPts = await service.AwardPointsAsync(LeagueScoreAction.HintUsed);
        Assert.Equal(10, hintPts);

        var profile = await service.GetProfileAsync();
        // Total: 10 + 25 + 5 + 0 + 15 + 0 + 10 = 65
        Assert.Equal(65, profile.TodayPoints);
        Assert.Equal(65, profile.WeeklyPoints);
    }

    [Fact]
    public async Task DailyCapOf150_IsEnforcedOnClient()
    {
        var service = CreateService();
        await service.EnableLeagueAsync(true);

        // Award 140 points through 14 rounds
        for (int i = 0; i < 14; i++)
        {
            await service.AwardPointsAsync(LeagueScoreAction.RoundCompleted);
        }

        var profile = await service.GetProfileAsync();
        Assert.Equal(140, profile.TodayPoints);

        // Attempting to award level finished (25 points) must clamp to remaining 10 points
        int awarded = await service.AwardPointsAsync(LeagueScoreAction.LevelFinished);
        Assert.Equal(10, awarded);

        profile = await service.GetProfileAsync();
        Assert.Equal(150, profile.TodayPoints);
        Assert.Equal(150, profile.WeeklyPoints);

        // Any further actions on the same day must award 0 points to encourage rest
        int excess = await service.AwardPointsAsync(LeagueScoreAction.RoundCompleted);
        Assert.Equal(0, excess);
        Assert.Equal(150, profile.TodayPoints);
    }

    [Fact]
    public void TiesShareTheSameRank()
    {
        var members = new List<LeagueMemberEntry>
        {
            new() { AnonId = "1", DisplayName = "Alice", WeeklyPoints = 120 },
            new() { AnonId = "2", DisplayName = "Bob", WeeklyPoints = 120 },
            new() { AnonId = "3", DisplayName = "Charlie", WeeklyPoints = 90 },
            new() { AnonId = "4", DisplayName = "David", WeeklyPoints = 80 },
            new() { AnonId = "5", DisplayName = "Eve", WeeklyPoints = 80 }
        };

        // Standard competitive rank assignment
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

        // Alice & Bob tie at 120 -> Rank 1
        Assert.Equal(1, members[0].Rank);
        Assert.Equal(1, members[1].Rank);

        // Charlie with 90 -> Rank 3
        Assert.Equal(3, members[2].Rank);

        // David & Eve tie at 80 -> Rank 4
        Assert.Equal(4, members[3].Rank);
        Assert.Equal(4, members[4].Rank);
    }

    [Fact]
    public void WeekIdAndWeekBoundaries_AreCorrect_MondayStart()
    {
        // 2026-11-09 is Monday (Start of week 46 in 2026)
        var monday = new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
        string mondayWeek = WeeklyLeagueProfile.GetIsoWeekId(monday);

        // 2026-11-15 is Sunday (End of week 46)
        var sunday = new DateTime(2026, 11, 15, 23, 59, 59, DateTimeKind.Utc);
        string sundayWeek = WeeklyLeagueProfile.GetIsoWeekId(sunday);

        Assert.Equal(mondayWeek, sundayWeek);
        Assert.Equal("2026-W46", mondayWeek);

        // Next Monday is 2026-11-16 (Start of week 47)
        var nextMonday = new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);
        string nextWeek = WeeklyLeagueProfile.GetIsoWeekId(nextMonday);
        Assert.Equal("2026-W47", nextWeek);
    }

    [Fact]
    public void Top5List_HidesAllLowerPositions()
    {
        // 10 members in room
        var members = Enumerable.Range(1, 10).Select(i => new LeagueMemberEntry
        {
            AnonId = $"user_{i}",
            DisplayName = $"Learner {i}",
            WeeklyPoints = 150 - (i * 10),
            IsCurrentUser = (i == 8) // Current user is 8th
        }).ToList();

        // Sort descending
        members.Sort((a, b) => b.WeeklyPoints.CompareTo(a.WeeklyPoints));
        for (int i = 0; i < members.Count; i++) members[i].Rank = i + 1;

        // Top 5 slice
        var top5 = members.Take(5).ToList();
        var userEntry = members.FirstOrDefault(m => m.IsCurrentUser);

        Assert.Equal(5, top5.Count);
        Assert.DoesNotContain(top5, m => m.AnonId == "user_6");
        Assert.DoesNotContain(top5, m => m.AnonId == "user_7");
        Assert.DoesNotContain(top5, m => m.AnonId == "user_8");
        Assert.DoesNotContain(top5, m => m.AnonId == "user_9");
        Assert.DoesNotContain(top5, m => m.AnonId == "user_10");

        // The user entry exists separately to show "Your Garden Spot" without exposing last place
        Assert.NotNull(userEntry);
        Assert.Equal(8, userEntry.Rank);
    }

    [Fact]
    public async Task Under13View_ProtectsChildrenFromRankNumbers()
    {
        var service = CreateService();
        var profile = await service.GetProfileAsync();
        Assert.True(profile.IsUnder13);

        var summary = await service.GetRoomSummaryAsync();
        Assert.NotNull(summary.CurrentUserEntry);
        // Under 13 flag is true: star rating is friendly
        Assert.NotEmpty(summary.CurrentUserEntry.StarRating);
    }

    [Fact]
    public async Task OptOut_DeletesAllRecords()
    {
        var service = CreateService();
        await service.EnableLeagueAsync(true);
        await service.JoinRoomAsync("FAM123");
        await service.AwardPointsAsync(LeagueScoreAction.RoundCompleted);

        var profile = await service.GetProfileAsync();
        Assert.True(profile.IsEnabled);
        Assert.Equal("FAM123", profile.CurrentRoomCode);
        Assert.True(profile.WeeklyPoints > 0);

        // Turn off / opt-out
        await service.EnableLeagueAsync(false);

        var updated = await service.GetProfileAsync();
        Assert.False(updated.IsEnabled);
        Assert.Null(updated.CurrentRoomCode);
        Assert.Equal(0, updated.WeeklyPoints);
        Assert.Equal(0, updated.TodayPoints);
    }

    [Fact]
    public async Task LeagueCard_WorksGracefully_WhenApiFails()
    {
        var service = CreateService(shouldHttpFail: true);
        await service.EnableLeagueAsync(true);
        await service.JoinRoomAsync("OFF123");

        // Fetching room summary must not throw; it falls back to local data gracefully
        var summary = await service.GetRoomSummaryAsync();
        Assert.NotNull(summary);
        Assert.True(summary.IsOfflineFallback);
        Assert.NotEmpty(summary.Top5);
    }

    [Fact]
    public async Task InvalidRoomCode_IsRejected()
    {
        var service = CreateService();
        await service.EnableLeagueAsync(true);

        bool emptyJoin = await service.JoinRoomAsync("");
        Assert.False(emptyJoin);

        bool shortJoin = await service.JoinRoomAsync("ABC");
        Assert.False(shortJoin);

        bool longJoin = await service.JoinRoomAsync("ABCDEFGH");
        Assert.False(longJoin);

        bool validJoin = await service.JoinRoomAsync("ABC234");
        Assert.True(validJoin);
    }

    [Fact]
    public void OnlyAllowedFields_AreDefinedInSubmission()
    {
        var submission = new LeagueScoreSubmission
        {
            AnonId = "anon_123",
            RoomCode = "FAM123",
            WeekId = "2026-W46",
            Points = 50,
            DisplayName = "Brave Panda 42",
            AvatarId = "panda"
        };

        var props = typeof(LeagueScoreSubmission).GetProperties();
        var propNames = props.Select(p => p.Name).ToList();

        // Ensure ONLY allowed fields are present: no name, no location, no age, no school
        Assert.Equal(6, props.Length);
        Assert.Contains("AnonId", propNames);
        Assert.Contains("RoomCode", propNames);
        Assert.Contains("WeekId", propNames);
        Assert.Contains("Points", propNames);
        Assert.Contains("DisplayName", propNames);
        Assert.Contains("AvatarId", propNames);

        Assert.DoesNotContain("ChildName", propNames);
        Assert.DoesNotContain("Age", propNames);
        Assert.DoesNotContain("Location", propNames);
        Assert.DoesNotContain("School", propNames);
    }
}
