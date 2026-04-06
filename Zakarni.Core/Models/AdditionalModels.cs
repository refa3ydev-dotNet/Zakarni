using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Zakarni.Core.Models;

public enum TodoCategory { Spiritual, Personal, Habit }
public enum TodoPriority { Low, Medium, High }

public class TodoItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    private string _task = string.Empty;
    public string Task { get => _task; set => SetProperty(ref _task, value); }

    private bool _isCompleted;
    public bool IsCompleted { get => _isCompleted; set => SetProperty(ref _isCompleted, value); }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    private TodoCategory _category = TodoCategory.Personal;
    public TodoCategory Category { get => _category; set => SetProperty(ref _category, value); }

    private TodoPriority _priority = TodoPriority.Medium;
    public TodoPriority Priority { get => _priority; set => SetProperty(ref _priority, value); }
}

public class AthkarItem
{
    public string Category { get; set; } = string.Empty; // Morning, Evening, AfterPrayer
    public string SubCategory { get; set; } = string.Empty; // All, Fajr, Dhuhr, Asr, Maghrib, Isha
    public string Text { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Order { get; set; }
}

public class QuranSurah
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public List<QuranAyah> Ayahs { get; set; } = new();
}

public class QuranAyah : ObservableObject
{
    public int NumberInSurah { get; set; }
    public int NumberInQuran { get; set; }
    public string Text { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int SurahNumber { get; set; }
    public string SurahNameAr { get; set; } = string.Empty;

    private string _translation = string.Empty;
    public string Translation { get => _translation; set => SetProperty(ref _translation, value); }

    private string _audioUrl = string.Empty;
    public string AudioUrl { get => _audioUrl; set => SetProperty(ref _audioUrl, value); }

    private bool _isPlaying;
    public bool IsPlaying { get => _isPlaying; set => SetProperty(ref _isPlaying, value); }
}

public class QuranPage
{
    public int PageNumber { get; set; }
    public List<QuranAyah> Ayahs { get; set; } = new();
    public string PrimarySurahName => Ayahs.FirstOrDefault()?.SurahNameAr ?? string.Empty;
}
