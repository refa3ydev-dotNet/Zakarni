using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zakarni.Core.Models;
using Zakarni.Core.Services;

namespace Zakarni.UI.ViewModels;

public partial class QuranViewModel : ObservableObject
{
    private readonly QuranService _quranService;
    private readonly HttpClient _httpClient = new();

    [ObservableProperty]
    private QuranSurah? _selectedSurah;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Advanced Reading State
    [ObservableProperty]
    private bool _isFetchingDetails;

    [ObservableProperty]
    private bool _isLoadingSurah;

    [ObservableProperty]
    private string _readingErrorMessage = string.Empty;

    [ObservableProperty]
    private QuranAyah? _activeAyah;

    [ObservableProperty]
    private bool _isSidePanelOpen;

    public string SelectedSurahName => SelectedSurah?.Name ?? string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoError => !HasError && !IsLoading;

    public ObservableCollection<QuranSurah> Surahs { get; } = new();

    public QuranViewModel(QuranService quranService)
    {
        _quranService = quranService;
        _ = LoadQuranAsync();
    }

    [RelayCommand]
    private async Task LoadQuranAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        Surahs.Clear();

        await _quranService.EnsureInitializedAsync();

        if (_quranService.HasError)
        {
            ErrorMessage = _quranService.ErrorMessage;
        }
        else
        {
            foreach (var surah in _quranService.GetSurahs())
            {
                Surahs.Add(surah);
            }
            SelectedSurah = Surahs.FirstOrDefault();
        }

        IsLoading = false;
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasNoError));
    }

    partial void OnSelectedSurahChanged(QuranSurah? value)
    {
        OnPropertyChanged(nameof(SelectedSurahName));
    }

    /// <summary>
    /// Fetches Tafsir/Translation and Audio URLs for the specified Surah from AlQuran.cloud API
    /// and populates the properties of the Ayahs within that Surah.
    /// </summary>
    public async Task FetchSurahDetailsAsync(QuranSurah surah)
    {
        if (surah == null || surah.Ayahs == null || !surah.Ayahs.Any()) return;

        IsFetchingDetails = true;
        ReadingErrorMessage = string.Empty;

        try
        {
            // Fetch English Translation (e.g., en.asad)
            var translationTask = _httpClient.GetFromJsonAsync<AlQuranApiResponse<AlQuranSurah>>(
                $"https://api.alquran.cloud/v1/surah/{surah.Number}/en.asad");

            // Fetch Audio Recitation (e.g., ar.alafasy)
            var audioTask = _httpClient.GetFromJsonAsync<AlQuranApiResponse<AlQuranSurah>>(
                $"https://api.alquran.cloud/v1/surah/{surah.Number}/ar.alafasy");

            await Task.WhenAll(translationTask, audioTask);

            var translationRes = translationTask.Result;
            var audioRes = audioTask.Result;

            if (translationRes?.Data?.Ayahs != null && audioRes?.Data?.Ayahs != null)
            {
                var translations = translationRes.Data.Ayahs.ToDictionary(a => a.NumberInSurah, a => a.Text);
                var audios = audioRes.Data.Ayahs.ToDictionary(a => a.NumberInSurah, a => a.Audio);

                foreach (var ayah in surah.Ayahs)
                {
                    if (translations.TryGetValue(ayah.NumberInSurah, out var trans))
                    {
                        ayah.Translation = trans;
                    }
                    
                    if (audios.TryGetValue(ayah.NumberInSurah, out var audioUrl))
                    {
                        ayah.AudioUrl = audioUrl ?? string.Empty;
                    }
                }
            }
            else
            {
                ReadingErrorMessage = "Failed to parse detailed Surah data from API.";
            }
        }
        catch (Exception ex)
        {
            ReadingErrorMessage = $"Network error fetching details: {ex.Message}";
        }
        finally
        {
            IsFetchingDetails = false;
        }
    }
}