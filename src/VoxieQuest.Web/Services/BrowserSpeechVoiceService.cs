using Microsoft.JSInterop;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Voice service implementation wrapping the Web Speech API (SpeechRecognition / webkitSpeechRecognition)
/// and Web Audio API AnalyserNode via thin JavaScript interop in voice.js.
/// </summary>
public class BrowserSpeechVoiceService : IVoiceService
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<BrowserSpeechVoiceService>? _dotNetRef;
    private bool _isListening = false;
    private float _currentVolume = 0.0f;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserSpeechVoiceService"/> class.
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime instance.</param>
    public BrowserSpeechVoiceService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public bool IsListening => _isListening;

    /// <inheritdoc />
    public string EngineName => "Browser Web Speech";

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
    public async Task StartListeningAsync()
    {
        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("voxieVoiceService.startListening", _dotNetRef, false);
            _isListening = true;
            OnListeningStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            _isListening = false;
            OnListeningStateChanged?.Invoke(false);
            OnError?.Invoke($"Microphone initialization error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task StopListeningAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieVoiceService.stopListening");
        }
        catch
        {
            // Ignore JS interop teardown exceptions
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
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("voxieVoiceService.speak", text, rate, pitch, _dotNetRef);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Speech synthesis error: {ex.Message}");
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
            // Ignore speech synthesis halt exceptions
        }
    }

    /// <summary>
    /// Invoked by voice.js when speech recognition yields an interim or final transcription.
    /// </summary>
    [JSInvokable]
    public void OnJsSpeechResult(string transcript, double confidence, bool isFinal)
    {
        var result = new SpeechResult
        {
            Transcript = transcript,
            Confidence = confidence,
            IsFinal = isFinal
        };

        OnResult?.Invoke(result);
    }

    /// <summary>
    /// Invoked by voice.js when the Web Audio AnalyserNode updates the 0.0 - 1.0 audio volume.
    /// </summary>
    [JSInvokable]
    public void OnJsVolumeChanged(float volume)
    {
        _currentVolume = volume;
        OnVolumeChanged?.Invoke(volume);
    }

    /// <summary>
    /// Invoked by voice.js when microphone listening toggles on or off.
    /// </summary>
    [JSInvokable]
    public void OnJsListeningStatus(bool isListening)
    {
        _isListening = isListening;
        OnListeningStateChanged?.Invoke(isListening);
    }

    /// <summary>
    /// Invoked by voice.js when speech synthesis concludes.
    /// </summary>
    [JSInvokable]
    public void OnJsSpeechFinished()
    {
        // Vocalization finished
    }

    /// <summary>
    /// Invoked by voice.js on recognition or microphone errors.
    /// </summary>
    [JSInvokable]
    public void OnJsError(string error)
    {
        _isListening = false;
        OnError?.Invoke(error);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopListeningAsync();
            await StopSpeakingAsync();
        }
        catch
        {
            // Silently dispose
        }
        finally
        {
            _dotNetRef?.Dispose();
        }
    }
}
