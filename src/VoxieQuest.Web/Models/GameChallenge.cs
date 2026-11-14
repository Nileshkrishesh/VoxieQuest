namespace VoxieQuest.Web.Models;

/// <summary>
/// Universal challenge model supporting LetterRocket, NumberJump, ColorSplash, and realm games.
/// </summary>
public class GameChallenge
{
    /// <summary>Unique identifier for the challenge item.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Educational skill category.</summary>
    public SkillCategory Category { get; set; }

    /// <summary>Difficulty tier (Easy, Medium, Hard).</summary>
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;

    /// <summary>Spoken/auditory prompt given by Voxie.</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Target word or character expected from speech recognition.</summary>
    public string TargetAnswer { get; set; } = string.Empty;

    /// <summary>List of accepted speech variations and synonyms.</summary>
    public List<string> AcceptedAnswers { get; set; } = new();

    /// <summary>Gentle educational hint revealed on the 2nd attempt miss.</summary>
    public string Hint { get; set; } = string.Empty;

    /// <summary>Phonetic pronunciation guide (e.g., "B - AH - T").</summary>
    public string PhoneticSpelling { get; set; } = string.Empty;

    /// <summary>Stars awarded on successful completion.</summary>
    public int RewardStars { get; set; } = 1;

    /// <summary>Primary visual item to render (Letter "A", Equation "3 + 2", Picture "apple").</summary>
    public string DisplayItem { get; set; } = string.Empty;

    /// <summary>Target color hex code for ColorSplash (e.g. "#EF476F" for red).</summary>
    public string TargetColor { get; set; } = "#FFD166";

    /// <summary>Starting lily pad number for NumberJump frog.</summary>
    public int FrogStartNumber { get; set; } = 1;

    /// <summary>Destination lily pad number for NumberJump frog.</summary>
    public int FrogTargetNumber { get; set; } = 2;

    /// <summary>Animal sound identifier for AnimalSafari (e.g. "cat", "dog", "cow", "lion").</summary>
    public string AnimalSoundKey { get; set; } = string.Empty;

    /// <summary>Display emoji or graphic symbol for animal or picture card.</summary>
    public string AnimalEmoji { get; set; } = "🐾";

    /// <summary>Child-friendly sound onomatopoeia for AnimalSafari (e.g. "Meow!", "Woof!").</summary>
    public string SoundOnomatopoeia { get; set; } = string.Empty;

    /// <summary>Individual letter blocks for WordBuilder (e.g. ["C", "A", "T"]).</summary>
    public List<string> LetterBlocks { get; set; } = new();

    /// <summary>Illustration or emoji revealed when WordBuilder blocks snap together.</summary>
    public string RevealedPicture { get; set; } = "🌟";
}
