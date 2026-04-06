using Zakarni.Core.Models;

namespace Zakarni.Core.Interfaces;

public interface ISettingsService
{
    // Location
    LocationInfo? SavedLocation { get; set; }
    bool AutoDetectLocation { get; set; }

    // Prayer calculation
    CalculationMethod CalculationMethod { get; set; }
    Madhab Madhab { get; set; }

    // Audio
    double AdhanVolume { get; set; }
    string? AdhanAudioPath { get; set; }
    Dictionary<PrayerName, bool> AdhanEnabled { get; set; }

    // App behavior
    bool RunAtStartup { get; set; }
    bool MinimizeToTray { get; set; }
    bool KeepFloatingWidgetAlwaysOnTop { get; set; }
    ThemeMode ThemeMode { get; set; }
    WidgetSide WidgetSide { get; set; }
    Language CurrentLanguage { get; set; }

    // Reminders
    bool PrayForProphetEnabled { get; set; }
    int PrayForProphetIntervalMinutes { get; set; }
    bool MorningAthkarEnabled { get; set; }
    bool EveningAthkarEnabled { get; set; }
    bool AfterPrayerAthkarEnabled { get; set; }

    List<TodoItem> TodoList { get; set; }

    void Save();
    void Load();
}
