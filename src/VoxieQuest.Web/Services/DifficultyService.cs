using System.Net.Http.Json;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implementation of the Voxie Quest Difficulty and Adaptive Learning Engine.
/// Provides continuous micro-level tracking, silent scaffolding, 10-level picture-book paths,
/// and child-first encouragement without punitive terminology.
/// </summary>
public class DifficultyService : IDifficultyService
{
    private const string StorageKey = "voxie_difficulty_state_v1";
    private readonly ISaveService _saveService;
    private readonly HttpClient _httpClient;

    private readonly Dictionary<SkillCategory, SkillDifficultyState> _states = new();
    private readonly Dictionary<string, GameDifficultyConfig> _configCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _isInitialized = false;

    public event Action? OnDifficultyChanged;

    public DifficultyService(ISaveService saveService, HttpClient httpClient)
    {
        _saveService = saveService;
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var saved = await _saveService.GetItemAsync<Dictionary<SkillCategory, SkillDifficultyState>>(StorageKey);
            if (saved != null && saved.Count > 0)
            {
                foreach (var (skill, state) in saved)
                {
                    // Ensure starting tier for the current session is captured
                    state.StartingSessionTier = state.CurrentTier;
                    _states[skill] = state;
                }
            }
        }
        catch
        {
            // Fallback gracefully on fresh profile
        }

        // Ensure all skill categories have an entry
        foreach (SkillCategory category in Enum.GetValues<SkillCategory>())
        {
            if (!_states.ContainsKey(category))
            {
                _states[category] = new SkillDifficultyState(category, 20);
            }
        }

        _isInitialized = true;
    }

    public SkillDifficultyState GetSkillState(SkillCategory skill)
    {
        if (!_states.TryGetValue(skill, out var state))
        {
            state = new SkillDifficultyState(skill, 20);
            _states[skill] = state;
        }
        return state;
    }

    public async Task<GameDifficultyConfig> GetGameConfigAsync(string gameKey)
    {
        if (_configCache.TryGetValue(gameKey, out var cached))
        {
            return cached;
        }

        try
        {
            var loaded = await _httpClient.GetFromJsonAsync<GameDifficultyConfig>($"data/difficulty/{gameKey}.json");
            if (loaded != null)
            {
                _configCache[gameKey] = loaded;
                return loaded;
            }
        }
        catch
        {
            // Graceful fallback if JSON is missing or inaccessible
        }

        var fallback = CreateFallbackConfig(gameKey);
        _configCache[gameKey] = fallback;
        return fallback;
    }

    public async Task<TierParameters> GetActiveTierParametersAsync(string gameKey, SkillCategory skill)
    {
        var config = await GetGameConfigAsync(gameKey);
        var state = GetSkillState(skill);
        return config.GetTier(state.CurrentTier);
    }

    public async Task<AdaptiveEvaluationResult> RecordAttemptAsync(
        SkillCategory skill,
        bool isCorrect,
        double responseTimeSeconds,
        bool usedHint)
    {
        var state = GetSkillState(skill);
        var oldMicro = state.MicroLevel;
        var oldTier = state.CurrentTier;

        // 1. Maintain rolling window of last 8 answers
        state.RecentAttempts.Add(new AttemptRecord
        {
            IsCorrect = isCorrect,
            ResponseTimeSeconds = Math.Max(0.2, responseTimeSeconds),
            UsedHint = usedHint,
            Timestamp = DateTime.UtcNow
        });

        if (state.RecentAttempts.Count > 8)
        {
            state.RecentAttempts.RemoveAt(0);
        }

        if (usedHint)
        {
            state.HintsUsedCount++;
        }

        var result = new AdaptiveEvaluationResult
        {
            OldMicroLevel = oldMicro,
            OldTier = oldTier
        };

        if (isCorrect)
        {
            state.ConsecutiveCorrect++;
            state.ConsecutiveMisses = 0;
            state.ConsecutiveSkips = 0;
            state.IsSilentHelpActive = false;

            // 3 in a row promotes micro-level (+3, or +5 if fast < 2.0s)
            if (state.ConsecutiveCorrect >= 3)
            {
                var recentThree = state.RecentAttempts.TakeLast(3).ToList();
                double avgTime = recentThree.Any() ? recentThree.Average(a => a.ResponseTimeSeconds) : 3.0;

                int delta = (avgTime < 2.0) ? 5 : 3;
                state.MicroLevel = Math.Clamp(state.MicroLevel + delta, 1, 100);
                state.ConsecutiveCorrect = 0;

                result.Signal = AdaptiveSignal.RaiseMicroLevel;
                result.FeedbackMessage = "You grew! Voxie's garden has a new flower 🌱✨";
            }

            // Boredom detection: 5 very fast consecutive answers (< 2.5s)
            var recentFive = state.RecentAttempts.TakeLast(5).ToList();
            if (recentFive.Count == 5 && recentFive.All(a => a.IsCorrect && a.ResponseTimeSeconds < 2.5))
            {
                result.OfferChallenge = true;
                result.Signal = AdaptiveSignal.OfferBoredomChallenge;
                result.FeedbackMessage = "Speed champion! Voxie unlocked a special challenge round with a bonus sticker! 🌟";
            }
        }
        else
        {
            state.ConsecutiveMisses++;
            state.ConsecutiveCorrect = 0;

            // Silent scaffolding help before lowering level (1 or 2 misses)
            if (state.ConsecutiveMisses < 3)
            {
                state.IsSilentHelpActive = true;
                result.Signal = AdaptiveSignal.SilentHelpActive;
                result.SilentHelpProvided = true;
                result.FeedbackMessage = "Voxie has a gentle clue ready for you! 💡";
            }
            else
            {
                // 3 misses: ease difficulty by -3, never dropping more than one tier in a single session
                int minAllowedLevel = GetSessionFloorMicroLevel(state.StartingSessionTier);
                state.MicroLevel = Math.Clamp(state.MicroLevel - 3, minAllowedLevel, 100);
                state.ConsecutiveMisses = 0;

                result.Signal = AdaptiveSignal.EaseDifficulty;
                result.OfferBreak = true;
                result.FeedbackMessage = "Let's try a warm-up! Voxie's garden is ready for fresh practice. 🌸";
            }
        }

        result.NewMicroLevel = state.MicroLevel;
        result.NewTier = state.CurrentTier;

        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
        return result;
    }

    public async Task<AdaptiveEvaluationResult> RecordSkipOrTimeoutAsync(SkillCategory skill)
    {
        var state = GetSkillState(skill);
        state.ConsecutiveSkips++;
        state.IsSilentHelpActive = true;

        var result = new AdaptiveEvaluationResult
        {
            OldMicroLevel = state.MicroLevel,
            NewMicroLevel = state.MicroLevel,
            OldTier = state.CurrentTier,
            NewTier = state.CurrentTier,
            SilentHelpProvided = true,
            Signal = AdaptiveSignal.SilentHelpActive
        };

        if (state.ConsecutiveSkips >= 2)
        {
            result.OfferBreak = true;
            result.Signal = AdaptiveSignal.OfferFrustrationBreak;
            result.FeedbackMessage = "No rush! Let's take a calm breath together or try a warm-up round. 🍃";
        }
        else
        {
            result.FeedbackMessage = "Take all the time you need! Voxie has your back. 🎈";
        }

        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
        return result;
    }

    public async Task SetManualTierAsync(SkillCategory skill, DifficultyTier tier, bool lockTier = false)
    {
        var state = GetSkillState(skill);
        state.IsTierLocked = lockTier;
        state.LockedTier = lockTier ? tier : null;

        // Align micro-level with chosen tier baseline
        state.MicroLevel = tier switch
        {
            DifficultyTier.Seedling => 20,
            DifficultyTier.Sapling => 50,
            DifficultyTier.BigTree => 85,
            _ => 20
        };

        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
    }

    public async Task UnlockManualTierAsync(SkillCategory skill)
    {
        var state = GetSkillState(skill);
        state.IsTierLocked = false;
        state.LockedTier = null;
        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
    }

    public async Task SetAgeBandDefaultsAsync(string ageBand)
    {
        int startingMicroLevel = ageBand switch
        {
            "3-5" => 20,
            "6-8" => 35,
            "9-12" => 50,
            "13+" => 65,
            _ => 20
        };

        foreach (var state in _states.Values)
        {
            state.MicroLevel = startingMicroLevel;
            state.StartingSessionTier = state.CurrentTier;
            state.IsTierLocked = false;
            state.LockedTier = null;
        }

        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
    }

    public async Task UnlockAllLevelsAsync(SkillCategory skill)
    {
        var state = GetSkillState(skill);
        state.AllLevelsUnlocked = true;
        foreach (var lvl in state.Levels.Values)
        {
            lvl.IsUnlocked = true;
        }

        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
    }

    public async Task ResetSkillAsync(SkillCategory skill)
    {
        var state = GetSkillState(skill);
        state.MicroLevel = 20;
        state.StartingSessionTier = DifficultyTier.Seedling;
        state.IsTierLocked = false;
        state.LockedTier = null;
        state.RecentAttempts.Clear();
        state.ConsecutiveCorrect = 0;
        state.ConsecutiveMisses = 0;
        state.ConsecutiveSkips = 0;
        state.HintsUsedCount = 0;
        state.IsSilentHelpActive = false;
        state.AllLevelsUnlocked = false;

        foreach (var lvl in state.Levels.Values)
        {
            lvl.IsUnlocked = (lvl.LevelNumber == 1);
            lvl.IsCompleted = false;
            lvl.StarsEarned = 0;
            lvl.BestResponseTimeSeconds = 0;
        }

        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
    }

    public async Task<LevelStarProgress> CompleteLevelAsync(
        SkillCategory skill,
        int levelNumber,
        int starsEarned,
        double bestTimeSeconds)
    {
        var state = GetSkillState(skill);
        if (!state.Levels.TryGetValue(levelNumber, out var levelProgress))
        {
            levelProgress = new LevelStarProgress { LevelNumber = levelNumber };
            state.Levels[levelNumber] = levelProgress;
        }

        levelProgress.IsCompleted = true;
        levelProgress.StarsEarned = Math.Max(levelProgress.StarsEarned, Math.Clamp(starsEarned, 1, 3));
        if (bestTimeSeconds > 0)
        {
            levelProgress.BestResponseTimeSeconds = levelProgress.BestResponseTimeSeconds == 0
                ? bestTimeSeconds
                : Math.Min(levelProgress.BestResponseTimeSeconds, bestTimeSeconds);
        }

        // Unlock next level in trail
        int nextLevel = levelNumber + 1;
        if (nextLevel <= 10 && state.Levels.TryGetValue(nextLevel, out var nextProgress))
        {
            nextProgress.IsUnlocked = true;
        }

        await PersistStatesAsync();
        OnDifficultyChanged?.Invoke();
        return levelProgress;
    }

    public string GetParentSummary(SkillCategory skill)
    {
        var state = GetSkillState(skill);
        var tier = state.CurrentTier.ToFriendlyName();
        var acc = state.RecentAccuracyPercent;

        if (acc >= 80.0)
        {
            return $"{skill} is blossoming beautifully at {tier} level ({acc}% accuracy). Great flow zone! Ready to explore deeper challenges together.";
        }
        if (acc >= 65.0)
        {
            return $"{skill} is right in the sweet spot ({acc}% accuracy) at {tier}. Healthy persistence and solid engagement.";
        }
        return $"{skill} is exploring foundational concepts at {tier}. Try a warm-up together with extra gentle clues.";
    }

    private int GetSessionFloorMicroLevel(DifficultyTier startingTier)
    {
        // Never drop more than one tier in a single session:
        // BigTree (starts >= 71) cannot drop below Sapling (min 36)
        // Sapling (starts 36-70) cannot drop below Seedling (min 1)
        // Seedling (starts 1-35) floor is 1
        return startingTier switch
        {
            DifficultyTier.BigTree => 36,
            DifficultyTier.Sapling => 1,
            _ => 1
        };
    }

    private async Task PersistStatesAsync()
    {
        try
        {
            await _saveService.SetItemAsync(StorageKey, _states);
        }
        catch
        {
            // Storage failure fallback
        }
    }

    private GameDifficultyConfig CreateFallbackConfig(string gameKey)
    {
        var config = new GameDifficultyConfig
        {
            GameKey = gameKey,
            GameTitle = gameKey
        };

        config.Tiers["Seedling"] = new TierParameters
        {
            LetterPool = "A,B,C,D,E",
            SumMin = 1,
            SumMax = 5,
            PaletteTier = "basic",
            AnimalCount = 4,
            WordLength = 3,
            TimerSeconds = null,
            ChoicesCount = 2
        };

        config.Tiers["Sapling"] = new TierParameters
        {
            LetterPool = "A,B,C,D,E,F,G,H,I,J,K,L,M",
            SumMin = 1,
            SumMax = 10,
            AllowSubtraction = true,
            PaletteTier = "extended",
            AnimalCount = 8,
            WordLength = 4,
            TimerSeconds = 15,
            ChoicesCount = 3
        };

        config.Tiers["BigTree"] = new TierParameters
        {
            LetterPool = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z",
            SumMin = 1,
            SumMax = 20,
            AllowSubtraction = true,
            AllowMixed = true,
            PaletteTier = "shades",
            AnimalCount = 12,
            WordLength = 5,
            TimerSeconds = 10,
            ChoicesCount = 4
        };

        return config;
    }
}
