using Microsoft.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Models;
using Zakarni.Core.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
namespace Zakarni.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPrayerTimeService _prayerService;
    private readonly ILocationService _locationService;
    private readonly ISettingsService _settings;
    private readonly ScheduleTimerService _scheduler;
    private readonly DispatcherQueue _dispatcher;
    private readonly Microsoft.Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader;
    private PrayerTimeData? _data;

    public ObservableCollection<PrayerCardViewModel> PrayerCards { get; } = new();

    // ── Navigation ─────────────────────────────
    [ObservableProperty] private bool _isHomeViewVisible = true;
    [ObservableProperty] private bool _isSettingsViewVisible = false;

    // ── Header ─────────────────────────────────
    [ObservableProperty] private string _locationText = "Detecting location…";
    [ObservableProperty] private string _currentTime = "--:--";
    [ObservableProperty] private string _currentDate = "";
    [ObservableProperty] private string _currentIslamicDate = "";

    // ── Hero ───────────────────────────────────
    [ObservableProperty] private string _nextPrayerName = "…";
    [ObservableProperty] private string _timeRemaining = "--:--:--";
    [ObservableProperty] private string _statusText = "Calculating times…";
    [ObservableProperty] private bool _isApproaching = false;
    [ObservableProperty] private double _heroProgressValue = 0;
    [ObservableProperty] private double _heroProgressMax = 100;

    // ── Schedule ───────────────────────────────
    [ObservableProperty] private string _fajrTime = "--:--";
    [ObservableProperty] private string _sunriseTime = "--:--";
    [ObservableProperty] private string _dhuhrTime = "--:--";
    [ObservableProperty] private string _asrTime = "--:--";
    [ObservableProperty] private string _maghribTime = "--:--";
    [ObservableProperty] private string _ishaTime = "--:--";

    // ── Tray & Widget ───────────────────────────────────
    [ObservableProperty] private string _fajrText = "Fajr";
    [ObservableProperty] private string _sunriseText = "Sunrise";
    [ObservableProperty] private string _dhuhrText = "Dhuhr";
    [ObservableProperty] private string _asrText = "Asr";
    [ObservableProperty] private string _maghribText = "Maghrib";
    [ObservableProperty] private string _ishaText = "Isha";

    [ObservableProperty] private string _widgetSubtitle = "NEXT PRAYER";
    [ObservableProperty] private string _widgetTitle = "";
    [ObservableProperty] private Brush _widgetTitleColor = null!;
    [ObservableProperty] private Brush _widgetProgressColor = null!;

    // Active row highlight
    [ObservableProperty] private string _activePrayer = "";

    [ObservableProperty] private bool _isFajrActive;
    [ObservableProperty] private bool _isSunriseActive;
    [ObservableProperty] private bool _isDhuhrActive;
    [ObservableProperty] private bool _isAsrActive;
    [ObservableProperty] private bool _isMaghribActive;
    [ObservableProperty] private bool _isIshaActive;

    // ── Offline indicator ──────────────────────
    [ObservableProperty] private bool _isOffline = false;

    public MainViewModel(
        IPrayerTimeService prayerService,
        ILocationService locationService,
        ISettingsService settings,
        ScheduleTimerService scheduler)
    {
        _prayerService = prayerService;
        _locationService = locationService;
        _settings = settings;
        _scheduler = scheduler;
        
        // CRITICAL: Always use the primary UI dispatcher
        _dispatcher = App.UIDispatcher ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        CurrentDate = DateTime.Now.ToString("MMMM d, yyyy");
        try
        {
            var umAlQura = new System.Globalization.UmAlQuraCalendar();
            var dt = DateTime.Now;
            CurrentIslamicDate = $"{umAlQura.GetDayOfMonth(dt)} {GetIslamicMonthName(umAlQura.GetMonth(dt))} {umAlQura.GetYear(dt)}";
        }
        catch { CurrentIslamicDate = ""; }

        // Subscribe to scheduler
        _scheduler.Tick += OnSchedulerTick;
        
        _ = LoadAsync();
        StartClock();
    }

    /// <summary>
    /// Call this when the window is closed to prevent memory leaks and background crashes.
    /// </summary>
    public void Deactivate()
    {
        try { _scheduler.Tick -= OnSchedulerTick; } catch { }
    }

    private string GetString(string key, string fallback)
    {
        try { return _resourceLoader?.GetString(key) ?? fallback; }
        catch { return fallback; }
    }

    private async Task LoadAsync()
    {
        if (_dispatcher == null) return;
        
        try
        {
            var loc = await _locationService.GetCurrentLocationAsync();
            _dispatcher.TryEnqueue(() => LocationText = loc.ToString());

            var data = await _prayerService.CalculatePrayerTimesAsync(
                DateTime.Now, loc, _settings.CalculationMethod, _settings.Madhab);

            _dispatcher.TryEnqueue(() => ApplyPrayerData(data));
        }
        catch
        {
            _dispatcher.TryEnqueue(() =>
            {
                IsOffline = true;
                StatusText = "Offline — using approximate times";
            });
        }
    }

    private void OnSchedulerTick(PrayerTimeData data)
    {
        _dispatcher.TryEnqueue(() => ApplyPrayerData(data));
    }

    private void ApplyPrayerData(PrayerTimeData data)
    {
        if (_dispatcher == null) return;
        
        if (!_dispatcher.HasThreadAccess)
        {
            _dispatcher.TryEnqueue(() => ApplyPrayerData(data));
            return;
        }

        _data = data;
        FajrTime = data.Fajr.ToString("HH:mm");
        SunriseTime = data.Sunrise.ToString("HH:mm");
        DhuhrTime = data.Dhuhr.ToString("HH:mm");
        AsrTime = data.Asr.ToString("HH:mm");
        MaghribTime = data.Maghrib.ToString("HH:mm");
        IshaTime = data.Isha.ToString("HH:mm");

        var fajrName = GetString("Fajr", "Fajr");
        var sunriseName = GetString("Sunrise", "Sunrise");
        var dhuhrName = GetString("Dhuhr", "Dhuhr");
        var asrName = GetString("Asr", "Asr");
        var maghribName = GetString("Maghrib", "Maghrib");
        var ishaName = GetString("Isha", "Isha");

        FajrText = $"{fajrName,-10} {FajrTime}";
        SunriseText = $"{sunriseName,-10} {SunriseTime}";
        DhuhrText = $"{dhuhrName,-10} {DhuhrTime}";
        AsrText = $"{asrName,-10} {AsrTime}";
        MaghribText = $"{maghribName,-10} {MaghribTime}";
        IshaText = $"{ishaName,-10} {IshaTime}";

        NextPrayerName = data.NextPrayerName switch
        {
            "Fajr" => fajrName,
            "Sunrise" => sunriseName,
            "Dhuhr" => dhuhrName,
            "Asr" => asrName,
            "Maghrib" => maghribName,
            "Isha" => ishaName,
            _ => data.NextPrayerName
        };
        ActivePrayer = data.NextPrayerName;

        IsFajrActive = ActivePrayer == "Fajr";
        IsSunriseActive = ActivePrayer == "Sunrise";
        IsDhuhrActive = ActivePrayer == "Dhuhr";
        IsAsrActive = ActivePrayer == "Asr";
        IsMaghribActive = ActivePrayer == "Maghrib";
        IsIshaActive = ActivePrayer == "Isha";

        UpdatePrayerCards(data);
        UpdateCountdown();
    }

    private void UpdatePrayerCards(PrayerTimeData data)
    {
        if (_dispatcher == null) return;
        
        if (!_dispatcher.HasThreadAccess)
        {
            _dispatcher.TryEnqueue(() => UpdatePrayerCards(data));
            return;
        }

        var prayers = new[]
        {
            (GetString("Fajr", "Fajr"), data.Fajr, "Fajr"),
            (GetString("Sunrise", "Sunrise"), data.Sunrise, "Sunrise"),
            (GetString("Dhuhr", "Dhuhr"), data.Dhuhr, "Dhuhr"),
            (GetString("Asr", "Asr"), data.Asr, "Asr"),
            (GetString("Maghrib", "Maghrib"), data.Maghrib, "Maghrib"),
            (GetString("Isha", "Isha"), data.Isha, "Isha")
        };

        if (PrayerCards.Count == 0)
        {
            for (int i = 0; i < prayers.Length; i++) PrayerCards.Add(new PrayerCardViewModel());
        }

        for (int i = 0; i < prayers.Length; i++)
        {
            var p = prayers[i];
            var card = PrayerCards[i];
            card.Name = p.Item1;
            card.Time = p.Item2.ToString("HH:mm");
            card.IsActive = data.NextPrayerName == p.Item3;
            
            card.Background = GetCardBackground(card.IsActive);
            card.BorderBrush = GetCardBorder(card.IsActive);
            card.Foreground = GetCardForeground(card.IsActive);
            card.BorderThickness = card.IsActive ? new Microsoft.UI.Xaml.Thickness(2) : new Microsoft.UI.Xaml.Thickness(1);
        }
    }

    private Brush GetCardBackground(bool active)
    {
        if (_dispatcher == null || !_dispatcher.HasThreadAccess)
        {
            return new SolidColorBrush(active
                ? Windows.UI.Color.FromArgb(80, 16, 185, 129)   // Emerald transparent
                : Windows.UI.Color.FromArgb(40, 255, 255, 255));
        }
        if (Application.Current == null)
            return new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        return active
            ? (Brush)Application.Current.Resources["BrandAccentAlphaBrush"]
            : (Brush)Application.Current.Resources["PremiumCardAcrylicBrush"];
    }

    private Brush GetCardBorder(bool active)
    {
        if (_dispatcher == null || !_dispatcher.HasThreadAccess)
        {
            var color = active
                ? Windows.UI.Color.FromArgb(255, 16, 185, 129)    // Emerald Green
                : Microsoft.UI.Colors.Transparent;

            return new SolidColorBrush(color);
        }

        if (Application.Current == null)
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);

        return active
            ? (Brush)Application.Current.Resources["BrandPrimaryBrush"]
            : (Brush)Application.Current.Resources["CardBorderBrush"];
    }

    private Brush GetCardForeground(bool active)
    {
        if (_dispatcher == null || !_dispatcher.HasThreadAccess)
        {
            return new SolidColorBrush(Microsoft.UI.Colors.White);
        }

        if (Application.Current == null)
            return new SolidColorBrush(Microsoft.UI.Colors.White);

        return active
            ? new SolidColorBrush(Microsoft.UI.Colors.White)
            : (Brush)Application.Current.Resources["TextPrimaryBrush"];
    }

    private DateTime _last1HourNotificationTime = DateTime.MinValue;

    private string GetPreviousPrayer(string nextPrayer) => nextPrayer switch
    {
        "Fajr" => "Isha",
        "Sunrise" => "Fajr",
        "Dhuhr" => "Sunrise",
        "Asr" => "Dhuhr",
        "Maghrib" => "Asr",
        "Isha" => "Maghrib",
        _ => "Isha"
    };

    private DateTime GetPrayerTimeByName(PrayerTimeData data, string prayerName) => prayerName switch
    {
        "Fajr" => data.Fajr,
        "Sunrise" => data.Sunrise,
        "Dhuhr" => data.Dhuhr,
        "Asr" => data.Asr,
        "Maghrib" => data.Maghrib,
        "Isha" => data.Isha,
        _ => data.Isha
    };

    private string GetLocalizedPrayerNameSafe(string name)
    {
        if (_settings.CurrentLanguage == Language.Arabic)
        {
            return name switch
            {
                "Fajr" => "الفجر",
                "Sunrise" => "الشروق",
                "Dhuhr" => "الظهر",
                "Asr" => "العصر",
                "Maghrib" => "المغرب",
                "Isha" => "العشاء",
                _ => name
            };
        }
        return GetString(name, name);
    }

    private void UpdateCountdown()
    {
        if (_data == null || _dispatcher == null) return;
        
        // Final safety check to avoid COMException (0x8001010E)
        if (!_dispatcher.HasThreadAccess)
        {
            _dispatcher.TryEnqueue(() => UpdateCountdown());
            return;
        }

        var now = DateTime.Now;
        var delta = _data.NextPrayerTime - now;

        var prevPrayerNameStr = GetPreviousPrayer(_data.NextPrayerName);
        var prevPrayerTime = GetPrayerTimeByName(_data, prevPrayerNameStr);
        
        // Handle yesterday's Isha if next is Fajr
        if (prevPrayerNameStr == "Isha" && now.Hour < 12)
        {
            prevPrayerTime = prevPrayerTime.AddDays(-1);
        }
        var timeSincePrevious = now - prevPrayerTime;

        if (Application.Current == null) return;

        var primaryBrush = (Brush)Application.Current.Resources["BrandPrimaryBrush"] ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
        var accentBrush = (Brush)Application.Current.Resources["BrandAccentBrush"] ?? new SolidColorBrush(Microsoft.UI.Colors.Gold);
        var warningBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 239, 68, 68)); // Red/Warning

        string currentLocalizedNext = GetLocalizedPrayerNameSafe(_data.NextPrayerName);
        string currentLocalizedPrev = GetLocalizedPrayerNameSafe(prevPrayerNameStr);

        if (delta.TotalSeconds > 0)
        {
            TimeRemaining = $"{(int)delta.TotalHours:D2}:{delta.Minutes:D2}:{delta.Seconds:D2}";
            IsApproaching = delta.TotalMinutes <= 5;
            StatusText = $"Next: {_data.NextPrayerTime:hh:mm tt}";
            
            var totalSecondsExpected = 4 * 3600; 
            HeroProgressMax = totalSecondsExpected;
            HeroProgressValue = Math.Max(0, totalSecondsExpected - delta.TotalSeconds); 

            // Widget States
            if (timeSincePrevious.TotalMinutes >= 0 && timeSincePrevious.TotalMinutes <= 5)
            {
                WidgetSubtitle = "IT IS TIME FOR";
                WidgetTitle = currentLocalizedPrev;
                TimeRemaining = "Pray Now";
                WidgetTitleColor = accentBrush;
                WidgetProgressColor = primaryBrush;
            }
            else if (delta.TotalHours < 1.0)
            {
                WidgetSubtitle = $"DID YOU PRAY {currentLocalizedPrev.ToUpper()}?";
                WidgetTitle = currentLocalizedNext;
                WidgetTitleColor = warningBrush;
                WidgetProgressColor = warningBrush;

                // 1-Hour Notification logic
                if (now.Subtract(_last1HourNotificationTime).TotalMinutes > 60 && delta.TotalMinutes <= 60 && delta.TotalMinutes > 59)
                {
                    _last1HourNotificationTime = now;
                    var title = _settings.CurrentLanguage == Language.Arabic ? "تذكير بالصلاة" : "Prayer Reminder";
                    var msg = _settings.CurrentLanguage == Language.Arabic 
                        ? $"هل صليت {currentLocalizedPrev}؟ يتبقى أقل من ساعة على أذان {currentLocalizedNext}." 
                        : $"Did you pray {currentLocalizedPrev}? Less than an hour left until {currentLocalizedNext}.";
                    _scheduler.TriggerReminder(title, msg);
                }
            }
            else
            {
                WidgetSubtitle = "NEXT PRAYER";
                WidgetTitle = currentLocalizedNext;
                WidgetTitleColor = primaryBrush;
                WidgetProgressColor = accentBrush;
            }
        }
        else
        {
            TimeRemaining = "00:00:00";
            StatusText = "It is time";
            HeroProgressMax = 100;
            HeroProgressValue = 100;
            
            WidgetSubtitle = "IT IS TIME FOR";
            WidgetTitle = currentLocalizedNext;
            WidgetTitleColor = accentBrush;
            WidgetProgressColor = primaryBrush;
        }
    }

    private async void StartClock()
    {
        if (_dispatcher == null) return;
        var timer = new System.Threading.PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync())
        {
            _dispatcher.TryEnqueue(() =>
            {
                var now = DateTime.Now;
                CurrentTime = now.ToString("HH:mm");
                UpdateCountdown();
            });
        }
    }

    [RelayCommand]
    private void ShowApp()
    {
        App.ShowMainWindow();
    }

    [RelayCommand]
    private void MuteNextAdhan() => _scheduler.MuteNextAdhan = true;

    [RelayCommand]
    private void ExitApp() => Microsoft.UI.Xaml.Application.Current.Exit();

    [RelayCommand]
    private void NavigateHome()
    {
        IsHomeViewVisible = true;
        IsSettingsViewVisible = false;
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        IsHomeViewVisible = false;
        IsSettingsViewVisible = true;
    }

    private string GetIslamicMonthName(int month) => month switch
    {
        1 => "Muharram", 2 => "Safar", 3 => "Rabi' al-Awwal", 4 => "Rabi' al-Thani",
        5 => "Jumada al-Awwal", 6 => "Jumada al-Thani", 7 => "Rajab", 8 => "Sha'ban",
        9 => "Ramadan", 10 => "Shawwal", 11 => "Dhu al-Qi'dah", 12 => "Dhu al-Hijjah",
        _ => ""
    };
}
