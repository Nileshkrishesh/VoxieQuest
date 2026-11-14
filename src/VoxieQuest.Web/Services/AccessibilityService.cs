using System.Text.Json;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements accessibility options with localStorage persistence and reactive notifications.
/// </summary>
public class AccessibilityService : IAccessibilityService
{
    private const string StorageKey = "voxie_accessibility_prefs";
    private readonly ISaveService _saveService;
    private readonly IAudioService _audioService;

    private bool _isColorblindMode = false;
    private bool _isDyslexiaFont = false;
    private double _speechRate = 0.9;
    private double _masterVolume = 0.8;
    private double _voiceVolume = 1.0;
    private bool _isMuted = false;
    private bool _isInitialized = false;

    public AccessibilityService(ISaveService saveService, IAudioService audioService)
    {
        _saveService = saveService;
        _audioService = audioService;
    }

    /// <inheritdoc />
    public bool IsColorblindMode => _isColorblindMode;

    /// <inheritdoc />
    public bool IsDyslexiaFont => _isDyslexiaFont;

    /// <inheritdoc />
    public double SpeechRate => _speechRate;

    /// <inheritdoc />
    public double MasterVolume => _masterVolume;

    /// <inheritdoc />
    public double VoiceVolume => _voiceVolume;

    /// <inheritdoc />
    public bool IsMuted => _isMuted;

    /// <inheritdoc />
    public event Action? OnSettingsChanged;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            var savedJson = await _saveService.GetItemAsync<string>(StorageKey);
            if (!string.IsNullOrWhiteSpace(savedJson))
            {
                var prefs = JsonSerializer.Deserialize<AccessibilityDto>(savedJson);
                if (prefs != null)
                {
                    _isColorblindMode = prefs.Colorblind;
                    _isDyslexiaFont = prefs.Dyslexia;
                    _speechRate = Math.Clamp(prefs.SpeechRate, 0.6, 1.4);
                    _masterVolume = Math.Clamp(prefs.MasterVolume, 0.0, 1.0);
                    _voiceVolume = Math.Clamp(prefs.VoiceVolume, 0.0, 1.0);
                    _isMuted = prefs.Muted;

                    await _audioService.SetVolumeAsync(_masterVolume);
                    await _audioService.SetMutedAsync(_isMuted);
                }
            }
        }
        catch
        {
            // Default settings retained if read fails
        }

        OnSettingsChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetColorblindModeAsync(bool enabled)
    {
        _isColorblindMode = enabled;
        await PersistAsync();
        OnSettingsChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetDyslexiaFontAsync(bool enabled)
    {
        _isDyslexiaFont = enabled;
        await PersistAsync();
        OnSettingsChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetSpeechRateAsync(double rate)
    {
        _speechRate = Math.Clamp(rate, 0.6, 1.4);
        await PersistAsync();
        OnSettingsChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetMasterVolumeAsync(double volume)
    {
        _masterVolume = Math.Clamp(volume, 0.0, 1.0);
        await _audioService.SetVolumeAsync(_masterVolume);
        await PersistAsync();
        OnSettingsChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetVoiceVolumeAsync(double volume)
    {
        _voiceVolume = Math.Clamp(volume, 0.0, 1.0);
        await PersistAsync();
        OnSettingsChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetMutedAsync(bool muted)
    {
        _isMuted = muted;
        await _audioService.SetMutedAsync(muted);
        await PersistAsync();
        OnSettingsChanged?.Invoke();
    }

    private async Task PersistAsync()
    {
        try
        {
            var dto = new AccessibilityDto
            {
                Colorblind = _isColorblindMode,
                Dyslexia = _isDyslexiaFont,
                SpeechRate = _speechRate,
                MasterVolume = _masterVolume,
                VoiceVolume = _voiceVolume,
                Muted = _isMuted
            };
            await _saveService.SetItemAsync(StorageKey, JsonSerializer.Serialize(dto));
        }
        catch
        {
            // Silently ignore storage save issues
        }
    }

    private class AccessibilityDto
    {
        public bool Colorblind { get; set; }
        public bool Dyslexia { get; set; }
        public double SpeechRate { get; set; } = 0.9;
        public double MasterVolume { get; set; } = 0.8;
        public double VoiceVolume { get; set; } = 1.0;
        public bool Muted { get; set; }
    }
}
