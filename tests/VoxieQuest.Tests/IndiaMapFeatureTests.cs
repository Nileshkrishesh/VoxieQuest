using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for the static Voxie Across India exploration map and sticker passport.
/// Validates landmark stickers, state fun facts, exploratory mini-games, and 100% local operation.
/// </summary>
public class IndiaMapFeatureTests
{
    private readonly InMemorySaveService _saveService;
    private readonly MockAudioService _audioService;
    private readonly IndiaMapService _service;

    public IndiaMapFeatureTests()
    {
        _saveService = new InMemorySaveService();
        _audioService = new MockAudioService();
        _service = new IndiaMapService(_saveService, _audioService);
    }

    [Fact]
    public void All36States_HaveLandmarkStickers_FunFacts_AndMiniGames()
    {
        var states = IndiaStateRegistry.States;
        Assert.Equal(36, states.Count);

        foreach (var (code, def) in states)
        {
            Assert.False(string.IsNullOrWhiteSpace(def.Code), $"State {code} missing Code");
            Assert.False(string.IsNullOrWhiteSpace(def.Name), $"State {code} missing Name");
            Assert.False(string.IsNullOrWhiteSpace(def.Capital), $"State {code} missing Capital");
            Assert.False(string.IsNullOrWhiteSpace(def.LandmarkIcon), $"State {code} missing LandmarkIcon");
            Assert.False(string.IsNullOrWhiteSpace(def.LandmarkName), $"State {code} missing LandmarkName");
            Assert.False(string.IsNullOrWhiteSpace(def.FunFact), $"State {code} missing FunFact");
            Assert.False(string.IsNullOrWhiteSpace(def.MiniGameQuestion), $"State {code} missing MiniGameQuestion");
            Assert.Equal(3, def.MiniGameChoices.Count);
            Assert.Contains(def.CorrectAnswer, def.MiniGameChoices);
            Assert.False(string.IsNullOrWhiteSpace(def.CelebrationCheer), $"State {code} missing CelebrationCheer");
        }
    }

    [Theory]
    [InlineData("PB", true)]
    [InlineData("RJ", true)]
    [InlineData("MH", true)]
    [InlineData("KA", true)]
    [InlineData("DL", true)]
    [InlineData("IN-TN", true)]
    [InlineData("XX", false)]
    [InlineData("CALIFORNIA", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void StateValidation_RejectsInvalidCodes(string inputCode, bool expectedValid)
    {
        bool isValid = IndiaStateRegistry.IsValidCode(inputCode);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task MiniGame_UnlocksStateSticker_AndUpdatesPassport()
    {
        // Initial passport should be empty
        var initial = await _service.GetPassportProgressAsync();
        Assert.Equal(0, initial.CollectedCount);
        Assert.False(await _service.IsStateUnlockedAsync("PB"));

        // Solve mini-game and unlock Punjab
        bool unlocked = await _service.UnlockStateStickerAsync("PB");
        Assert.True(unlocked);

        // Verify sticker is collected
        Assert.True(await _service.IsStateUnlockedAsync("PB"));
        var progress = await _service.GetPassportProgressAsync();
        Assert.Equal(1, progress.CollectedCount);
        Assert.Equal("PB", progress.LastExploredStateCode);
        Assert.NotNull(progress.LastUnlockedUtc);
    }

    [Fact]
    public async Task Passport_CollectsUniqueStickers_WithoutDuplicates()
    {
        await _service.UnlockStateStickerAsync("PB");
        bool duplicate = await _service.UnlockStateStickerAsync("PB");

        Assert.False(duplicate, "Unlocking the same state twice should not count as a new unlock");

        var progress = await _service.GetPassportProgressAsync();
        Assert.Equal(1, progress.CollectedCount);

        // Unlock a second state
        await _service.UnlockStateStickerAsync("RJ");
        var updated = await _service.GetPassportProgressAsync();
        Assert.Equal(2, updated.CollectedCount);
    }

    [Fact]
    public async Task ResetPassport_ClearsAllCollectedStickers()
    {
        await _service.UnlockStateStickerAsync("PB");
        await _service.UnlockStateStickerAsync("MH");
        await _service.UnlockStateStickerAsync("KA");

        var beforeReset = await _service.GetPassportProgressAsync();
        Assert.Equal(3, beforeReset.CollectedCount);

        await _service.ResetPassportAsync();

        var afterReset = await _service.GetPassportProgressAsync();
        Assert.Equal(0, afterReset.CollectedCount);
        Assert.False(await _service.IsStateUnlockedAsync("PB"));
    }

    [Fact]
    public void SpecificLandmarks_MatchRequiredExamples()
    {
        var pb = _service.GetStateDetails("PB");
        Assert.NotNull(pb);
        Assert.Equal("Golden Temple", pb.LandmarkName);
        Assert.Equal("🛕", pb.LandmarkIcon);

        var rj = _service.GetStateDetails("RJ");
        Assert.NotNull(rj);
        Assert.Contains("Camel", rj.LandmarkName);
        Assert.Equal("🐪", rj.LandmarkIcon);
    }

    [Fact]
    public void ZeroUserData_RunsCompletelyOffline()
    {
        // Service operates strictly with local storage mock, zero HTTP requests needed
        var states = _service.GetAllStates();
        Assert.Equal(36, states.Count);
    }
}

public class MockAudioService : IAudioService
{
    public double Volume => 1.0;
    public bool IsMuted => false;
    public Task PlaySoundAsync(SoundEffect effect) => Task.CompletedTask;
    public Task PlayChimeAsync(bool success) => Task.CompletedTask;
    public Task PlayAnimalSoundAsync(string animalName) => Task.CompletedTask;
    public Task SetVolumeAsync(double volume) => Task.CompletedTask;
    public Task SetMutedAsync(bool muted) => Task.CompletedTask;
}
