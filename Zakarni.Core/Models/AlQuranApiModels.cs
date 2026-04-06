using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zakarni.Core.Models;

public class AlQuranApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class AlQuranData
{
    [JsonPropertyName("surahs")]
    public List<AlQuranSurah> Surahs { get; set; } = new();
}

public class AlQuranSurah
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("englishName")]
    public string EnglishName { get; set; } = string.Empty;

    [JsonPropertyName("englishNameTranslation")]
    public string EnglishNameTranslation { get; set; } = string.Empty;

    [JsonPropertyName("revelationType")]
    public string RevelationType { get; set; } = string.Empty;

    [JsonPropertyName("ayahs")]
    public List<AlQuranAyah> Ayahs { get; set; } = new();
}

public class AlQuranAyah
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("numberInSurah")]
    public int NumberInSurah { get; set; }

    [JsonPropertyName("juz")]
    public int Juz { get; set; }

    [JsonPropertyName("manzil")]
    public int Manzil { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("ruku")]
    public int Ruku { get; set; }

    [JsonPropertyName("hizbQuarter")]
    public int HizbQuarter { get; set; }

    [JsonPropertyName("sajda")]
    public object? Sajda { get; set; } // Could be bool or object
    
    [JsonPropertyName("audio")]
    public string? Audio { get; set; }
    
    [JsonPropertyName("audioSecondary")]
    public List<string>? AudioSecondary { get; set; }
}
