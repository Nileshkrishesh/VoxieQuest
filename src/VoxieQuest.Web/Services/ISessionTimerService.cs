namespace VoxieQuest.Web.Services;

/// <summary>
/// Monitors child screen time and triggers gentle rest breaks ("Voxie is sleepy, time to rest!").
/// </summary>
public interface ISessionTimerService
{
    /// <summary>Gets the elapsed play time in the current session.</summary>
    TimeSpan ElapsedSessionTime { get; }

    /// <summary>Gets the configured session limit in minutes (0 means unlimited/disabled).</summary>
    int SessionLimitMinutes { get; }

    /// <summary>Gets whether the screen time break is currently due.</summary>
    bool IsBreakDue { get; }

    /// <summary>Fired on every second tick of the session timer.</summary>
    event Action? OnTimerTick;

    /// <summary>Fired when the configured session limit is reached.</summary>
    event Action? OnBreakTriggered;

    /// <summary>Initializes and loads configured limit preferences.</summary>
    Task InitializeAsync();

    /// <summary>Starts or resumes the continuous session timer.</summary>
    void Start();

    /// <summary>Pauses the session timer (e.g. when app is backgrounded).</summary>
    void Pause();

    /// <summary>Sets the maximum session limit in minutes (e.g. 15, 20, 30, 45, or 0 for off).</summary>
    Task SetSessionLimitMinutesAsync(int minutes);

    /// <summary>Extends the current session by additional minutes (unlocked via Parent Gate).</summary>
    void ExtendSession(int extraMinutes);

    /// <summary>Resets the session timer after the child completes a break.</summary>
    void ResetBreak();
}
