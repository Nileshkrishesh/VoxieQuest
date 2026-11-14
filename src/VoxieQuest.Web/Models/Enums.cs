namespace VoxieQuest.Web.Models;

/// <summary>
/// Represents the high-level operational state of the game loop.
/// </summary>
public enum GameModeState
{
    /// <summary>Main landing or menu selection screen.</summary>
    Menu,

    /// <summary>Active gameplay with speech interaction.</summary>
    Playing,

    /// <summary>Celebratory reward and badge allocation state.</summary>
    Reward,

    /// <summary>Paused gameplay with audio cues suspended.</summary>
    Paused
}

/// <summary>
/// Adaptive difficulty levels for educational challenges.
/// </summary>
public enum DifficultyLevel
{
    /// <summary>Ages 5-6: Single phonemes, simple sight words, numbers 1-5.</summary>
    Easy = 1,

    /// <summary>Ages 7-8: Blends, compound words, numbers 1-20, simple sums.</summary>
    Medium = 2,

    /// <summary>Ages 9-10: Multi-syllable vocabulary, arithmetic, narrative prompts.</summary>
    Hard = 3
}

/// <summary>
/// Educational skill realms supported by the platform.
/// </summary>
public enum SkillCategory
{
    /// <summary>Phonetics, letter sounds, and rhyming.</summary>
    Phonics = 0,

    /// <summary>Vocabulary building and object identification.</summary>
    Vocabulary = 1,

    /// <summary>Verbal counting, recognition, and arithmetic.</summary>
    Numbers = 2,

    /// <summary>Narrative listening and spoken adventure decisions.</summary>
    Storytelling = 3,

    /// <summary>Letters and alphabet phoneme identification (Letter Rocket).</summary>
    Letters = 4,

    /// <summary>Colors and hue identification (Color Splash).</summary>
    Colors = 5,

    /// <summary>Animal sounds and creature recognition (Animal Safari).</summary>
    Animals = 6,

    /// <summary>Spelling and block word construction (Word Builder).</summary>
    Spelling = 7
}

/// <summary>
/// Procedural and sampled audio sound effects.
/// </summary>
public enum SoundEffect
{
    /// <summary>Gentle button click.</summary>
    Click,

    /// <summary>Playful bubble pop.</summary>
    Pop,

    /// <summary>High-pitched chime on successful answer.</summary>
    SuccessChime,

    /// <summary>Gentle curious chime on misrecognition or try-again.</summary>
    EncourageChime,

    /// <summary>Musical fanfare when completing a quest.</summary>
    Fanfare,

    /// <summary>Star counter burst increment sound.</summary>
    StarBurst
}

/// <summary>
/// Child-friendly avatar choices for player profiles.
/// </summary>
public enum AvatarType
{
    /// <summary>Voxie the friendly chameleon companion.</summary>
    VoxieChameleon,

    /// <summary>Leo the courageous lion.</summary>
    LeoLion,

    /// <summary>Pip the playful penguin.</summary>
    PipPenguin,

    /// <summary>Mia the clever monkey.</summary>
    MiaMonkey
}

/// <summary>
/// Expressive emotional states for the Voxie mascot avatar.
/// </summary>
public enum MascotMood
{
    /// <summary>Gentle standing idle.</summary>
    Idle,

    /// <summary>Ears attentive, listening to child.</summary>
    Listening,

    /// <summary>Sparkles and joyful smile on correct answer.</summary>
    Celebrating,

    /// <summary>Thinking and evaluating speech input.</summary>
    Thinking
}

