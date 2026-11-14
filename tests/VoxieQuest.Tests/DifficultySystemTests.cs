using System.Net;
using System.Text.Json;
using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for the complete Level and Adaptive Difficulty system in Voxie Quest.
/// Validates micro-level 1-100 dynamics, botanical tiers, flow-zone scaffolding,
/// session drop constraints, JSON fallbacks, and picture-book trail progressions.
/// </summary>
public class DifficultySystemTests
{
    private readonly InMemorySaveService _saveService;
    private readonly DifficultyService _service;

    public DifficultySystemTests()
    {
        _saveService = new InMemorySaveService();
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        _service = new DifficultyService(_saveService, httpClient);
    }

    [Fact]
    public void MicroLevelRanges_MapToCorrectBotanicalTiers()
    {
        var seedlingState = new SkillDifficultyState(SkillCategory.Numbers, 20);
        Assert.Equal(DifficultyTier.Seedling, seedlingState.CurrentTier);
        Assert.Equal("Seedling", seedlingState.CurrentTier.ToFriendlyName());
        Assert.Equal("🌱", seedlingState.CurrentTier.ToIcon());

        var saplingState = new SkillDifficultyState(SkillCategory.Numbers, 50);
        Assert.Equal(DifficultyTier.Sapling, saplingState.CurrentTier);
        Assert.Equal("Sapling", saplingState.CurrentTier.ToFriendlyName());
        Assert.Equal("🌿", saplingState.CurrentTier.ToIcon());

        var bigTreeState = new SkillDifficultyState(SkillCategory.Numbers, 85);
        Assert.Equal(DifficultyTier.BigTree, bigTreeState.CurrentTier);
        Assert.Equal("Big Tree", bigTreeState.CurrentTier.ToFriendlyName());
        Assert.Equal("🌳", bigTreeState.CurrentTier.ToIcon());
    }

    [Fact]
    public async Task ConsecutiveCorrect_RaisesMicroLevel()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Letters;
        var state = _service.GetSkillState(skill);
        state.MicroLevel = 20;

        // 1st correct
        await _service.RecordAttemptAsync(skill, true, 3.0, false);
        Assert.Equal(20, state.MicroLevel);

        // 2nd correct
        await _service.RecordAttemptAsync(skill, true, 3.0, false);
        Assert.Equal(20, state.MicroLevel);

        // 3rd correct -> triggers +3 promotion
        var result = await _service.RecordAttemptAsync(skill, true, 3.0, false);
        Assert.Equal(23, state.MicroLevel);
        Assert.Equal(AdaptiveSignal.RaiseMicroLevel, result.Signal);
        Assert.Contains("garden has a new flower", result.FeedbackMessage);
    }

    [Fact]
    public async Task FastConsecutiveCorrect_AwardsSpeedBonus()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Letters;
        var state = _service.GetSkillState(skill);
        state.MicroLevel = 20;

        // 3 very fast answers (< 2.0s)
        await _service.RecordAttemptAsync(skill, true, 1.2, false);
        await _service.RecordAttemptAsync(skill, true, 1.4, false);
        var result = await _service.RecordAttemptAsync(skill, true, 1.1, false);

        // Expect +5 promotion instead of +3
        Assert.Equal(25, state.MicroLevel);
        Assert.Equal(AdaptiveSignal.RaiseMicroLevel, result.Signal);
    }

    [Fact]
    public async Task ConsecutiveMisses_AddsHelpFirst_ThenLowersLevelSlowly()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Numbers;
        var state = _service.GetSkillState(skill);
        state.MicroLevel = 45;

        // Miss 1: Silent help activated, NO level drop
        var r1 = await _service.RecordAttemptAsync(skill, false, 2.5, false);
        Assert.True(state.IsSilentHelpActive);
        Assert.True(r1.SilentHelpProvided);
        Assert.Equal(45, state.MicroLevel);

        // Miss 2: Still silent help, NO level drop
        var r2 = await _service.RecordAttemptAsync(skill, false, 2.5, false);
        Assert.True(state.IsSilentHelpActive);
        Assert.Equal(45, state.MicroLevel);

        // Miss 3: Ease difficulty by -3, warm-up offered, NO failure words
        var r3 = await _service.RecordAttemptAsync(skill, false, 2.5, false);
        Assert.Equal(42, state.MicroLevel);
        Assert.Equal(AdaptiveSignal.EaseDifficulty, r3.Signal);
        Assert.Contains("Let's try a warm-up", r3.FeedbackMessage);
        Assert.DoesNotContain("wrong", r3.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fail", r3.FeedbackMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SessionDropConstraint_NeverDropsMoreThanOneTierPerSession()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Colors;
        var state = _service.GetSkillState(skill);
        
        // Starts at BigTree in this session
        state.MicroLevel = 75;
        state.StartingSessionTier = DifficultyTier.BigTree;

        // Force repeated misses: floor must not drop below Sapling (min 36)
        for (int i = 0; i < 60; i++)
        {
            await _service.RecordAttemptAsync(skill, false, 3.0, false);
        }

        Assert.True(state.MicroLevel >= 36, $"Expected micro-level >= 36, got {state.MicroLevel}");
        Assert.Equal(DifficultyTier.Sapling, state.CurrentTier);
    }

    [Fact]
    public async Task MicroLevel_StaysWithinOneToHundredBounds()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Spelling;
        var state = _service.GetSkillState(skill);

        // Test upper bound
        state.MicroLevel = 98;
        await _service.RecordAttemptAsync(skill, true, 1.0, false);
        await _service.RecordAttemptAsync(skill, true, 1.0, false);
        await _service.RecordAttemptAsync(skill, true, 1.0, false);
        Assert.Equal(100, state.MicroLevel);

        // Test lower bound
        state.MicroLevel = 2;
        state.StartingSessionTier = DifficultyTier.Seedling;
        await _service.RecordAttemptAsync(skill, false, 2.0, false);
        await _service.RecordAttemptAsync(skill, false, 2.0, false);
        await _service.RecordAttemptAsync(skill, false, 2.0, false);
        Assert.Equal(1, state.MicroLevel);
    }

    [Fact]
    public async Task BoredomSignal_TriggersChallengeRound()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Animals;

        // 5 consecutive fast answers (< 2.5s)
        await _service.RecordAttemptAsync(skill, true, 1.5, false);
        await _service.RecordAttemptAsync(skill, true, 1.6, false);
        await _service.RecordAttemptAsync(skill, true, 1.4, false);
        await _service.RecordAttemptAsync(skill, true, 1.3, false);
        var r5 = await _service.RecordAttemptAsync(skill, true, 1.5, false);

        Assert.True(r5.OfferChallenge);
        Assert.Equal(AdaptiveSignal.OfferBoredomChallenge, r5.Signal);
        Assert.Contains("Speed champion", r5.FeedbackMessage);
    }

    [Fact]
    public async Task FrustrationSignal_RepeatedSkips_OffersVoxieBreak()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Letters;

        await _service.RecordSkipOrTimeoutAsync(skill);
        var r2 = await _service.RecordSkipOrTimeoutAsync(skill);

        Assert.True(r2.OfferBreak);
        Assert.Equal(AdaptiveSignal.OfferFrustrationBreak, r2.Signal);
        Assert.Contains("warm-up", r2.FeedbackMessage);
    }

    [Fact]
    public async Task LevelUnlockOrder_UnlocksSequentialStops_AndBossRounds()
    {
        await _service.InitializeAsync();
        var skill = SkillCategory.Numbers;
        var state = _service.GetSkillState(skill);

        // Level 1 starts unlocked, Level 2 is locked
        Assert.True(state.Levels[1].IsUnlocked);
        Assert.False(state.Levels[2].IsUnlocked);

        // Complete Level 1 with 3 stars
        await _service.CompleteLevelAsync(skill, 1, 3, 4.2);

        Assert.True(state.Levels[1].IsCompleted);
        Assert.Equal(3, state.Levels[1].StarsEarned);
        Assert.True(state.Levels[2].IsUnlocked);

        // Boss level 5 check
        Assert.True(state.Levels[5].IsBossRound);
        Assert.NotNull(state.Levels[5].SpecialSticker);

        // Parent Unlock All
        await _service.UnlockAllLevelsAsync(skill);
        Assert.True(state.AllLevelsUnlocked);
        for (int i = 1; i <= 10; i++)
        {
            Assert.True(state.Levels[i].IsUnlocked);
        }
    }

    [Fact]
    public async Task MissingDifficultyJson_FallsBackToDefaultsGracefully()
    {
        var config = await _service.GetGameConfigAsync("nonexistent-game");
        Assert.NotNull(config);
        Assert.NotNull(config.GetTier(DifficultyTier.Seedling));
        Assert.NotNull(config.GetTier(DifficultyTier.Sapling));
        Assert.NotNull(config.GetTier(DifficultyTier.BigTree));
    }
}

public class InMemorySaveService : ISaveService
{
    private readonly Dictionary<string, object> _store = new();

    public Task<T?> GetItemAsync<T>(string key)
    {
        if (_store.TryGetValue(key, out var val))
        {
            if (val is T typed) return Task.FromResult<T?>(typed);
            var json = JsonSerializer.Serialize(val);
            return Task.FromResult(JsonSerializer.Deserialize<T>(json));
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetItemAsync<T>(string key, T value)
    {
        _store[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Return 404 to exercise graceful fallback
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
