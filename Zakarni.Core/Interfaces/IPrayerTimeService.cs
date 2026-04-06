using Zakarni.Core.Models;

namespace Zakarni.Core.Interfaces;

public interface IPrayerTimeService
{
    Task<PrayerTimeData> CalculatePrayerTimesAsync(DateTime date, LocationInfo location,
        CalculationMethod method = CalculationMethod.MuslimWorldLeague,
        Madhab madhab = Madhab.Shafi);

    void InvalidateCache();
}
