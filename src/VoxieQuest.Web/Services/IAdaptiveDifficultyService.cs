using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Service managing real-time adaptive challenge calibration for children.
/// Adjusts difficulty upwards after 3 consecutive successes, or eases after 3 misses.
/// </summary>
public interface IAdaptiveDifficultyService
{
    /// <summary>
    /// Gets the active difficulty level.
    /// </summary>
    DifficultyLevel CurrentDifficulty { get; }

    /// <summary>
    /// Gets the current streak of consecutive correct answers.
    /// </summary>
    int ConsecutiveCorrect { get; }

    /// <summary>
    /// Gets the current count of consecutive missed answers.
    /// </summary>
    int ConsecutiveMisses { get; }

    /// <summary>
    /// Event raised whenever the difficulty level changes.
    /// Parameters: new level, whether the change was a promotion (true) or easing (false).
    /// </summary>
    event Action<DifficultyLevel, bool>? OnDifficultyChanged;

    /// <summary>
    /// Records the outcome of a child's challenge attempt and evaluates difficulty thresholds.
    /// </summary>
    /// <param name="isCorrect">True if the verbal answer matched, false otherwise.</param>
    /// <returns>True if the difficulty level changed as a result of this attempt.</returns>
    bool RecordAttempt(bool isCorrect);

    /// <summary>
    /// Resets the consecutive counters and restores difficulty to Easy.
    /// </summary>
    void Reset();

    /// <summary>
    /// Explicitly sets the difficulty level and resets consecutive counters.
    /// </summary>
    /// <param name="level">Target difficulty level.</param>
    void SetDifficulty(DifficultyLevel level);
}
