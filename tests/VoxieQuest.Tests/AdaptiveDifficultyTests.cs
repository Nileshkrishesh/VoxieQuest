using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for the <see cref="AdaptiveDifficultyService"/> verifying 3-in-a-row promotion and 3-misses easing rules.
/// </summary>
public class AdaptiveDifficultyTests
{
    private readonly AdaptiveDifficultyService _service;

    public AdaptiveDifficultyTests()
    {
        _service = new AdaptiveDifficultyService();
    }

    [Fact]
    public void InitialState_StartsAtEasy_WithZeroCounters()
    {
        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveCorrect);
        Assert.Equal(0, _service.ConsecutiveMisses);
    }

    [Fact]
    public void SingleCorrect_IncrementsConsecutiveCorrect_DoesNotChangeDifficulty()
    {
        var changed = _service.RecordAttempt(true);

        Assert.False(changed);
        Assert.Equal(1, _service.ConsecutiveCorrect);
        Assert.Equal(0, _service.ConsecutiveMisses);
        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
    }

    [Fact]
    public void TwoCorrect_DoesNotChangeDifficulty()
    {
        _service.RecordAttempt(true);
        var changed = _service.RecordAttempt(true);

        Assert.False(changed);
        Assert.Equal(2, _service.ConsecutiveCorrect);
        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
    }

    [Fact]
    public void ThreeConsecutiveCorrect_PromotesFromEasyToMedium_AndResetsCounter()
    {
        _service.RecordAttempt(true);
        _service.RecordAttempt(true);
        var changed = _service.RecordAttempt(true);

        Assert.True(changed);
        Assert.Equal(DifficultyLevel.Medium, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveCorrect);
        Assert.Equal(0, _service.ConsecutiveMisses);
    }

    [Fact]
    public void ThreeConsecutiveCorrectFromMedium_PromotesToHard_AndResetsCounter()
    {
        // Reach Medium
        _service.RecordAttempt(true);
        _service.RecordAttempt(true);
        _service.RecordAttempt(true);
        Assert.Equal(DifficultyLevel.Medium, _service.CurrentDifficulty);

        // 3 more correct
        _service.RecordAttempt(true);
        _service.RecordAttempt(true);
        var changed = _service.RecordAttempt(true);

        Assert.True(changed);
        Assert.Equal(DifficultyLevel.Hard, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveCorrect);
    }

    [Fact]
    public void ThreeConsecutiveCorrectFromHard_CapsAtHard_AndResetsCounter()
    {
        // Explicitly set to Hard
        _service.SetDifficulty(DifficultyLevel.Hard);

        _service.RecordAttempt(true);
        _service.RecordAttempt(true);
        var changed = _service.RecordAttempt(true);

        // Cannot promote beyond Hard
        Assert.False(changed);
        Assert.Equal(DifficultyLevel.Hard, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveCorrect);
    }

    [Fact]
    public void SingleMiss_IncrementsConsecutiveMisses_AndResetsConsecutiveCorrect()
    {
        _service.RecordAttempt(true);
        _service.RecordAttempt(true);
        Assert.Equal(2, _service.ConsecutiveCorrect);

        var changed = _service.RecordAttempt(false);

        Assert.False(changed);
        Assert.Equal(0, _service.ConsecutiveCorrect);
        Assert.Equal(1, _service.ConsecutiveMisses);
        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
    }

    [Fact]
    public void ThreeConsecutiveMissesFromHard_DemotesToMedium_AndResetsCounter()
    {
        _service.SetDifficulty(DifficultyLevel.Hard);

        _service.RecordAttempt(false);
        _service.RecordAttempt(false);
        var changed = _service.RecordAttempt(false);

        Assert.True(changed);
        Assert.Equal(DifficultyLevel.Medium, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveMisses);
    }

    [Fact]
    public void ThreeConsecutiveMissesFromMedium_DemotesToEasy_AndResetsCounter()
    {
        _service.SetDifficulty(DifficultyLevel.Medium);

        _service.RecordAttempt(false);
        _service.RecordAttempt(false);
        var changed = _service.RecordAttempt(false);

        Assert.True(changed);
        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveMisses);
    }

    [Fact]
    public void ThreeConsecutiveMissesFromEasy_CapsAtEasy_AndResetsCounter()
    {
        _service.SetDifficulty(DifficultyLevel.Easy);

        _service.RecordAttempt(false);
        _service.RecordAttempt(false);
        var changed = _service.RecordAttempt(false);

        // Cannot ease below Easy
        Assert.False(changed);
        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveMisses);
    }

    [Fact]
    public void AlternatingCorrectAndMiss_NeverTriggersDifficultyChange()
    {
        for (var i = 0; i < 10; i++)
        {
            var r1 = _service.RecordAttempt(true);
            var r2 = _service.RecordAttempt(false);

            Assert.False(r1);
            Assert.False(r2);
        }

        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
    }

    [Fact]
    public void OnDifficultyChangedEvent_FiresOnPromotion()
    {
        DifficultyLevel? reportedLevel = null;
        bool? reportedIsPromotion = null;

        _service.OnDifficultyChanged += (level, isPromotion) =>
        {
            reportedLevel = level;
            reportedIsPromotion = isPromotion;
        };

        _service.RecordAttempt(true);
        _service.RecordAttempt(true);
        _service.RecordAttempt(true);

        Assert.Equal(DifficultyLevel.Medium, reportedLevel);
        Assert.True(reportedIsPromotion);
    }

    [Fact]
    public void OnDifficultyChangedEvent_FiresOnEasing()
    {
        _service.SetDifficulty(DifficultyLevel.Medium);

        DifficultyLevel? reportedLevel = null;
        bool? reportedIsPromotion = null;

        _service.OnDifficultyChanged += (level, isPromotion) =>
        {
            reportedLevel = level;
            reportedIsPromotion = isPromotion;
        };

        _service.RecordAttempt(false);
        _service.RecordAttempt(false);
        _service.RecordAttempt(false);

        Assert.Equal(DifficultyLevel.Easy, reportedLevel);
        Assert.False(reportedIsPromotion);
    }

    [Fact]
    public void Reset_RestoresEasy_AndClearsCounters()
    {
        _service.SetDifficulty(DifficultyLevel.Hard);
        _service.RecordAttempt(true);
        _service.RecordAttempt(true);

        _service.Reset();

        Assert.Equal(DifficultyLevel.Easy, _service.CurrentDifficulty);
        Assert.Equal(0, _service.ConsecutiveCorrect);
        Assert.Equal(0, _service.ConsecutiveMisses);
    }
}
