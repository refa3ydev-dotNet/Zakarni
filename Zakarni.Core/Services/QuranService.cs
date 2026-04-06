using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Zakarni.Core.Models;

namespace Zakarni.Core.Services;

public class QuranService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheFilePath;
    private List<QuranSurah> _surahs = new();
    private List<QuranPage> _pages = new();
    private bool _isLoaded = false;
    
    public string ErrorMessage { get; private set; } = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get; private set; }

    public QuranService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "Zakarni");
        Directory.CreateDirectory(appFolder);
        _cacheFilePath = Path.Combine(appFolder, "quran_cache.json");
    }

    public async Task EnsureInitializedAsync()
    {
        if (_isLoaded || IsLoading) return;
        
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            AlQuranApiResponse<AlQuranData>? response = null;

            // 1. Try local cache first
            if (File.Exists(_cacheFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(_cacheFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        response = JsonSerializer.Deserialize<AlQuranApiResponse<AlQuranData>>(json);
                    }
                }
                catch
                {
                    // Cache corrupt or inaccessible — ignore and continue to API
                }
            }

            // 2. Fetch from API if cache is missing or corrupt
            if (response == null || response.Data == null)
            {
                response = await _httpClient.GetFromJsonAsync<AlQuranApiResponse<AlQuranData>>("https://api.alquran.cloud/v1/quran/quran-uthmani");
                
                if (response != null && response.Code == 200 && response.Data != null)
                {
                    // Save to cache without blocking, safely
                    var jsonToCache = JsonSerializer.Serialize(response);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await File.WriteAllTextAsync(_cacheFilePath, jsonToCache);
                        }
                        catch { /* Silent fail for cache write */ }
                    });
                }
            }
            
            if (response == null || response.Code != 200 || response.Data == null)
            {
                ErrorMessage = "Failed to fetch Quran data from API.";
                return;
            }

            var apiSurahs = response.Data.Surahs;
            var allAyahs = new List<QuranAyah>();
            var surahList = new List<QuranSurah>();

            foreach (var s in apiSurahs)
            {
                var surah = new QuranSurah
                {
                    Number = s.Number,
                    Name = s.Name,
                    EnglishName = s.EnglishName,
                    Ayahs = s.Ayahs.Select(a => {
                        string text = a.Text;
                        // Strip Basmalah from first Ayah of Surahs 2-114 (except 9)
                        if (s.Number != 1 && s.Number != 9 && a.NumberInSurah == 1)
                        {
                            const string basmalah = "بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ";
                            if (text.StartsWith(basmalah))
                            {
                                text = text.Substring(basmalah.Length).Trim();
                            }
                        }
                        
                        return new QuranAyah
                        {
                            NumberInSurah = a.NumberInSurah,
                            NumberInQuran = a.Number,
                            Text = text,
                            PageNumber = a.Page,
                            SurahNumber = s.Number,
                            SurahNameAr = s.Name
                        };
                    }).ToList()
                };
                surahList.Add(surah);
                allAyahs.AddRange(surah.Ayahs);
            }

            _surahs = surahList;

            // Group ayahs by page
            var pageGroups = allAyahs.GroupBy(a => a.PageNumber).OrderBy(g => g.Key);
            _pages = pageGroups.Select(g => new QuranPage
            {
                PageNumber = g.Key,
                Ayahs = g.ToList()
            }).ToList();

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading Quran: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public List<QuranSurah> GetSurahs() => _surahs;
    public List<QuranPage> GetPages() => _pages;
}
