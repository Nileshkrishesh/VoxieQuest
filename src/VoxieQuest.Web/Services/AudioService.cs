using Microsoft.JSInterop;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements child-friendly audio synthesis using Web Audio API via JavaScript interop.
/// </summary>
public class AudioService : IAudioService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isMuted = false;
    private double _volume = 0.8;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioService"/> class.
    /// </summary>
    /// <param name="jsRuntime">JavaScript runtime.</param>
    public AudioService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public bool IsMuted => _isMuted;

    /// <inheritdoc />
    public double Volume => _volume;

    /// <inheritdoc />
    public async Task PlaySoundAsync(SoundEffect effect)
    {
        if (_isMuted) return;

        var effectName = effect switch
        {
            SoundEffect.Click => "click",
            SoundEffect.Pop => "pop",
            SoundEffect.SuccessChime => "success",
            SoundEffect.EncourageChime => "encourage",
            SoundEffect.Fanfare => "fanfare",
            SoundEffect.StarBurst => "star",
            _ => "click"
        };

        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieAudio.playEffect", effectName, _volume);
        }
        catch
        {
            // Silently ignore audio playback failures in restrictive browser policies
        }
    }

    /// <inheritdoc />
    public async Task PlayChimeAsync(bool success)
    {
        await PlaySoundAsync(success ? SoundEffect.SuccessChime : SoundEffect.EncourageChime);
    }

    /// <inheritdoc />
    public async Task PlayAnimalSoundAsync(string animalName)
    {
        if (_isMuted || string.IsNullOrWhiteSpace(animalName)) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieAudio.playAnimalSound", animalName.Trim().ToLowerInvariant(), _volume);
        }
        catch
        {
            // Silently fallback if audio output is restricted
        }
    }

    /// <inheritdoc />
    public async Task SetMutedAsync(bool muted)
    {
        _isMuted = muted;
        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieAudio.setMuted", muted);
        }
        catch
        {
            // Ignore JS failures
        }
    }

    /// <inheritdoc />
    public async Task SetVolumeAsync(double volume)
    {
        _volume = Math.Clamp(volume, 0.0, 1.0);
        try
        {
            await _jsRuntime.InvokeVoidAsync("voxieAudio.setVolume", _volume);
        }
        catch
        {
            // Ignore JS failures
        }
    }
}
