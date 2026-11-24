using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Core contract for speech recognition engines with live microphone volume level streaming.
/// </summary>
public interface IVoiceService : IAsyncDisposable
{
    /// <summary>Gets whether the voice engine is currently actively listening to the microphone.</summary>
    bool IsListening { get; }

    /// <summary>Gets the current real-time hardware and logical microphone state.</summary>
    MicState CurrentState { get; }

    /// <summary>Gets the display name of this recognition engine backend.</summary>
    string EngineName { get; }

    /// <summary>Gets the current real-time microphone volume level (0.0 to 1.0).</summary>
    float CurrentVolume { get; }

    /// <summary>Event triggered when speech is transcribed (both interim and final results).</summary>
    event Action<SpeechResult>? OnResult;

    /// <summary>Event triggered on microphone errors or permission denials.</summary>
    event Action<string>? OnError;

    /// <summary>Event streaming the real-time audio volume (0.0 to 1.0) from the Web Audio AnalyserNode.</summary>
    event Action<float>? OnVolumeChanged;

    /// <summary>Event triggered when listening state starts or stops.</summary>
    event Action<bool>? OnListeningStateChanged;

    /// <summary>Event triggered when the microphone hardware or logical state changes.</summary>
    event Action<MicState>? OnStateChanged;

    /// <summary>Sets the microphone state explicitly.</summary>
    void SetState(MicState state);

    /// <summary>Starts listening for verbal answers.</summary>
    Task StartListeningAsync();

    /// <summary>Stops listening.</summary>
    Task StopListeningAsync();

    /// <summary>Checks and requests microphone permission from the browser.</summary>
    /// <returns>True if permission granted, false otherwise.</returns>
    Task<bool> RequestPermissionAsync();

    /// <summary>Uses text-to-speech synthesis to vocalize a prompt.</summary>
    Task SpeakAsync(string text, double rate = 0.9, double pitch = 1.1);

    /// <summary>Cancels any active speech synthesis output.</summary>
    Task StopSpeakingAsync();
}
