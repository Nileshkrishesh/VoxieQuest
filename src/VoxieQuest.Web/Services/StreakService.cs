using System.Globalization;
using VoxieQuest.Web.Models;

namespace VoxieQuest.Web.Services;

/// <summary>
/// Child-safe educational streak management service implementing rest-day streak freezes,
/// milestone rewards, and local device date tracking.
/// </summary>
public class StreakService : IStreakService, IDisposable
{
    private readonly ISaveService _saveService;
    private readonly IProgressService _progressService;
    private ProfileStreakData _data = new();
    private bool _isInitialized;
    private bool _wasFreezeUsedToday;
    private bool _wasStreakResetRecently;
    private Func<DateOnly> _dateProvider = () => DateOnly.FromDateTime(DateTime.Now);

    private static readonly int[] Milestones = { 3, 7, 14, 30 };

    public StreakService(ISaveService saveService, IProgressService progressService)
    {
        _saveService = saveService;
        _progressService = progressService;
        _progressService.OnProfileChanged += HandleProfileChanged;
    }

    /// <inheritdoc />
    public int CurrentStreak => _data.CurrentStreak;

    /// <inheritdoc />
    public int LongestStreak => _data.LongestStreak;

    /// <inheritdoc />
    public int TotalDaysPlayed => _data.TotalDaysPlayed;

    /// <inheritdoc />
    public bool WasFreezeUsedToday => _wasFreezeUsedToday;

    /// <inheritdoc />
    public bool IsFreezeAvailable
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_data.LastFreezeDate))
            {
                return true;
            }

            if (DateOnly.TryParseExact(_data.LastFreezeDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastFreeze))
            {
                var today = _dateProvider();
                return (today.DayNumber - lastFreeze.DayNumber) >= 7;
            }

            return true;
        }
    }

    /// <inheritdoc />
    public string GentleEncouragementMessage
    {
        get
        {
            if (_wasFreezeUsedToday)
            {
                return "Voxie says: I saved your streak! You're doing amazing!";
            }

            if (_wasStreakResetRecently)
            {
                return "Voxie missed you! Let's start a new adventure today";
            }

            return CurrentStreak switch
            {
                >= 30 => "Voxie says: Legend! 30+ days of learning magic!",
                >= 14 => "Voxie says: Two whole weeks! You're a superstar!",
                >= 7 => "Voxie says: 7 days in a row! Unstoppable!",
                >= 3 => "Voxie says: 3 days strong! Keep shining!",
                _ => "Ready to speak, play, and win stars?"
            };
        }
    }

    /// <inheritdoc />
    public event Action? OnStreakUpdated;

    /// <inheritdoc />
    public event Action<StreakMilestoneEventArgs>? OnMilestoneReached;

    /// <inheritdoc />
    public event Action<string>? OnFreezeUsed;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _wasFreezeUsedToday = false;
        _wasStreakResetRecently = false;
        var profileId = _progressService.ActiveProfile?.Id ?? "default";
        var storageKey = $"voxie_streak_{profileId}";

        var loaded = await _saveService.GetItemAsync<ProfileStreakData>(storageKey);
        if (loaded != null)
        {
            _data = loaded;
            _data.ProfileId = profileId;
            _data.History ??= new Dictionary<string, DailyActivity>(StringComparer.OrdinalIgnoreCase);
            _data.UnlockedMilestones ??= new List<int>();
        }
        else
        {
            _data = new ProfileStreakData
            {
                ProfileId = profileId,
                History = new Dictionary<string, DailyActivity>(StringComparer.OrdinalIgnoreCase),
                UnlockedMilestones = new List<int>()
            };
        }

        // Validate current streak against today's date
        EvaluateStreakIntegrityOnLoad();
        _isInitialized = true;
        OnStreakUpdated?.Invoke();
    }

    /// <inheritdoc />
    public async Task RecordRoundPlayedAsync(int starsEarned = 1)
    {
        var today = _dateProvider();
        await RecordPlayForDateAsync(today, 1, Math.Max(0, starsEarned));
    }

    /// <inheritdoc />
    public async Task RecordPlayForDateAsync(DateOnly date, int rounds, int stars)
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        var dateKey = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // 1. Check if this date was already played
        if (_data.History.TryGetValue(dateKey, out var existingActivity))
        {
            // Same day played twice -> increment rounds and stars, do not advance streak counter
            existingActivity.RoundsPlayed += rounds;
            existingActivity.StarsEarned += stars;
            existingActivity.LastPlayedTimestamp = DateTime.UtcNow;
            await SaveDataAsync();
            OnStreakUpdated?.Invoke();
            return;
        }

        // 2. New day played
        _data.History[dateKey] = new DailyActivity
        {
            RoundsPlayed = rounds,
            StarsEarned = stars,
            LastPlayedTimestamp = DateTime.UtcNow
        };
        _data.TotalDaysPlayed++;

        // 3. Compute Streak
        if (string.IsNullOrWhiteSpace(_data.LastPlayedDate) ||
            !DateOnly.TryParseExact(_data.LastPlayedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastPlayed))
        {
            // First ever day
            _data.CurrentStreak = 1;
            _wasStreakResetRecently = false;
        }
        else
        {
            int dayDiff = date.DayNumber - lastPlayed.DayNumber;

            if (dayDiff <= 0)
            {
                // Playing out of chronological order (e.g. backfilled or clock adjustment)
                // Maintain current streak without corrupting it
                if (_data.CurrentStreak == 0) _data.CurrentStreak = 1;
            }
            else if (dayDiff == 1)
            {
                // Perfect consecutive day!
                _data.CurrentStreak++;
                _wasStreakResetRecently = false;
                _wasFreezeUsedToday = false;
            }
            else if (dayDiff == 2)
            {
                // Missed exactly 1 day: check if Streak Freeze is available (1 per 7 days)
                if (IsFreezeAvailable)
                {
                    var freezeDate = date.AddDays(-1);
                    _data.LastFreezeDate = freezeDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    _data.CurrentStreak++; // Streak is protected!
                    _wasFreezeUsedToday = true;
                    _wasStreakResetRecently = false;
                    OnFreezeUsed?.Invoke("Voxie saved your streak!");
                }
                else
                {
                    // No freeze available: gentle restart from 1
                    _data.CurrentStreak = 1;
                    _wasStreakResetRecently = true;
                    _wasFreezeUsedToday = false;
                }
            }
            else
            {
                // Gap of 2 or more days: gentle restart from 1
                _data.CurrentStreak = 1;
                _wasStreakResetRecently = true;
                _wasFreezeUsedToday = false;
            }
        }

        // 4. Update Longest Streak
        if (_data.CurrentStreak > _data.LongestStreak)
        {
            _data.LongestStreak = _data.CurrentStreak;
        }

        _data.LastPlayedDate = dateKey;

        // 5. Milestone Checks (3, 7, 14, 30 days)
        CheckMilestones();

        // 6. Persist & Notify
        await SaveDataAsync();
        OnStreakUpdated?.Invoke();
    }

    /// <inheritdoc />
    public DailyActivity? GetActivityForDate(DateOnly date)
    {
        var key = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return _data.History.TryGetValue(key, out var act) ? act : null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, DailyActivity> GetActivityHistory()
    {
        return _data.History;
    }

    /// <inheritdoc />
    public List<StreakDayInfo> GetCurrentWeekDays()
    {
        var today = _dateProvider();
        // Monday = 0, Sunday = 6
        int offsetFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-offsetFromMonday);

        var weekDays = new List<StreakDayInfo>(7);
        for (int i = 0; i < 7; i++)
        {
            var dayDate = monday.AddDays(i);
            var key = dayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            _data.History.TryGetValue(key, out var activity);

            bool isFreeze = !string.IsNullOrWhiteSpace(_data.LastFreezeDate) &&
                            _data.LastFreezeDate.Equals(key, StringComparison.OrdinalIgnoreCase);

            weekDays.Add(new StreakDayInfo
            {
                Date = dayDate,
                RoundsPlayed = activity?.RoundsPlayed ?? 0,
                StarsEarned = activity?.StarsEarned ?? 0,
                IsToday = dayDate == today,
                IsFreezeDay = isFreeze
            });
        }

        return weekDays;
    }

    /// <inheritdoc />
    public List<StreakWeekColumn> GetTwelveWeeksMatrix()
    {
        var today = _dateProvider();
        int offsetFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var thisMonday = today.AddDays(-offsetFromMonday);
        var matrixStartMonday = thisMonday.AddDays(-11 * 7); // 12 weeks total

        var columns = new List<StreakWeekColumn>(12);
        string? previousMonth = null;

        for (int w = 0; w < 12; w++)
        {
            var colMonday = matrixStartMonday.AddDays(w * 7);
            var col = new StreakWeekColumn
            {
                WeekNumber = w + 1
            };

            // Check if month changes during this week for header label
            string monthName = colMonday.ToString("MMM", CultureInfo.InvariantCulture);
            if (previousMonth == null || monthName != previousMonth)
            {
                col.MonthLabel = monthName;
                previousMonth = monthName;
            }

            for (int d = 0; d < 7; d++)
            {
                var dayDate = colMonday.AddDays(d);
                var key = dayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                _data.History.TryGetValue(key, out var act);

                bool isFreeze = !string.IsNullOrWhiteSpace(_data.LastFreezeDate) &&
                                _data.LastFreezeDate.Equals(key, StringComparison.OrdinalIgnoreCase);

                col.Days.Add(new StreakDayInfo
                {
                    Date = dayDate,
                    RoundsPlayed = act?.RoundsPlayed ?? 0,
                    StarsEarned = act?.StarsEarned ?? 0,
                    IsToday = dayDate == today,
                    IsFreezeDay = isFreeze
                });
            }

            columns.Add(col);
        }

        return columns;
    }

    /// <inheritdoc />
    public double GetWeeklyConsistencyPercentage()
    {
        var today = _dateProvider();
        int playedDaysInLast28 = 0;

        for (int i = 0; i < 28; i++)
        {
            var d = today.AddDays(-i);
            var key = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_data.History.TryGetValue(key, out var act) && act.RoundsPlayed > 0)
            {
                playedDaysInLast28++;
            }
        }

        return Math.Round((playedDaysInLast28 / 28.0) * 100.0, 1);
    }

    /// <inheritdoc />
    public void SetCurrentDateProvider(Func<DateOnly> provider)
    {
        _dateProvider = provider ?? (() => DateOnly.FromDateTime(DateTime.Now));
    }

    /// <inheritdoc />
    public void ResetToDefaultDateProvider()
    {
        _dateProvider = () => DateOnly.FromDateTime(DateTime.Now);
    }

    private void EvaluateStreakIntegrityOnLoad()
    {
        if (string.IsNullOrWhiteSpace(_data.LastPlayedDate) ||
            !DateOnly.TryParseExact(_data.LastPlayedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastPlayed))
        {
            return;
        }

        var today = _dateProvider();
        int diff = today.DayNumber - lastPlayed.DayNumber;

        // If today or yesterday, streak is still alive!
        if (diff <= 1)
        {
            return;
        }

        // If exactly 2 days ago (missed yesterday) and freeze is available, streak is protected!
        if (diff == 2 && IsFreezeAvailable)
        {
            return;
        }

        // Missed too many days without a freeze -> gentle reset
        _data.CurrentStreak = 0;
        _wasStreakResetRecently = true;
    }

    private void CheckMilestones()
    {
        foreach (var m in Milestones)
        {
            if (_data.CurrentStreak >= m && !_data.UnlockedMilestones.Contains(m))
            {
                _data.UnlockedMilestones.Add(m);

                string badgeId = $"streak_{m}";
                string title = m switch
                {
                    3 => "3-Day Flame!",
                    7 => "Week Champion!",
                    14 => "Fortnight Master!",
                    30 => "Monthly Legend!",
                    _ => $"{m}-Day Streak!"
                };

                string message = m switch
                {
                    3 => "You've played for 3 days in a row! Here's your Flame Sticker!",
                    7 => "A full week of learning! Voxie is dancing with joy!",
                    14 => "Two whole weeks of speaking adventures! Super star!",
                    30 => "30 days of courage and play! You are a true Voxie Quest legend!",
                    _ => $"{m} streak days achieved!"
                };

                // Unlock badge in child profile if available
                if (_progressService.ActiveProfile != null &&
                    !_progressService.ActiveProfile.Badges.Contains(badgeId))
                {
                    _ = _progressService.AwardBadgeAsync(badgeId);
                }

                OnMilestoneReached?.Invoke(new StreakMilestoneEventArgs(m, badgeId, title, message));
            }
        }
    }

    private async Task SaveDataAsync()
    {
        var profileId = _progressService.ActiveProfile?.Id ?? "default";
        var storageKey = $"voxie_streak_{profileId}";
        await _saveService.SetItemAsync(storageKey, _data);
    }

    private async void HandleProfileChanged()
    {
        await InitializeAsync();
    }

    public void Dispose()
    {
        _progressService.OnProfileChanged -= HandleProfileChanged;
    }
}
