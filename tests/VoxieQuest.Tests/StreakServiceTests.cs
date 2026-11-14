using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for <see cref="StreakService"/> verifying streak increments,
/// weekly streak freezes, reset tolerance, longest streak memory, calendar matrix,
/// and milestone event emission.
/// </summary>
public class StreakServiceTests
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

    private async Task<(StreakService streakService, ProgressService progressService)> CreateInitializedServicesAsync()
    {
        var fakeSave = new FakeSaveService();
        var progressService = new ProgressService(fakeSave);
        await progressService.InitializeAsync();

        var streakService = new StreakService(fakeSave, progressService);
        await streakService.InitializeAsync();

        return (streakService, progressService);
    }

    [Fact]
    public async Task ConsecutiveDays_IncreaseStreak()
    {
        var (streakService, _) = await CreateInitializedServicesAsync();

        var d1 = new DateOnly(2026, 11, 1);
        streakService.SetCurrentDateProvider(() => d1);
        await streakService.RecordRoundPlayedAsync(3);
        Assert.Equal(1, streakService.CurrentStreak);
        Assert.Equal(1, streakService.TotalDaysPlayed);

        var d2 = new DateOnly(2026, 11, 2);
        streakService.SetCurrentDateProvider(() => d2);
        await streakService.RecordRoundPlayedAsync(2);
        Assert.Equal(2, streakService.CurrentStreak);
        Assert.Equal(2, streakService.TotalDaysPlayed);

        var d3 = new DateOnly(2026, 11, 3);
        streakService.SetCurrentDateProvider(() => d3);
        await streakService.RecordRoundPlayedAsync(5);
        Assert.Equal(3, streakService.CurrentStreak);
        Assert.Equal(3, streakService.TotalDaysPlayed);
    }

    [Fact]
    public async Task GapOfOneDay_WithFreezeAvailable_KeepsStreak()
    {
        var (streakService, _) = await CreateInitializedServicesAsync();

        // Day 1
        var d1 = new DateOnly(2026, 11, 1);
        streakService.SetCurrentDateProvider(() => d1);
        await streakService.RecordRoundPlayedAsync(2);
        Assert.Equal(1, streakService.CurrentStreak);
        Assert.True(streakService.IsFreezeAvailable);

        // Gap of 1 day (missed 2026-11-02), play on 2026-11-03
        var d3 = new DateOnly(2026, 11, 3);
        streakService.SetCurrentDateProvider(() => d3);
        await streakService.RecordRoundPlayedAsync(4);

        // Streak Freeze preserves and increments streak to 2
        Assert.Equal(2, streakService.CurrentStreak);
        Assert.True(streakService.WasFreezeUsedToday);
        Assert.False(streakService.IsFreezeAvailable); // Consumed for the rolling 7 days
    }

    [Fact]
    public async Task GapOfTwoOrMoreDays_ResetsStreak()
    {
        var (streakService, _) = await CreateInitializedServicesAsync();

        // Day 1 & Day 2
        var d1 = new DateOnly(2026, 11, 1);
        streakService.SetCurrentDateProvider(() => d1);
        await streakService.RecordRoundPlayedAsync(3);

        var d2 = new DateOnly(2026, 11, 2);
        streakService.SetCurrentDateProvider(() => d2);
        await streakService.RecordRoundPlayedAsync(3);
        Assert.Equal(2, streakService.CurrentStreak);

        // Gap of 2 missed days (missed 2026-11-03 and 2026-11-04), plays on 2026-11-05
        var d5 = new DateOnly(2026, 11, 5);
        streakService.SetCurrentDateProvider(() => d5);
        await streakService.RecordRoundPlayedAsync(2);

        // Resets gently to 1
        Assert.Equal(1, streakService.CurrentStreak);
        Assert.Equal("Voxie missed you! Let's start a new adventure today", streakService.GentleEncouragementMessage);
    }

    [Fact]
    public async Task LongestStreak_StaysCorrect_AfterReset()
    {
        var (streakService, _) = await CreateInitializedServicesAsync();

        // 5 consecutive days
        for (int i = 1; i <= 5; i++)
        {
            var date = new DateOnly(2026, 11, i);
            streakService.SetCurrentDateProvider(() => date);
            await streakService.RecordRoundPlayedAsync(2);
        }

        Assert.Equal(5, streakService.CurrentStreak);
        Assert.Equal(5, streakService.LongestStreak);

        // Skip 4 days, play again on Nov 10
        var resetDate = new DateOnly(2026, 11, 10);
        streakService.SetCurrentDateProvider(() => resetDate);
        await streakService.RecordRoundPlayedAsync(1);

        // Current streak restarts at 1, but LongestStreak remains 5
        Assert.Equal(1, streakService.CurrentStreak);
        Assert.Equal(5, streakService.LongestStreak);
    }

    [Fact]
    public async Task SameDay_PlayedTwice_CountsOnce()
    {
        var (streakService, _) = await CreateInitializedServicesAsync();

        var today = new DateOnly(2026, 11, 14);
        streakService.SetCurrentDateProvider(() => today);

        // First round
        await streakService.RecordRoundPlayedAsync(3);
        Assert.Equal(1, streakService.CurrentStreak);
        Assert.Equal(1, streakService.TotalDaysPlayed);

        // Second round on same day
        await streakService.RecordRoundPlayedAsync(4);
        Assert.Equal(1, streakService.CurrentStreak);
        Assert.Equal(1, streakService.TotalDaysPlayed);

        // Verify daily activity recorded cumulative rounds and stars
        var activity = streakService.GetActivityForDate(today);
        Assert.NotNull(activity);
        Assert.Equal(2, activity.RoundsPlayed);
        Assert.Equal(7, activity.StarsEarned);
    }

    [Fact]
    public async Task MonthAndYearBoundary_HandlesStreakCorrectly()
    {
        var (streakService, _) = await CreateInitializedServicesAsync();

        // New Year's Eve: 2026-12-31
        var nye = new DateOnly(2026, 12, 31);
        streakService.SetCurrentDateProvider(() => nye);
        await streakService.RecordRoundPlayedAsync(2);
        Assert.Equal(1, streakService.CurrentStreak);

        // New Year's Day: 2027-01-01
        var nyd = new DateOnly(2027, 1, 1);
        streakService.SetCurrentDateProvider(() => nyd);
        await streakService.RecordRoundPlayedAsync(3);
        Assert.Equal(2, streakService.CurrentStreak);

        // Next Day: 2027-01-02
        var nextDay = new DateOnly(2027, 1, 2);
        streakService.SetCurrentDateProvider(() => nextDay);
        await streakService.RecordRoundPlayedAsync(1);
        Assert.Equal(3, streakService.CurrentStreak);
        Assert.Equal(3, streakService.LongestStreak);
    }

    [Fact]
    public async Task MilestoneEvent_FiresExactlyOnce()
    {
        var (streakService, _) = await CreateInitializedServicesAsync();

        int milestone3FiredCount = 0;
        StreakMilestoneEventArgs? receivedArgs = null;

        streakService.OnMilestoneReached += args =>
        {
            if (args.MilestoneDays == 3)
            {
                milestone3FiredCount++;
                receivedArgs = args;
            }
        };

        // Day 1
        var d1 = new DateOnly(2026, 11, 1);
        streakService.SetCurrentDateProvider(() => d1);
        await streakService.RecordRoundPlayedAsync(1);

        // Day 2
        var d2 = new DateOnly(2026, 11, 2);
        streakService.SetCurrentDateProvider(() => d2);
        await streakService.RecordRoundPlayedAsync(1);
        Assert.Equal(0, milestone3FiredCount);

        // Day 3 -> reaches 3-day milestone!
        var d3 = new DateOnly(2026, 11, 3);
        streakService.SetCurrentDateProvider(() => d3);
        await streakService.RecordRoundPlayedAsync(1);
        Assert.Equal(1, milestone3FiredCount);
        Assert.NotNull(receivedArgs);
        Assert.Equal("streak_3", receivedArgs.BadgeId);

        // Play again on Day 3 -> milestone should NOT fire again
        await streakService.RecordRoundPlayedAsync(2);
        Assert.Equal(1, milestone3FiredCount);

        // Day 4 -> milestone 3 should still NOT fire again
        var d4 = new DateOnly(2026, 11, 4);
        streakService.SetCurrentDateProvider(() => d4);
        await streakService.RecordRoundPlayedAsync(1);
        Assert.Equal(1, milestone3FiredCount);
    }
}
