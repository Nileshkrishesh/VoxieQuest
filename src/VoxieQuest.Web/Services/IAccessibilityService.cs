namespace VoxieQuest.Web.Services;

/// <summary>
/// Child-friendly accessibility settings including colorblind assistance,
/// dyslexia-friendly typography, speech speed control, and audio adjustments.
/// </summary>
public interface IAccessibilityService
{
    /// <summary>Gets whether high-contrast colorblind assistance mode is enabled.</summary>
    bool IsColorblindMode { get; }

    /// <summary>Gets whether the dyslexia-friendly high-legibility font is enabled.</summary>
    bool IsDyslexiaFont { get; }

    /// <summary>Gets the spoken speech rate multiplier (0.6x to 1.3x).</summary>
    double SpeechRate { get; }

    /// <summary>Gets the master sound effects volume (0.0 to 1.0).</summary>
    double MasterVolume { get; }

    /// <summary>Gets the voice prompts volume (0.0 to 1.0).</summary>
    double VoiceVolume { get; }

    /// <summary>Gets whether audio is muted.</summary>
    bool IsMuted { get; }

    /// <summary>Gets timer duration multiplier: 1.0 (standard), 2.0 (extra time), 0.0 (timers disabled).</summary>
    double TimerMultiplier { get; }

    /// <summary>Gets whether ADHD mode is active (5-round focused bursts with enhanced progress visualization).</summary>
    bool IsAdhdMode { get; }

    /// <summary>Gets whether motor-assistance hold-to-confirm is active instead of fast single-tap.</summary>
    bool IsHoldToConfirm { get; }

    /// <summary>Gets whether oversized tap targets are active for motor assistance.</summary>
    bool IsLargeTargets { get; }

    /// <summary>Fired when any accessibility setting changes.</summary>
    event Action? OnSettingsChanged;

    /// <summary>Loads saved settings from browser storage.</summary>
    Task InitializeAsync();

    /// <summary>Toggles or sets the colorblind accessibility mode.</summary>
    Task SetColorblindModeAsync(bool enabled);

    /// <summary>Toggles or sets the dyslexia-friendly typography mode.</summary>
    Task SetDyslexiaFontAsync(bool enabled);

    /// <summary>Sets the speech rate for TTS prompts (0.6 to 1.3).</summary>
    Task SetSpeechRateAsync(double rate);

    /// <summary>Sets the master audio volume (0.0 to 1.0).</summary>
    Task SetMasterVolumeAsync(double volume);

    /// <summary>Sets the voice prompt volume (0.0 to 1.0).</summary>
    Task SetVoiceVolumeAsync(double volume);

    /// <summary>Sets the mute status.</summary>
    Task SetMutedAsync(bool muted);

    /// <summary>Sets timer multiplier (1.0 = standard, 2.0 = extra time, 0.0 = disabled).</summary>
    Task SetTimerMultiplierAsync(double multiplier);

    /// <summary>Sets ADHD focused burst mode.</summary>
    Task SetAdhdModeAsync(bool enabled);

    /// <summary>Sets motor-assistance hold-to-confirm mode.</summary>
    Task SetHoldToConfirmAsync(bool enabled);

    /// <summary>Sets extra large tap target mode.</summary>
    Task SetLargeTargetsAsync(bool enabled);
}
