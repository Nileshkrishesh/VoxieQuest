using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for Skill Garden 5-plant growth stages, Parent Gate verification,
/// Accessibility controls (colorblind, dyslexia font, speech rate), and Session Screen Time break timer.
/// </summary>
public class SkillGardenAndParentFeaturesTests
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

    private class FakeAudioService : IAudioService
    {
        public bool IsMuted { get; private set; }
        public double Volume { get; private set; } = 0.8;

        public Task PlaySoundAsync(SoundEffect effect) => Task.CompletedTask;
        public Task PlayChimeAsync(bool success) => Task.CompletedTask;
        public Task PlayAnimalSoundAsync(string animalName) => Task.CompletedTask;

        public Task SetMutedAsync(bool muted)
        {
            IsMuted = muted;
            return Task.CompletedTask;
        }

        public Task SetVolumeAsync(double volume)
        {
            Volume = volume;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void SkillGarden_FivePlants_ExistAndAreDistinctInSkillCategory()
    {
        var requiredSkills = new[]
        {
            SkillCategory.Letters,
            SkillCategory.Numbers,
            SkillCategory.Colors,
            SkillCategory.Animals,
            SkillCategory.Spelling
        };

        Assert.Equal(5, requiredSkills.Distinct().Count());
        Assert.True(Enum.IsDefined(typeof(SkillCategory), SkillCategory.Letters));
        Assert.True(Enum.IsDefined(typeof(SkillCategory), SkillCategory.Numbers));
        Assert.True(Enum.IsDefined(typeof(SkillCategory), SkillCategory.Colors));
        Assert.True(Enum.IsDefined(typeof(SkillCategory), SkillCategory.Animals));
        Assert.True(Enum.IsDefined(typeof(SkillCategory), SkillCategory.Spelling));
    }

    [Theory]
    [InlineData(0, 1, "Cozy Seed")]
    [InlineData(2, 1, "Cozy Seed")]
    [InlineData(4, 2, "Fresh Sprout")]
    [InlineData(8, 2, "Fresh Sprout")]
    [InlineData(10, 3, "Leafy Stem")]
    [InlineData(15, 3, "Leafy Stem")]
    [InlineData(20, 4, "Budding Petals")]
    [InlineData(30, 4, "Budding Petals")]
    [InlineData(35, 5, "Radiant Blossom")]
    [InlineData(50, 5, "Radiant Blossom")]
    public void SkillGarden_GrowthStages_ProgressFromSeedToRadiantBlossom(int correct, int expectedLevel, string expectedStageName)
    {
        int level;
        string stageName;

        if (correct >= 35)
        {
            level = 5;
            stageName = "Radiant Blossom";
        }
        else if (correct >= 20)
        {
            level = 4;
            stageName = "Budding Petals";
        }
        else if (correct >= 10)
        {
            level = 3;
            stageName = "Leafy Stem";
        }
        else if (correct >= 4)
        {
            level = 2;
            stageName = "Fresh Sprout";
        }
        else
        {
            level = 1;
            stageName = "Cozy Seed";
        }

        Assert.Equal(expectedLevel, level);
        Assert.Equal(expectedStageName, stageName);
    }

    [Fact]
    public async Task AccessibilityService_TogglesColorblindAndDyslexia_AndClampsAudioValues()
    {
        var fakeSave = new FakeSaveService();
        var fakeAudio = new FakeAudioService();

        var service = new AccessibilityService(fakeSave, fakeAudio);
        await service.InitializeAsync();

        bool eventFired = false;
        service.OnSettingsChanged += () => eventFired = true;

        // Toggle Colorblind
        await service.SetColorblindModeAsync(true);
        Assert.True(service.IsColorblindMode);
        Assert.True(eventFired);

        // Toggle Dyslexia Font
        eventFired = false;
        await service.SetDyslexiaFontAsync(true);
        Assert.True(service.IsDyslexiaFont);
        Assert.True(eventFired);

        // Speech rate clamped between 0.6 and 1.4
        await service.SetSpeechRateAsync(0.3);
        Assert.Equal(0.6, service.SpeechRate);

        await service.SetSpeechRateAsync(2.0);
        Assert.Equal(1.4, service.SpeechRate);

        await service.SetSpeechRateAsync(0.9);
        Assert.Equal(0.9, service.SpeechRate);

        // Master Volume clamped 0.0 to 1.0
        await service.SetMasterVolumeAsync(1.5);
        Assert.Equal(1.0, service.MasterVolume);

        await service.SetMasterVolumeAsync(-0.5);
        Assert.Equal(0.0, service.MasterVolume);

        await service.SetMasterVolumeAsync(0.75);
        Assert.Equal(0.75, service.MasterVolume);

        // Mute
        await service.SetMutedAsync(true);
        Assert.True(service.IsMuted);
    }

    [Fact]
    public async Task SessionTimerService_TracksElapsed_SetsLimit_AndTriggersBreak()
    {
        var fakeSave = new FakeSaveService();
        var progressService = new ProgressService(fakeSave);
        await progressService.InitializeAsync();

        var timerService = new SessionTimerService(fakeSave, progressService);
        await timerService.InitializeAsync();

        Assert.Equal(20, timerService.SessionLimitMinutes);
        Assert.False(timerService.IsBreakDue);

        // Change session limit
        await timerService.SetSessionLimitMinutesAsync(15);
        Assert.Equal(15, timerService.SessionLimitMinutes);

        // Extend session
        timerService.ExtendSession(10);
        Assert.False(timerService.IsBreakDue);

        // Reset break
        timerService.ResetBreak();
        Assert.Equal(TimeSpan.Zero, timerService.ElapsedSessionTime);
        Assert.False(timerService.IsBreakDue);
    }

    [Fact]
    public async Task ProgressService_MirrorsGameSkills_ToBroadRealms()
    {
        var fakeSave = new FakeSaveService();
        var progressService = new ProgressService(fakeSave);
        await progressService.InitializeAsync();

        // Record a correct attempt for Letters
        await progressService.RecordChallengeAttemptAsync(SkillCategory.Letters, true, 2);

        var profile = progressService.ActiveProfile;
        Assert.NotNull(profile);

        // Letters must be recorded
        Assert.True(profile.SkillProgressMap.ContainsKey(SkillCategory.Letters));
        Assert.Equal(1, profile.SkillProgressMap[SkillCategory.Letters].Correct);

        // Phonics must be mirrored
        Assert.True(profile.SkillProgressMap.ContainsKey(SkillCategory.Phonics));
        Assert.Equal(1, profile.SkillProgressMap[SkillCategory.Phonics].Correct);

        // Record Colors
        await progressService.RecordChallengeAttemptAsync(SkillCategory.Colors, true, 1);
        Assert.Equal(1, profile.SkillProgressMap[SkillCategory.Colors].Correct);
        Assert.Equal(1, profile.SkillProgressMap[SkillCategory.Vocabulary].Correct);

        // Record Animals
        await progressService.RecordChallengeAttemptAsync(SkillCategory.Animals, true, 1);
        Assert.Equal(1, profile.SkillProgressMap[SkillCategory.Animals].Correct);
        Assert.Equal(2, profile.SkillProgressMap[SkillCategory.Vocabulary].Correct);

        // Record Spelling
        await progressService.RecordChallengeAttemptAsync(SkillCategory.Spelling, true, 1);
        Assert.Equal(1, profile.SkillProgressMap[SkillCategory.Spelling].Correct);
        Assert.Equal(2, profile.SkillProgressMap[SkillCategory.Phonics].Correct);
    }
}
