using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements profile management, star allocation, badge tracking, and local storage synchronization.
/// </summary>
public class ProgressService : IProgressService
{
    private const string StorageKey = "voxie_quest_profiles_v1";
    private const string ActiveProfileIdKey = "voxie_quest_active_profile_id_v1";
    private const int MaxProfiles = 4;

    private readonly ISaveService _saveService;
    private readonly List<ChildProfile> _profiles = new();
    private ChildProfile? _activeProfile;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressService"/> class.
    /// </summary>
    /// <param name="saveService">Persistence service.</param>
    public ProgressService(ISaveService saveService)
    {
        _saveService = saveService;
        var defaultProfile = new ChildProfile
        {
            Name = "Little Explorer",
            Avatar = AvatarType.VoxieChameleon,
            Age = 6,
            TotalStars = 3,
            Badges = new List<string> { "first_step" }
        };
        _profiles.Add(defaultProfile);
        _activeProfile = defaultProfile;
    }

    /// <inheritdoc />
    public IReadOnlyList<ChildProfile> Profiles => _profiles.AsReadOnly();

    /// <inheritdoc />
    public ChildProfile? ActiveProfile => _activeProfile;

    /// <inheritdoc />
    public event Action? OnProfileChanged;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        var loadedProfiles = await _saveService.GetItemAsync<List<ChildProfile>>(StorageKey);
        _profiles.Clear();

        if (loadedProfiles != null && loadedProfiles.Count > 0)
        {
            _profiles.AddRange(loadedProfiles.Take(MaxProfiles));
        }
        else
        {
            // Seed first default child profile
            var defaultProfile = new ChildProfile
            {
                Name = "Little Explorer",
                Avatar = AvatarType.VoxieChameleon,
                Age = 6,
                TotalStars = 3,
                Badges = new List<string> { "first_step" }
            };
            _profiles.Add(defaultProfile);
            await SaveProfilesAsync();
        }

        var activeId = await _saveService.GetItemAsync<string>(ActiveProfileIdKey);
        _activeProfile = _profiles.FirstOrDefault(p => p.Id == activeId) ?? _profiles.FirstOrDefault();

        _isInitialized = true;
        OnProfileChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task<ChildProfile> CreateProfileAsync(string name, AvatarType avatar, int age)
    {
        await EnsureInitializedAsync();

        if (_profiles.Count >= MaxProfiles)
        {
            throw new InvalidOperationException($"Cannot exceed {MaxProfiles} child profiles.");
        }

        var newProfile = new ChildProfile
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Explorer {_profiles.Count + 1}" : name.Trim(),
            Avatar = avatar,
            Age = Math.Clamp(age, 5, 10),
            TotalStars = 0,
            Badges = new List<string> { "first_step" }
        };

        _profiles.Add(newProfile);
        _activeProfile = newProfile;

        await SaveProfilesAsync();
        await _saveService.SetItemAsync(ActiveProfileIdKey, newProfile.Id);

        OnProfileChanged?.Invoke();
        return newProfile;
    }

    /// <inheritdoc />
    public async Task SelectProfileAsync(string profileId)
    {
        await EnsureInitializedAsync();

        var match = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (match != null && match != _activeProfile)
        {
            _activeProfile = match;
            await _saveService.SetItemAsync(ActiveProfileIdKey, match.Id);
            OnProfileChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task UpdateProfileAsync(ChildProfile profile)
    {
        await EnsureInitializedAsync();

        var index = _profiles.FindIndex(p => p.Id == profile.Id);
        if (index >= 0)
        {
            _profiles[index] = profile;
            if (_activeProfile?.Id == profile.Id)
            {
                _activeProfile = profile;
            }
            await SaveProfilesAsync();
            OnProfileChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task DeleteProfileAsync(string profileId)
    {
        await EnsureInitializedAsync();

        var match = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (match != null)
        {
            _profiles.Remove(match);

            if (_profiles.Count == 0)
            {
                // Re-seed if last profile deleted
                var fresh = new ChildProfile
                {
                    Name = "Adventurer",
                    Avatar = AvatarType.VoxieChameleon,
                    Age = 6
                };
                _profiles.Add(fresh);
                _activeProfile = fresh;
            }
            else if (_activeProfile?.Id == profileId)
            {
                _activeProfile = _profiles[0];
            }

            await SaveProfilesAsync();
            if (_activeProfile != null)
            {
                await _saveService.SetItemAsync(ActiveProfileIdKey, _activeProfile.Id);
            }

            OnProfileChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task RecordChallengeAttemptAsync(SkillCategory category, bool isCorrect, int starsEarned = 1)
    {
        await EnsureInitializedAsync();
        if (_activeProfile == null) return;

        RecordSingleSkill(_activeProfile, category, isCorrect);

        // Mirror related categories for cross-tracking (e.g. Letters <-> Phonics, Colors/Animals -> Vocabulary)
        var mirrorCategory = category switch
        {
            SkillCategory.Letters => SkillCategory.Phonics,
            SkillCategory.Phonics => SkillCategory.Letters,
            SkillCategory.Colors => SkillCategory.Vocabulary,
            SkillCategory.Animals => SkillCategory.Vocabulary,
            SkillCategory.Spelling => SkillCategory.Phonics,
            _ => (SkillCategory?)null
        };

        if (mirrorCategory.HasValue)
        {
            RecordSingleSkill(_activeProfile, mirrorCategory.Value, isCorrect);
        }

        if (isCorrect)
        {
            _activeProfile.TotalStars += Math.Max(1, starsEarned);
            CheckMilestoneBadges(_activeProfile, category);
        }

        _activeProfile.LastPlayedUtc = DateTime.UtcNow;
        await SaveProfilesAsync();
        OnProfileChanged?.Invoke();
    }

    private static void RecordSingleSkill(ChildProfile profile, SkillCategory cat, bool isCorrect)
    {
        if (!profile.SkillProgressMap.TryGetValue(cat, out var progress))
        {
            progress = new SkillProgress();
            profile.SkillProgressMap[cat] = progress;
        }

        progress.Attempts++;
        if (isCorrect)
        {
            progress.Correct++;
        }
    }

    /// <inheritdoc />
    public async Task AddPlayTimeSecondsAsync(long seconds)
    {
        await EnsureInitializedAsync();
        if (_activeProfile == null || seconds <= 0) return;

        _activeProfile.TotalSecondsPlayed += seconds;
        _activeProfile.LastPlayedUtc = DateTime.UtcNow;
        await SaveProfilesAsync();
        OnProfileChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task AwardBadgeAsync(string badgeId)
    {
        await EnsureInitializedAsync();
        if (_activeProfile == null || string.IsNullOrWhiteSpace(badgeId)) return;

        if (!_activeProfile.Badges.Contains(badgeId))
        {
            _activeProfile.Badges.Add(badgeId);
            await SaveProfilesAsync();
            OnProfileChanged?.Invoke();
        }
    }

    private async Task SaveProfilesAsync()
    {
        await _saveService.SetItemAsync(StorageKey, _profiles);
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
    }

    private static void CheckMilestoneBadges(ChildProfile profile, SkillCategory category)
    {
        if (profile.TotalStars >= 10 && !profile.Badges.Contains("star_collector_10"))
        {
            profile.Badges.Add("star_collector_10");
        }
        if (profile.TotalStars >= 50 && !profile.Badges.Contains("star_champion_50"))
        {
            profile.Badges.Add("star_champion_50");
        }

        var badgeCode = category switch
        {
            SkillCategory.Phonics => "phonics_star",
            SkillCategory.Vocabulary => "word_wizard",
            SkillCategory.Numbers => "number_ninja",
            SkillCategory.Storytelling => "story_teller",
            _ => null
        };

        if (badgeCode != null && profile.SkillProgressMap.TryGetValue(category, out var p) && p.Correct >= 5 && !profile.Badges.Contains(badgeCode))
        {
            profile.Badges.Add(badgeCode);
        }
    }
}
