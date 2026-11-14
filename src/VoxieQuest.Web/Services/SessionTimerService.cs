using System.Timers;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Implements session screen-time tracking and break scheduling.
/// </summary>
public class SessionTimerService : ISessionTimerService, IDisposable
{
    private const string StorageKey = "voxie_session_limit_minutes";
    private readonly ISaveService _saveService;
    private readonly IProgressService _progressService;
    private readonly System.Timers.Timer _timer;

    private int _elapsedSeconds = 0;
    private int _sessionLimitMinutes = 20; // 20 minutes default recommended limit
    private bool _isBreakDue = false;
    private bool _isInitialized = false;

    public SessionTimerService(ISaveService saveService, IProgressService progressService)
    {
        _saveService = saveService;
        _progressService = progressService;

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += HandleTimerElapsed;
        _timer.AutoReset = true;
    }

    /// <inheritdoc />
    public TimeSpan ElapsedSessionTime => TimeSpan.FromSeconds(_elapsedSeconds);

    /// <inheritdoc />
    public int SessionLimitMinutes => _sessionLimitMinutes;

    /// <inheritdoc />
    public bool IsBreakDue => _isBreakDue;

    /// <inheritdoc />
    public event Action? OnTimerTick;

    /// <inheritdoc />
    public event Action? OnBreakTriggered;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            var saved = await _saveService.GetItemAsync<string>(StorageKey);
            if (int.TryParse(saved, out var limit))
            {
                _sessionLimitMinutes = Math.Max(0, limit);
            }
        }
        catch
        {
            // Default 20 mins retained
        }

        Start();
    }

    /// <inheritdoc />
    public void Start()
    {
        if (!_timer.Enabled)
        {
            _timer.Start();
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        if (_timer.Enabled)
        {
            _timer.Stop();
        }
    }

    /// <inheritdoc />
    public async Task SetSessionLimitMinutesAsync(int minutes)
    {
        _sessionLimitMinutes = Math.Max(0, minutes);
        try
        {
            await _saveService.SetItemAsync(StorageKey, _sessionLimitMinutes.ToString());
        }
        catch
        {
            // Ignore storage failure
        }

        CheckBreakCondition();
        OnTimerTick?.Invoke();
    }

    /// <inheritdoc />
    public void ExtendSession(int extraMinutes)
    {
        _isBreakDue = false;
        // Shift elapsed back so extraMinutes can be played
        _elapsedSeconds = Math.Max(0, _elapsedSeconds - (extraMinutes * 60));
        Start();
        OnTimerTick?.Invoke();
    }

    /// <inheritdoc />
    public void ResetBreak()
    {
        _elapsedSeconds = 0;
        _isBreakDue = false;
        Start();
        OnTimerTick?.Invoke();
    }

    private void HandleTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _elapsedSeconds++;

        // Add playtime to child profile every 30 seconds
        if (_elapsedSeconds % 30 == 0)
        {
            _ = _progressService.AddPlayTimeSecondsAsync(30);
        }

        CheckBreakCondition();
        OnTimerTick?.Invoke();
    }

    private void CheckBreakCondition()
    {
        if (_sessionLimitMinutes > 0 && !_isBreakDue)
        {
            var limitSeconds = _sessionLimitMinutes * 60;
            if (_elapsedSeconds >= limitSeconds)
            {
                _isBreakDue = true;
                Pause();
                OnBreakTriggered?.Invoke();
            }
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
