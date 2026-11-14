using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Orchestrates speech recognition backends (Browser Web Speech, Offline Vosk, and Practice Mode),
/// switching engines automatically on failure or permission denial.
/// </summary>
public class VoiceManager : IVoiceService
{
    private readonly BrowserSpeechVoiceService _browserService;
    private readonly OfflineVoiceService _offlineService;
    private IVoiceService _activeService;
    private VoiceEngineType _currentEngine = VoiceEngineType.BrowserWebSpeech;
    private VoiceMode _mode = VoiceMode.AutoListen;
    private float _currentVolume = 0.0f;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceManager"/> class.
    /// </summary>
    public VoiceManager(BrowserSpeechVoiceService browserService, OfflineVoiceService offlineService)
    {
        _browserService = browserService;
        _offlineService = offlineService;
        _activeService = _browserService;

        WireEvents(_browserService);
        WireEvents(_offlineService);
    }

    /// <summary>Gets or sets the listening operational mode (AutoListen, PushToTalk, or PracticeButtons).</summary>
    public VoiceMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    /// <summary>Gets the currently active voice recognition engine.</summary>
    public VoiceEngineType CurrentEngine => _currentEngine;

    /// <inheritdoc />
    public bool IsListening => _activeService.IsListening;

    /// <inheritdoc />
    public string EngineName => _currentEngine switch
    {
        VoiceEngineType.OfflineVosk => _offlineService.EngineName,
        VoiceEngineType.PracticeManual => "Practice Mode (Buttons & Keyboard)",
        _ => _browserService.EngineName
    };

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

    /// <summary>Event raised when the active recognition engine switches.</summary>
    public event Action<VoiceEngineType>? OnEngineSwitched;

    /// <summary>
    /// Switches the active engine manually or programmatically.
    /// </summary>
    public void SetEngine(VoiceEngineType engineType)
    {
        if (_currentEngine == engineType) return;

        _activeService.StopListeningAsync();
        _currentEngine = engineType;

        _activeService = engineType switch
        {
            VoiceEngineType.OfflineVosk => _offlineService,
            VoiceEngineType.PracticeManual => _browserService, // Dummy target for practice mode
            _ => _browserService
        };

        OnEngineSwitched?.Invoke(_currentEngine);
    }

    /// <summary>
    /// Submits a manual answer from on-screen buttons or keyboard in Practice Mode.
    /// </summary>
    /// <param name="answer">The chosen text answer.</param>
    public void SubmitManualAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer)) return;

        var result = new SpeechResult
        {
            Transcript = answer.Trim(),
            Confidence = 1.0,
            IsFinal = true
        };

        OnResult?.Invoke(result);
    }

    /// <inheritdoc />
    public async Task StartListeningAsync()
    {
        if (_currentEngine == VoiceEngineType.PracticeManual)
        {
            // Practice mode does not capture microphone
            OnListeningStateChanged?.Invoke(true);
            return;
        }

        await _activeService.StartListeningAsync();
    }

    /// <inheritdoc />
    public async Task StopListeningAsync()
    {
        if (_currentEngine == VoiceEngineType.PracticeManual)
        {
            OnListeningStateChanged?.Invoke(false);
            return;
        }

        await _activeService.StopListeningAsync();
    }

    /// <inheritdoc />
    public async Task<bool> RequestPermissionAsync()
    {
        var hasPermission = await _activeService.RequestPermissionAsync();
        if (!hasPermission)
        {
            // If browser microphone is rejected, offer fallback
            SetEngine(VoiceEngineType.PracticeManual);
        }
        return hasPermission;
    }

    /// <inheritdoc />
    public async Task SpeakAsync(string text, double rate = 0.9, double pitch = 1.1)
    {
        await _activeService.SpeakAsync(text, rate, pitch);
    }

    /// <inheritdoc />
    public async Task StopSpeakingAsync()
    {
        await _activeService.StopSpeakingAsync();
    }

    private void WireEvents(IVoiceService service)
    {
        service.OnResult += result =>
        {
            if (service == _activeService && _currentEngine != VoiceEngineType.PracticeManual)
            {
                OnResult?.Invoke(result);
            }
        };

        service.OnVolumeChanged += vol =>
        {
            if (service == _activeService)
            {
                _currentVolume = vol;
                OnVolumeChanged?.Invoke(vol);
            }
        };

        service.OnListeningStateChanged += state =>
        {
            if (service == _activeService)
            {
                OnListeningStateChanged?.Invoke(state);
            }
        };

        service.OnError += err =>
        {
            if (service == _activeService)
            {
                // Auto-fallback: if browser API fails or lacks permission, offer offline or practice mode
                if (err.Contains("not supported", StringComparison.OrdinalIgnoreCase) ||
                    err.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
                    err.Contains("permission", StringComparison.OrdinalIgnoreCase))
                {
                    SetEngine(VoiceEngineType.PracticeManual);
                }

                OnError?.Invoke(err);
            }
        };
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _browserService.DisposeAsync();
        await _offlineService.DisposeAsync();
    }
}
