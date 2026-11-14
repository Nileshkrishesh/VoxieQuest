using System.Text.Json.Serialization;

namespace VoxieQuest.Web.Models;

/// <summary>
/// Represents a local child user profile stored safely in browser storage.
/// </summary>
public class ChildProfile
{
    /// <summary>
    /// Gets or sets the unique identifier for the profile.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the child's display name.
    /// </summary>
    public string Name { get; set; } = "Young Adventurer";

    /// <summary>
    /// Gets or sets the chosen avatar mascot for the child.
    /// </summary>
    public AvatarType Avatar { get; set; } = AvatarType.VoxieChameleon;

    /// <summary>
    /// Gets or sets the child's age group (5 to 10).
    /// </summary>
    public int Age { get; set; } = 6;

    /// <summary>
    /// Gets or sets the total accumulated stars earned across games.
    /// </summary>
    public int TotalStars { get; set; } = 0;

    /// <summary>
    /// Gets or sets the list of badge codes earned by this adventurer.
    /// </summary>
    public List<string> Badges { get; set; } = new();

    /// <summary>
    /// Gets or sets the historical accuracy and progress per educational skill.
    /// </summary>
    public Dictionary<SkillCategory, SkillProgress> SkillProgressMap { get; set; } = new()
    {
        [SkillCategory.Phonics] = new SkillProgress(),
        [SkillCategory.Vocabulary] = new SkillProgress(),
        [SkillCategory.Numbers] = new SkillProgress(),
        [SkillCategory.Storytelling] = new SkillProgress(),
        [SkillCategory.Letters] = new SkillProgress(),
        [SkillCategory.Colors] = new SkillProgress(),
        [SkillCategory.Animals] = new SkillProgress(),
        [SkillCategory.Spelling] = new SkillProgress()
    };

    /// <summary>
    /// Gets or sets the cumulative time played in seconds.
    /// </summary>
    public long TotalSecondsPlayed { get; set; } = 0;

    /// <summary>
    /// Gets or sets the profile creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last active session timestamp.
    /// </summary>
    public DateTime LastPlayedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Formats total seconds played into a human-readable string (e.g., "15m" or "1h 20m").
    /// </summary>
    [JsonIgnore]
    public string FormattedPlayTime
    {
        get
        {
            var timeSpan = TimeSpan.FromSeconds(TotalSecondsPlayed);
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            }
            return $"{Math.Max(1, (int)timeSpan.TotalMinutes)}m";
        }
    }
}
