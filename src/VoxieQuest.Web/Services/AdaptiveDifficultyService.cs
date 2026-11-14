using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements adaptive difficulty scaling: raises after 3 correct in a row, eases after 3 misses.
/// </summary>
public class AdaptiveDifficultyService : IAdaptiveDifficultyService
{
    private const int Threshold = 3;

    /// <inheritdoc />
    public DifficultyLevel CurrentDifficulty { get; private set; } = DifficultyLevel.Easy;

    /// <inheritdoc />
    public int ConsecutiveCorrect { get; private set; } = 0;

    /// <inheritdoc />
    public int ConsecutiveMisses { get; private set; } = 0;

    /// <inheritdoc />
    public event Action<DifficultyLevel, bool>? OnDifficultyChanged;

    /// <inheritdoc />
    public bool RecordAttempt(bool isCorrect)
    {
        if (isCorrect)
        {
            ConsecutiveCorrect++;
            ConsecutiveMisses = 0;

            if (ConsecutiveCorrect >= Threshold)
            {
                ConsecutiveCorrect = 0;

                if (CurrentDifficulty < DifficultyLevel.Hard)
                {
                    CurrentDifficulty++;
                    OnDifficultyChanged?.Invoke(CurrentDifficulty, true);
                    return true;
                }
            }
        }
        else
        {
            ConsecutiveMisses++;
            ConsecutiveCorrect = 0;

            if (ConsecutiveMisses >= Threshold)
            {
                ConsecutiveMisses = 0;

                if (CurrentDifficulty > DifficultyLevel.Easy)
                {
                    CurrentDifficulty--;
                    OnDifficultyChanged?.Invoke(CurrentDifficulty, false);
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        ConsecutiveCorrect = 0;
        ConsecutiveMisses = 0;
        CurrentDifficulty = DifficultyLevel.Easy;
    }

    /// <inheritdoc />
    public void SetDifficulty(DifficultyLevel level)
    {
        ConsecutiveCorrect = 0;
        ConsecutiveMisses = 0;
        CurrentDifficulty = level;
    }
}
