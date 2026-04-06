using System;
using System.Threading;
using System.Threading.Tasks;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Models;

namespace Zakarni.Core.Services;

public class ScheduleTimerService
{
    private readonly IPrayerTimeService _prayerService;
    private readonly ILocationService _locationService;
    private readonly IAudioService _audioService;
    private readonly ISettingsService _settings;
    private PeriodicTimer? _timer;
    private DateTime? _lastTriggeredPrayer;
    private DateTime _lastDate;
    private bool _muteNextAdhan = false;

    /// <summary>Fired when a prayer time is reached (for UI animation).</summary>
    public event Action<PrayerName>? PrayerTriggered;

    /// <summary>Fired when a reminder is triggered.</summary>
    public event Action<string, string>? ReminderTriggered;

    /// <summary>Fired every second with fresh prayer data (for UI updates).</summary>
    public event Action<PrayerTimeData>? Tick;

    private DateTime _lastProphetReminder = DateTime.MinValue;

    public bool MuteNextAdhan
    {
        get => _muteNextAdhan;
        set => _muteNextAdhan = value;
    }

    public ScheduleTimerService(
        IPrayerTimeService prayerService,
        ILocationService locationService,
        IAudioService audioService,
        ISettingsService settings)
    {
        _prayerService = prayerService;
        _locationService = locationService;
        _audioService = audioService;
        _settings = settings;
        _lastDate = DateTime.Today;
        _lastProphetReminder = DateTime.Now;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var location = await _locationService.GetCurrentLocationAsync();

        while (await _timer.WaitForNextTickAsync(ct))
        {
            var now = DateTime.Now;

            // Midnight rollover — refetch times
            if (now.Date != _lastDate)
            {
                _prayerService.InvalidateCache();
                _lastDate = now.Date;
                _lastTriggeredPrayer = null;
            }

            try
            {
                var times = await _prayerService.CalculatePrayerTimesAsync(
                    now, location, _settings.CalculationMethod, _settings.Madhab);

                Tick?.Invoke(times);

                // Check each adhan-eligible prayer
                CheckAndPlay(now, times, PrayerName.Fajr);
                CheckAndPlay(now, times, PrayerName.Dhuhr);
                CheckAndPlay(now, times, PrayerName.Asr);
                CheckAndPlay(now, times, PrayerName.Maghrib);
                CheckAndPlay(now, times, PrayerName.Isha);

                // Check reminders
                CheckReminders(now, times);
            }
            catch { /* Swallow — don't crash the timer loop */ }
        }
    }

    private void CheckReminders(DateTime now, PrayerTimeData times)
    {
        // 1. Pray for Prophet
        if (_settings.PrayForProphetEnabled)
        {
            var interval = TimeSpan.FromMinutes(_settings.PrayForProphetIntervalMinutes);
            if (now - _lastProphetReminder >= interval)
            {
                _lastProphetReminder = now;
                var title = _settings.CurrentLanguage == Language.Arabic ? "تذكير" : "Reminder";
                var msg = _settings.CurrentLanguage == Language.Arabic ? "صلّ على النبي ﷺ" : "Pray for the Prophet ﷺ";
                ReminderTriggered?.Invoke(title, msg);
                _audioService.PlayNotificationSoundAsync();
            }
        }

        // 2. Morning Athkar (At Sunrise)
        if (_settings.MorningAthkarEnabled && now.Hour == times.Sunrise.Hour && now.Minute == times.Sunrise.Minute && now.Second == 0)
        {
            var title = _settings.CurrentLanguage == Language.Arabic ? "أذكار الصباح" : "Morning Athkar";
            var msg = _settings.CurrentLanguage == Language.Arabic ? "حان وقت أذكار الصباح" : "Time for Morning Athkar";
            ReminderTriggered?.Invoke(title, msg);
            _audioService.PlayNotificationSoundAsync();
        }

        // 3. Evening Athkar (At Maghrib)
        if (_settings.EveningAthkarEnabled && now.Hour == times.Maghrib.Hour && now.Minute == times.Maghrib.Minute && now.Second == 0)
        {
            var title = _settings.CurrentLanguage == Language.Arabic ? "أذكار المساء" : "Evening Athkar";
            var msg = _settings.CurrentLanguage == Language.Arabic ? "حان وقت أذكار المساء" : "Time for Evening Athkar";
            ReminderTriggered?.Invoke(title, msg);
            _audioService.PlayNotificationSoundAsync();
        }
    }

    public void TriggerReminder(string title, string message)
    {
        ReminderTriggered?.Invoke(title, message);
        _audioService.PlayNotificationSoundAsync();
    }

    private string GetLocalizedPrayer(string prayer, bool isArabic)
    {
        if (!isArabic) return prayer;
        return prayer switch {
            "Fajr" => "الفجر",
            "Sunrise" => "الشروق",
            "Dhuhr" => "الظهر",
            "Asr" => "العصر",
            "Maghrib" => "المغرب",
            "Isha" => "العشاء",
            _ => prayer
        };
    }

    private void CheckAndPlay(DateTime now, PrayerTimeData times, PrayerName prayer)
    {
        var target = times.TimeFor(prayer);
        if (now.Hour == target.Hour && now.Minute == target.Minute && now.Second == 0)
        {
            // Don't re-trigger the same prayer
            if (_lastTriggeredPrayer.HasValue &&
                _lastTriggeredPrayer.Value.Hour == target.Hour &&
                _lastTriggeredPrayer.Value.Minute == target.Minute)
                return;

            _lastTriggeredPrayer = now;
            PrayerTriggered?.Invoke(prayer);

            // 1. Notify that prayer time has arrived
            bool isArabic = _settings.CurrentLanguage == Language.Arabic;
            var title = isArabic ? "حان وقت الصلاة" : "Prayer Time";
            var msg = isArabic ? $"حان الآن موعد أذان {GetLocalizedPrayer(prayer.ToString(), isArabic)}" : $"It is now time for {prayer.ToString()}";
            TriggerReminder(title, msg);

            // After prayer Athkar
            if (_settings.AfterPrayerAthkarEnabled && prayer != PrayerName.Sunrise)
            {
                var athkarTitle = isArabic ? "أذكار بعد الصلاة" : "After Prayer Athkar";
                var athkarMsg = isArabic ? "لا تنس أذكار ما بعد الصلاة" : "Don't forget After Prayer Athkar";
                TriggerReminder(athkarTitle, athkarMsg);
            }

            // Check per-prayer + mute settings
            if (_muteNextAdhan)
            {
                _muteNextAdhan = false;
                return;
            }

            if (_settings.AdhanEnabled.TryGetValue(prayer, out var enabled) && !enabled)
                return;

            _audioService.PlayAdhanAsync();
        }
    }
}
