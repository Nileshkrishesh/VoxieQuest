namespace VoxieQuest.Web.Models;

/// <summary>
/// Tracks accuracy, attempts, and difficulty level for a specific educational skill.
/// </summary>
public class SkillProgress
{
    /// <summary>
    /// Gets or sets the total number of attempts made in this skill.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the number of correct responses recorded.
    /// </summary>
    public int Correct { get; set; }

    /// <summary>
    /// Gets or sets the current adaptive difficulty level for this skill.
    /// </summary>
    public DifficultyLevel CurrentDifficulty { get; set; } = DifficultyLevel.Easy;

    /// <summary>
    /// Gets the accuracy percentage (0.0 to 100.0). Returns 100.0 if no attempts have been made yet.
    /// </summary>
    public double AccuracyPercentage => Attempts == 0 ? 100.0 : Math.Round((double)Correct / Attempts * 100.0, 1);
}
