using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Core educational difficulty orchestration service.
/// Manages child-friendly botanical tiers (Seedling, Sapling, Big Tree),
/// hidden 1-100 micro-levels, adaptive flow-zone tracking, silent scaffolding,
/// and winding picture-book level progression.
/// </summary>
public interface IDifficultyService
{
    /// <summary>Initializes and loads persistent skill difficulty states from storage.</summary>
    Task InitializeAsync();

    /// <summary>Gets the tracking difficulty state for the specified skill.</summary>
    SkillDifficultyState GetSkillState(SkillCategory skill);

    /// <summary>Asynchronously loads or retrieves cached JSON game tuning configuration.</summary>
    Task<GameDifficultyConfig> GetGameConfigAsync(string gameKey);

    /// <summary>Retrieves active tier parameters for the specified game and skill realm.</summary>
    Task<TierParameters> GetActiveTierParametersAsync(string gameKey, SkillCategory skill);

    /// <summary>
    /// Records a child's answer attempt, evaluates the rolling 8-answer window,
    /// applies micro-level adjustments, detects flow zone, and returns adaptive feedback.
    /// </summary>
    Task<AdaptiveEvaluationResult> RecordAttemptAsync(SkillCategory skill, bool isCorrect, double responseTimeSeconds, bool usedHint);

    /// <summary>Records a question skip or timeout, triggering gentle support scaffolding.</summary>
    Task<AdaptiveEvaluationResult> RecordSkipOrTimeoutAsync(SkillCategory skill);

    /// <summary>Manually switches the visible tier for a skill (optional parent lock).</summary>
    Task SetManualTierAsync(SkillCategory skill, DifficultyTier tier, bool lockTier = false);

    /// <summary>Unlocks manual tier lock, returning skill to automatic adaptive flow.</summary>
    Task UnlockManualTierAsync(SkillCategory skill);

    /// <summary>Configures age-appropriate defaults across all skills (3-5, 6-8, 9-12, 13+).</summary>
    Task SetAgeBandDefaultsAsync(string ageBand);

    /// <summary>Allows parents or educators to unlock all 10 trail levels for a skill.</summary>
    Task UnlockAllLevelsAsync(SkillCategory skill);

    /// <summary>Resets a skill realm back to baseline starting micro-level.</summary>
    Task ResetSkillAsync(SkillCategory skill);

    /// <summary>Marks a trail level completed with 1-3 effort/improvement stars.</summary>
    Task<LevelStarProgress> CompleteLevelAsync(SkillCategory skill, int levelNumber, int starsEarned, double bestTimeSeconds);

    /// <summary>Generates comforting, non-judgmental plain-language progress summary for parents.</summary>
    string GetParentSummary(SkillCategory skill);

    /// <summary>Fired when any skill state or tier changes.</summary>
    event Action? OnDifficultyChanged;
}
