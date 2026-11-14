namespace VoxieQuest.Web.Models;

/// <summary>
/// Represents an educational challenge loaded from external JSON data files.
/// </summary>
public class QuestionItem
{
    /// <summary>
    /// Gets or sets the unique challenge code.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the skill realm category this question belongs to.
    /// </summary>
    public SkillCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the challenge difficulty tier.
    /// </summary>
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;

    /// <summary>
    /// Gets or sets the verbal or visual prompt shown to the child.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary target word or phrase expected from speech recognition.
    /// </summary>
    public string TargetAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets alternate accepted verbal variations (e.g., synonyms, number words vs digits).
    /// </summary>
    public List<string> AcceptedAnswers { get; set; } = new();

    /// <summary>
    /// Gets or sets an optional gentle hint spoken or shown when the child hesitates.
    /// </summary>
    public string Hint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets phonetic pronunciation guide (e.g., "K-AA-T").
    /// </summary>
    public string PhoneticSpelling { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a descriptive SVG icon name or path associated with this challenge.
    /// </summary>
    public string IconSvg { get; set; } = "star";

    /// <summary>
    /// Gets or sets the star reward granted upon successful completion.
    /// </summary>
    public int RewardStars { get; set; } = 1;
}
