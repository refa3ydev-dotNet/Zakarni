using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zakarni.Core.Models;
using Zakarni.Core.Services;
using System.Linq;

namespace Zakarni.UI.ViewModels;

public partial class AthkarViewModel : ObservableObject
{
    private readonly AthkarService _athkarService;

    public ObservableCollection<AthkarItem> MorningAthkar { get; } = new();
    public ObservableCollection<AthkarItem> EveningAthkar { get; } = new();
    public ObservableCollection<AthkarItem> AfterPrayerAthkar { get; } = new();

    private string _selectedAfterPrayerFilter = "All";
    public string SelectedAfterPrayerFilter
    {
        get => _selectedAfterPrayerFilter;
        set
        {
            if (SetProperty(ref _selectedAfterPrayerFilter, value))
            {
                FilterAfterPrayer();
            }
        }
    }

    public ObservableCollection<string> AfterPrayerFilters { get; } = new() { "All", "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };

    public AthkarViewModel(AthkarService athkarService)
    {
        _athkarService = athkarService;
        RefreshAthkar();
    }

    private void RefreshAthkar()
    {
        MorningAthkar.Clear();
        foreach (var item in _athkarService.GetMorningAthkar().OrderBy(a => a.Order)) MorningAthkar.Add(item);

        EveningAthkar.Clear();
        foreach (var item in _athkarService.GetEveningAthkar().OrderBy(a => a.Order)) EveningAthkar.Add(item);

        FilterAfterPrayer();
    }

    private void FilterAfterPrayer()
    {
        AfterPrayerAthkar.Clear();
        var allItems = _athkarService.GetAfterPrayerAthkar().OrderBy(a => a.Order);
        foreach (var item in allItems)
        {
            if (item.SubCategory == "All" || item.SubCategory == SelectedAfterPrayerFilter || SelectedAfterPrayerFilter == "All")
            {
                AfterPrayerAthkar.Add(item);
            }
        }
    }
}
