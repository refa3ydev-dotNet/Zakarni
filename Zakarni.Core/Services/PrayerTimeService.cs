using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Models;

namespace Zakarni.Core.Services;

public class PrayerTimeService : IPrayerTimeService
{
    private readonly HttpClient _http = new();
    private PrayerTimeData? _cached;
    private DateTime _cachedDate;
    private int _cachedMethodId;

    public async Task<PrayerTimeData> CalculatePrayerTimesAsync(
        DateTime date, LocationInfo location,
        CalculationMethod method = CalculationMethod.MuslimWorldLeague,
        Madhab madhab = Madhab.Shafi)
    {
        int methodId = (int)method;
        int school = (int)madhab;

        // Return cache if same day + same method
        if (_cached != null && _cachedDate.Date == date.Date && _cachedMethodId == methodId)
        {
            _cached.UpdateNextPrayer(DateTime.Now);
            return _cached;
        }

        try
        {
            var url = $"https://api.aladhan.com/v1/timings/{date:dd-MM-yyyy}" +
                      $"?latitude={location.Latitude}&longitude={location.Longitude}" +
                      $"&method={methodId}&school={school}";

            var response = await _http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            var timings = json.RootElement.GetProperty("data").GetProperty("timings");

            var data = new PrayerTimeData
            {
                Date = date.Date,
                Fajr = ParseTime(date, timings.GetProperty("Fajr").GetString()),
                Sunrise = ParseTime(date, timings.GetProperty("Sunrise").GetString()),
                Dhuhr = ParseTime(date, timings.GetProperty("Dhuhr").GetString()),
                Asr = ParseTime(date, timings.GetProperty("Asr").GetString()),
                Maghrib = ParseTime(date, timings.GetProperty("Maghrib").GetString()),
                Isha = ParseTime(date, timings.GetProperty("Isha").GetString())
            };

            data.UpdateNextPrayer(DateTime.Now);

            _cached = data;
            _cachedDate = date;
            _cachedMethodId = methodId;

            return data;
        }
        catch
        {
            // Offline fallback with approximate times
            var fallback = new PrayerTimeData
            {
                Date = date.Date,
                Fajr = date.Date.AddHours(5),
                Sunrise = date.Date.AddHours(6).AddMinutes(15),
                Dhuhr = date.Date.AddHours(12).AddMinutes(15),
                Asr = date.Date.AddHours(15).AddMinutes(30),
                Maghrib = date.Date.AddHours(18),
                Isha = date.Date.AddHours(19).AddMinutes(30)
            };
            fallback.UpdateNextPrayer(DateTime.Now);
            return fallback;
        }
    }

    public void InvalidateCache() => _cached = null;

    private static DateTime ParseTime(DateTime date, string? timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr)) return date;

        // Aladhan returns "HH:mm (TZ)" — strip the timezone part
        var clean = timeStr.Contains('(') ? timeStr[..timeStr.IndexOf('(')].Trim() : timeStr.Trim();

        return TimeSpan.TryParse(clean, out var ts) ? date.Date.Add(ts) : date;
    }
}
