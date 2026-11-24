using Microsoft.JSInterop;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Offline voice service that uses a WebAssembly recognizer (Vosk-browser) loaded on demand.
/// Runs 100% locally in the browser sandbox without requiring an external internet connection.
/// </summary>
public class OfflineVoiceService : IVoiceService
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<OfflineVoiceService>? _dotNetRef;
    private bool _isListening = false;
    private float _currentVolume = 0.0f;
    private MicState _currentState = MicState.Idle;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineVoiceService"/> class.
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime.</param>
    public OfflineVoiceService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public bool IsListening => _isListening;

    /// <inheritdoc />
    public MicState CurrentState => _currentState;

    /// <inheritdoc />
    public string EngineName => "Offline WASM (Vosk)";

    /// <inheritdoc />
    public float CurrentVolume => _currentVolume;

    /// <inheritdoc />
    public event Action<SpeechResult>? OnResult;

    /// <inheritdoc />
    public event Action<string>? OnError;

    /// <inheritdoc />
    public event Action<float>? OnVolumeChanged;

    /// <inheritdoc />
    public event Action<bool>? OnListeningStateChanged;

    /// <inheritdoc />
    public event Action<MicState>? OnStateChanged;

    /// <inheritdoc />
    public void SetState(MicState state)
    {
        if (_currentState != state)
        {
            _currentState = state;
            OnStateChanged?.Invoke(state);
        }
    }

    /// <inheritdoc />
    public async Task StartListeningAsync()
    {
        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("voxieOfflineVoice.startListening", _dotNetRef);
            _isListening = true;
            OnListeningStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            _isListening = false;
            OnListeningStateChanged?.Invoke(false);
            OnError?.Invoke($"Offline speech initialization error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task StopListeningAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieOfflineVoice.stopListening");
        }
        catch
        {
            // Ignore teardown issues
        }
        finally
        {
            _isListening = false;
            _currentVolume = 0.0f;
            OnListeningStateChanged?.Invoke(false);
            OnVolumeChanged?.Invoke(0.0f);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("voxieVoiceService.requestPermission");
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task SpeakAsync(string text, double rate = 0.9, double pitch = 1.1)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieVoiceService.speak", text, rate, pitch);
        }
        catch
        {
            // Ignore speech synthesis failures in offline mode
        }
    }

    /// <inheritdoc />
    public async Task StopSpeakingAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieVoiceService.stopSpeaking");
        }
        catch
        {
            // Ignore stop errors
        }
    }

    /// <summary>
    /// Invoked by voskInterop.js when an offline speech result is generated.
    /// </summary>
    [JSInvokable]
    public void OnJsSpeechResult(string transcript, double confidence, bool isFinal)
    {
        OnResult?.Invoke(new SpeechResult
        {
            Transcript = transcript,
            Confidence = confidence,
            IsFinal = isFinal
        });
    }

    /// <summary>
    /// Invoked by voskInterop.js / voice.js when volume level changes.
    /// </summary>
    [JSInvokable]
    public void OnJsVolumeChanged(float volume)
    {
        _currentVolume = volume;
        OnVolumeChanged?.Invoke(volume);
    }

    /// <summary>
    /// Invoked by voskInterop.js when listening status toggles.
    /// </summary>
    [JSInvokable]
    public void OnJsListeningStatus(bool isListening)
    {
        _isListening = isListening;
        OnListeningStateChanged?.Invoke(isListening);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopListeningAsync();
        }
        catch
        {
            // Ignore disposal errors
        }
        finally
        {
            _dotNetRef?.Dispose();
        }
    }
}
