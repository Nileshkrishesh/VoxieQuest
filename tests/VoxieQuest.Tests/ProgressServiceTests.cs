using VoxieQuest.Web.Models;
using VoxieQuest.Web.Services;
using Xunit;

namespace VoxieQuest.Tests;

/// <summary>
/// Unit tests for <see cref="ProgressService"/> verifying profiles, stars, and skill progress.
/// </summary>
public class ProgressServiceTests
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

    [Fact]
    public async Task InitializeAsync_SeedsInitialDefaultProfile()
    {
        var fakeSave = new FakeSaveService();
        var service = new ProgressService(fakeSave);

        await service.InitializeAsync();

        Assert.NotEmpty(service.Profiles);
        Assert.NotNull(service.ActiveProfile);
        Assert.Equal("Little Explorer", service.ActiveProfile.Name);
    }

    [Fact]
    public async Task CreateProfileAsync_EnforcesMaximum4Profiles()
    {
        var fakeSave = new FakeSaveService();
        var service = new ProgressService(fakeSave);
        await service.InitializeAsync(); // Has 1 seeded profile

        await service.CreateProfileAsync("Child 2", AvatarType.LeoLion, 6);
        await service.CreateProfileAsync("Child 3", AvatarType.PipPenguin, 7);
        await service.CreateProfileAsync("Child 4", AvatarType.MiaMonkey, 8);

        Assert.Equal(4, service.Profiles.Count);

        // 5th profile creation must throw InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProfileAsync("Child 5", AvatarType.VoxieChameleon, 5));
    }

    [Fact]
    public async Task RecordChallengeAttemptAsync_UpdatesAccuracyAndStars()
    {
        var fakeSave = new FakeSaveService();
        var service = new ProgressService(fakeSave);
        await service.InitializeAsync();

        var initialStars = service.ActiveProfile!.TotalStars;

        // Record 1 correct attempt
        await service.RecordChallengeAttemptAsync(SkillCategory.Phonics, true, starsEarned: 2);

        var progress = service.ActiveProfile.SkillProgressMap[SkillCategory.Phonics];
        Assert.Equal(1, progress.Attempts);
        Assert.Equal(1, progress.Correct);
        Assert.Equal(100.0, progress.AccuracyPercentage);
        Assert.Equal(initialStars + 2, service.ActiveProfile.TotalStars);

        // Record 1 missed attempt
        await service.RecordChallengeAttemptAsync(SkillCategory.Phonics, false);
        Assert.Equal(2, progress.Attempts);
        Assert.Equal(1, progress.Correct);
        Assert.Equal(50.0, progress.AccuracyPercentage);
    }
}
