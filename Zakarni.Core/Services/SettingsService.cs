using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Models;

namespace Zakarni.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    // Location
    public LocationInfo? SavedLocation { get; set; }
    public bool AutoDetectLocation { get; set; } = true;

    // Prayer calculation
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.MuslimWorldLeague;
    public Madhab Madhab { get; set; } = Madhab.Shafi;

    // Audio
    public double AdhanVolume { get; set; } = 0.8;
    public string? AdhanAudioPath { get; set; }
    public Dictionary<PrayerName, bool> AdhanEnabled { get; set; } = new()
    {
        { PrayerName.Fajr, true },
        { PrayerName.Dhuhr, true },
        { PrayerName.Asr, true },
        { PrayerName.Maghrib, true },
        { PrayerName.Isha, true }
    };

    // App behavior
    public bool RunAtStartup { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool KeepFloatingWidgetAlwaysOnTop { get; set; } = true;
    public ThemeMode ThemeMode { get; set; } = ThemeMode.Dark;
    public WidgetSide WidgetSide { get; set; } = WidgetSide.Right;
    public Language CurrentLanguage { get; set; } = Language.Arabic;

    // Reminders
    public bool PrayForProphetEnabled { get; set; } = true;
    public int PrayForProphetIntervalMinutes { get; set; } = 30;
    public bool MorningAthkarEnabled { get; set; } = true;
    public bool EveningAthkarEnabled { get; set; } = true;
    public bool AfterPrayerAthkarEnabled { get; set; } = true;

    public List<TodoItem> TodoList { get; set; } = new();

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "Zakarni");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
            File.WriteAllText(_settingsFilePath, json);
            UpdateStartupRegistry(RunAtStartup);
        }
        catch { /* Fail silently on save */ }
    }

    public void Load()
    {
        if (!File.Exists(_settingsFilePath)) return;

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize<SettingsService>(json, new JsonSerializerOptions
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            if (loaded == null) return;

            SavedLocation = loaded.SavedLocation;
            AutoDetectLocation = loaded.AutoDetectLocation;
            CalculationMethod = loaded.CalculationMethod;
            Madhab = loaded.Madhab;
            AdhanVolume = loaded.AdhanVolume;
            AdhanAudioPath = loaded.AdhanAudioPath;
            AdhanEnabled = loaded.AdhanEnabled;
            RunAtStartup = loaded.RunAtStartup;
            MinimizeToTray = loaded.MinimizeToTray;
            KeepFloatingWidgetAlwaysOnTop = loaded.KeepFloatingWidgetAlwaysOnTop;
            ThemeMode = loaded.ThemeMode;
            WidgetSide = loaded.WidgetSide;
            CurrentLanguage = loaded.CurrentLanguage;
            PrayForProphetEnabled = loaded.PrayForProphetEnabled;
            PrayForProphetIntervalMinutes = loaded.PrayForProphetIntervalMinutes;
            MorningAthkarEnabled = loaded.MorningAthkarEnabled;
            EveningAthkarEnabled = loaded.EveningAthkarEnabled;
            AfterPrayerAthkarEnabled = loaded.AfterPrayerAthkarEnabled;
            TodoList = loaded.TodoList ?? new();
        }
        catch
        {
            // Corrupt file — keep defaults
        }
    }

    private void UpdateStartupRegistry(bool runAtStartup)
    {
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            if (key != null && exePath != null)
            {
                if (runAtStartup)
                    key.SetValue("Zakarni", exePath);
                else
                    key.DeleteValue("Zakarni", false);
            }
        }
        catch { /* Registry access may fail — non-critical */ }
    }
}
