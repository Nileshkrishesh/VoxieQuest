using System.Text.Json.Serialization;

namespace VoxieQuest.Web.Models;

/// <summary>
/// The three visible difficulty tiers presented to children with friendly botanical growth themes.
/// Replaces traditional "Easy / Medium / Hard" terminology.
/// </summary>
public enum DifficultyTier
{
    /// <summary>Gentle learning pace, shorter words/sums, abundant hints, no countdown pressure.</summary>
    Seedling = 1,

    /// <summary>Balanced learning flow, moderate vocabulary, gentle timers, standard choices.</summary>
    Sapling = 2,

    /// <summary>Advanced learning, expanded pool, multi-step thinking, challenge timers.</summary>
    BigTree = 3
}

/// <summary>
/// Extension methods for formatting difficulty tiers for child and parent UIs.
/// </summary>
public static class DifficultyTierExtensions
{
    /// <summary>Returns child-friendly botanical tier title.</summary>
    public static string ToFriendlyName(this DifficultyTier tier) => tier switch
    {
        DifficultyTier.Seedling => "Seedling",
        DifficultyTier.Sapling => "Sapling",
        DifficultyTier.BigTree => "Big Tree",
        _ => "Seedling"
    };

    /// <summary>Returns playful emoji icon representing the growth tier.</summary>
    public static string ToIcon(this DifficultyTier tier) => tier switch
    {
        DifficultyTier.Seedling => "🌱",
        DifficultyTier.Sapling => "🌿",
        DifficultyTier.BigTree => "🌳",
        _ => "🌱"
    };

    /// <summary>Returns comforting description for child selection.</summary>
    public static string ToDescription(this DifficultyTier tier) => tier switch
    {
        DifficultyTier.Seedling => "Gentle play, no rush, lots of hints",
        DifficultyTier.Sapling => "Just right, balanced adventure quests",
        DifficultyTier.BigTree => "Bigger words, faster leaps, mighty challenge",
        _ => "Gentle play"
    };
}

/// <summary>
/// Individual attempt record within a rolling window of recent responses.
/// </summary>
public class AttemptRecord
{
    public bool IsCorrect { get; set; }
    public double ResponseTimeSeconds { get; set; }
    public bool UsedHint { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Progress on an individual level stop (1 to 10) on a game's picture-book trail.
/// </summary>
public class LevelStarProgress
{
    public int LevelNumber { get; set; } = 1;
    public int StarsEarned { get; set; } = 0; // 0 to 3 stars
    public bool IsUnlocked { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    public bool IsBossRound { get; set; } = false; // Levels 5 and 10
    public string? SpecialSticker { get; set; }
    public double BestResponseTimeSeconds { get; set; } = 0;
}

/// <summary>
/// Persistent adaptive difficulty tracking state for a specific educational skill.
/// </summary>
public class SkillDifficultyState
{
    public SkillCategory Skill { get; set; }

    /// <summary>
    /// Hidden micro-level (1 to 100) calibrated smoothly behind the scenes.
    /// 1-35: Seedling 🌱 | 36-70: Sapling 🌿 | 71-100: Big Tree 🌳
    /// </summary>
    public int MicroLevel { get; set; } = 20;

    /// <summary>Tier recorded at the beginning of the active session to prevent dropping > 1 tier.</summary>
    public DifficultyTier StartingSessionTier { get; set; } = DifficultyTier.Seedling;

    /// <summary>Gets the current calculated or locked tier.</summary>
    [JsonIgnore]
    public DifficultyTier CurrentTier
    {
        get
        {
            if (IsTierLocked && LockedTier.HasValue)
            {
                return LockedTier.Value;
            }

            if (MicroLevel <= 35) return DifficultyTier.Seedling;
            if (MicroLevel <= 70) return DifficultyTier.Sapling;
            return DifficultyTier.BigTree;
        }
    }

    /// <summary>Indicates whether parent has locked the skill to a specific tier.</summary>
    public bool IsTierLocked { get; set; } = false;

    /// <summary>The parent-locked difficulty tier when locked.</summary>
    public DifficultyTier? LockedTier { get; set; }

    /// <summary>Rolling window of the last 8 answers for flow-zone evaluation.</summary>
    public List<AttemptRecord> RecentAttempts { get; set; } = new();

    public int ConsecutiveCorrect { get; set; } = 0;
    public int ConsecutiveMisses { get; set; } = 0;
    public int ConsecutiveSkips { get; set; } = 0;
    public int HintsUsedCount { get; set; } = 0;

    /// <summary>Whether silent scaffolding help (slower pace, hints, fewer choices) is currently engaged.</summary>
    public bool IsSilentHelpActive { get; set; } = false;

    /// <summary>Weekly micro-level snapshot points over 4 weeks for parent trend visualization.</summary>
    public List<int> WeeklyHistory { get; set; } = new() { 20, 22, 25, 28 };

    /// <summary>10-level progression trail for this skill realm.</summary>
    public Dictionary<int, LevelStarProgress> Levels { get; set; } = new();

    /// <summary>Whether a parent has unlocked all 10 levels.</summary>
    public bool AllLevelsUnlocked { get; set; } = false;

    /// <summary>Calculates percentage accuracy across recent attempts.</summary>
    [JsonIgnore]
    public double RecentAccuracyPercent
    {
        get
        {
            if (!RecentAttempts.Any()) return 100.0;
            return Math.Round((double)RecentAttempts.Count(a => a.IsCorrect) / RecentAttempts.Count * 100.0, 1);
        }
    }

    /// <summary>Calculates average response time in seconds across recent attempts.</summary>
    [JsonIgnore]
    public double AverageResponseTimeSeconds
    {
        get
        {
            if (!RecentAttempts.Any()) return 3.5;
            return Math.Round(RecentAttempts.Average(a => a.ResponseTimeSeconds), 2);
        }
    }

    public SkillDifficultyState()
    {
        InitializeLevels();
    }

    public SkillDifficultyState(SkillCategory skill, int initialMicroLevel = 20)
    {
        Skill = skill;
        MicroLevel = Math.Clamp(initialMicroLevel, 1, 100);
        StartingSessionTier = CurrentTier;
        InitializeLevels();
    }

    private void InitializeLevels()
    {
        if (Levels.Any()) return;

        for (int i = 1; i <= 10; i++)
        {
            Levels[i] = new LevelStarProgress
            {
                LevelNumber = i,
                IsUnlocked = (i == 1),
                IsBossRound = (i == 5 || i == 10),
                SpecialSticker = i == 5 ? "👑 Forest Guardian Badge" : (i == 10 ? "🏆 Grand Realm Explorer" : null)
            };
        }
    }
}

/// <summary>
/// Actions triggered by the adaptive engine to keep children in their optimal flow zone.
/// </summary>
public enum AdaptiveSignal
{
    None = 0,
    RaiseMicroLevel = 1,
    SilentHelpActive = 2,
    EaseDifficulty = 3,
    OfferFrustrationBreak = 4,
    OfferBoredomChallenge = 5
}

/// <summary>
/// Evaluation outcome result after processing a child's answer attempt.
/// </summary>
public class AdaptiveEvaluationResult
{
    public AdaptiveSignal Signal { get; set; } = AdaptiveSignal.None;
    public int OldMicroLevel { get; set; }
    public int NewMicroLevel { get; set; }
    public DifficultyTier OldTier { get; set; }
    public DifficultyTier NewTier { get; set; }
    public bool TierChanged => OldTier != NewTier;
    public bool Promoted => NewMicroLevel > OldMicroLevel;
    public string FeedbackMessage { get; set; } = string.Empty;
    public bool OfferBreak { get; set; } = false;
    public bool OfferChallenge { get; set; } = false;
    public bool SilentHelpProvided { get; set; } = false;
}

/// <summary>
/// External JSON parameter schema for fine-tuning game mechanics per tier without recompiling.
/// Stored in wwwroot/data/difficulty/{game-key}.json
/// </summary>
public class GameDifficultyConfig
{
    public string GameKey { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public Dictionary<string, TierParameters> Tiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public TierParameters GetTier(DifficultyTier tier)
    {
        var key = tier.ToString();
        if (Tiers.TryGetValue(key, out var parameters))
        {
            return parameters;
        }

        // Fallback default
        return new TierParameters();
    }
}

/// <summary>
/// Tunable parameters for an individual difficulty tier.
/// </summary>
public class TierParameters
{
    // Letter Rocket
    public string LetterPool { get; set; } = "A,B,C,D,E";
    public bool AllowLowercase { get; set; } = false;
    public int? TimerSeconds { get; set; } = null; // null = no timer
    public int ChoicesCount { get; set; } = 2;

    // Number Jump
    public int SumMin { get; set; } = 1;
    public int SumMax { get; set; } = 5;
    public bool AllowSubtraction { get; set; } = false;
    public bool AllowMixed { get; set; } = false;

    // Color Splash
    public string PaletteTier { get; set; } = "basic"; // basic, shades, mixed
    public int ColorsCount { get; set; } = 4;

    // Animal Safari
    public int AnimalCount { get; set; } = 4;
    public bool UseSimilarSounds { get; set; } = false;

    // Word Builder
    public int WordLength { get; set; } = 3;
    public bool UseBlends { get; set; } = false;
}
