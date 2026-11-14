using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Audio synthesis and sound effects service for children's game feedback.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Gets whether audio playback is muted.
    /// </summary>
    bool IsMuted { get; }

    /// <summary>
    /// Gets the current master audio volume (0.0 to 1.0).
    /// </summary>
    double Volume { get; }

    /// <summary>
    /// Plays a designated sound effect (procedural Web Audio tone or chime).
    /// </summary>
    /// <param name="effect">The sound effect to trigger.</param>
    Task PlaySoundAsync(SoundEffect effect);

    /// <summary>
    /// Plays an immediate celebratory or try-again musical chime.
    /// </summary>
    /// <param name="success">True for celebratory, false for try-again.</param>
    Task PlayChimeAsync(bool success);

    /// <summary>
    /// Plays an animal call or sound effect, attempting to load from static audio assets with procedural audio fallback.
    /// </summary>
    /// <param name="animalName">The lower-case name of the animal (e.g. "cat", "dog", "lion", "frog").</param>
    Task PlayAnimalSoundAsync(string animalName);

    /// <summary>
    /// Mutes or unmutes all game sounds.
    /// </summary>
    /// <param name="muted">True to mute, false to unmute.</param>
    Task SetMutedAsync(bool muted);

    /// <summary>
    /// Sets the master volume between 0.0 (silent) and 1.0 (full).
    /// </summary>
    /// <param name="volume">Volume multiplier.</param>
    Task SetVolumeAsync(double volume);
}
