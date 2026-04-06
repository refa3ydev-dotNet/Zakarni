using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Models;

namespace Zakarni.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IPrayerTimeService _prayerService;

    // Location
    [ObservableProperty] private bool _autoDetectLocation;

    // Calculation
    [ObservableProperty] private int _calculationMethodIndex;
    [ObservableProperty] private int _madhabIndex;

    // Audio
    [ObservableProperty] private double _volume;
    [ObservableProperty] private bool _fajrEnabled;
    [ObservableProperty] private bool _dhuhrEnabled;
    [ObservableProperty] private bool _asrEnabled;
    [ObservableProperty] private bool _maghribEnabled;
    [ObservableProperty] private bool _ishaEnabled;

    // Behavior
    [ObservableProperty] private bool _runAtStartup;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _keepFloatingWidgetAlwaysOnTop;
    [ObservableProperty] private int _themeModeIndex;
    [ObservableProperty] private int _widgetSideIndex;
    [ObservableProperty] private int _languageIndex;

    public SettingsViewModel(ISettingsService settings, IPrayerTimeService prayerService)
    {
        _settings = settings;
        _prayerService = prayerService;

        // Populate from current settings
        AutoDetectLocation = settings.AutoDetectLocation;
        CalculationMethodIndex = GetMethodIndex(settings.CalculationMethod);
        MadhabIndex = (int)settings.Madhab;
        Volume = settings.AdhanVolume * 100;

        FajrEnabled = settings.AdhanEnabled.GetValueOrDefault(PrayerName.Fajr, true);
        DhuhrEnabled = settings.AdhanEnabled.GetValueOrDefault(PrayerName.Dhuhr, true);
        AsrEnabled = settings.AdhanEnabled.GetValueOrDefault(PrayerName.Asr, true);
        MaghribEnabled = settings.AdhanEnabled.GetValueOrDefault(PrayerName.Maghrib, true);
        IshaEnabled = settings.AdhanEnabled.GetValueOrDefault(PrayerName.Isha, true);

        RunAtStartup = settings.RunAtStartup;
        MinimizeToTray = settings.MinimizeToTray;
        KeepFloatingWidgetAlwaysOnTop = settings.KeepFloatingWidgetAlwaysOnTop;
        ThemeModeIndex = (int)settings.ThemeMode;
        WidgetSideIndex = (int)settings.WidgetSide;
        LanguageIndex = (int)settings.CurrentLanguage;
    }

    public void Save()
    {
        _settings.AutoDetectLocation = AutoDetectLocation;
        _settings.CalculationMethod = GetMethodFromIndex(CalculationMethodIndex);
        _settings.Madhab = (Zakarni.Core.Models.Madhab)MadhabIndex;
        _settings.AdhanVolume = Volume / 100.0;

        _settings.AdhanEnabled = new Dictionary<Zakarni.Core.Models.PrayerName, bool>
        {
            { Zakarni.Core.Models.PrayerName.Fajr, FajrEnabled },
            { Zakarni.Core.Models.PrayerName.Dhuhr, DhuhrEnabled },
            { Zakarni.Core.Models.PrayerName.Asr, AsrEnabled },
            { Zakarni.Core.Models.PrayerName.Maghrib, MaghribEnabled },
            { Zakarni.Core.Models.PrayerName.Isha, IshaEnabled }
        };

        _settings.RunAtStartup = RunAtStartup;
        _settings.MinimizeToTray = MinimizeToTray;
        _settings.KeepFloatingWidgetAlwaysOnTop = KeepFloatingWidgetAlwaysOnTop;
        _settings.ThemeMode = (Zakarni.Core.Models.ThemeMode)ThemeModeIndex;
        _settings.WidgetSide = (Zakarni.Core.Models.WidgetSide)WidgetSideIndex;
        _settings.CurrentLanguage = (Zakarni.Core.Models.Language)LanguageIndex;

        _settings.Save();
        
        // Immediate system/UI updates
        _prayerService.InvalidateCache();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        AutoDetectLocation = true;
        CalculationMethodIndex = 0;
        MadhabIndex = 0;
        Volume = 80;
        FajrEnabled = DhuhrEnabled = AsrEnabled = MaghribEnabled = IshaEnabled = true;
        RunAtStartup = false;
        MinimizeToTray = true;
        KeepFloatingWidgetAlwaysOnTop = true;
        ThemeModeIndex = 0;
        WidgetSideIndex = 0;
    }

    // Map ComboBox index → CalculationMethod enum (ordered as in XAML)
    private static readonly Zakarni.Core.Models.CalculationMethod[] _methods =
    {
        Zakarni.Core.Models.CalculationMethod.MuslimWorldLeague,
        Zakarni.Core.Models.CalculationMethod.ISNA,
        Zakarni.Core.Models.CalculationMethod.Egyptian,
        Zakarni.Core.Models.CalculationMethod.UmmAlQura,
        Zakarni.Core.Models.CalculationMethod.Karachi,
        Zakarni.Core.Models.CalculationMethod.Tehran,
        Zakarni.Core.Models.CalculationMethod.Dubai,
        Zakarni.Core.Models.CalculationMethod.Kuwait,
        Zakarni.Core.Models.CalculationMethod.Qatar,
        Zakarni.Core.Models.CalculationMethod.Singapore
    };

    private static int GetMethodIndex(Zakarni.Core.Models.CalculationMethod m)
    {
        for (int i = 0; i < _methods.Length; i++)
            if (_methods[i] == m) return i;
        return 0;
    }

    private static Zakarni.Core.Models.CalculationMethod GetMethodFromIndex(int i) =>
        i >= 0 && i < _methods.Length ? _methods[i] : Zakarni.Core.Models.CalculationMethod.MuslimWorldLeague;
}
