using System;

namespace Zakarni.Core.Models;

public class PrayerTimeData
{
    public DateTime Date { get; set; }
    public DateTime Fajr { get; set; }
    public DateTime Sunrise { get; set; }
    public DateTime Dhuhr { get; set; }
    public DateTime Asr { get; set; }
    public DateTime Maghrib { get; set; }
    public DateTime Isha { get; set; }

    public string NextPrayerName { get; set; } = string.Empty;
    public DateTime NextPrayerTime { get; set; }
    public PrayerName NextPrayer { get; set; }

    /// <summary>Returns the time for a given prayer name.</summary>
    public DateTime TimeFor(PrayerName prayer) => prayer switch
    {
        PrayerName.Fajr => Fajr,
        PrayerName.Sunrise => Sunrise,
        PrayerName.Dhuhr => Dhuhr,
        PrayerName.Asr => Asr,
        PrayerName.Maghrib => Maghrib,
        PrayerName.Isha => Isha,
        _ => DateTime.MinValue
    };

    /// <summary>Recalculates the next prayer based on the current time.</summary>
    public void UpdateNextPrayer(DateTime now)
    {
        PrayerName[] prayers = { PrayerName.Fajr, PrayerName.Sunrise, PrayerName.Dhuhr,
                                  PrayerName.Asr, PrayerName.Maghrib, PrayerName.Isha };
        string[] names = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };

        for (int i = 0; i < prayers.Length; i++)
        {
            if (TimeFor(prayers[i]) > now)
            {
                NextPrayer = prayers[i];
                NextPrayerName = names[i];
                NextPrayerTime = TimeFor(prayers[i]);
                return;
            }
        }

        // Past Isha — next is tomorrow's Fajr
        NextPrayer = PrayerName.Fajr;
        NextPrayerName = "Fajr";
        NextPrayerTime = Fajr.AddDays(1);
    }
}
