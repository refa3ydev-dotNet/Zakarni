using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Models;

namespace Zakarni.Core.Services;

public class LocationService : ILocationService
{
    public async Task<LocationInfo> GetCurrentLocationAsync()
    {
        try 
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                var geolocator = new Geolocator { DesiredAccuracyInMeters = 1000 };
                var position = await geolocator.GetGeopositionAsync();

                return new LocationInfo
                {
                    Latitude = position.Coordinate.Point.Position.Latitude,
                    Longitude = position.Coordinate.Point.Position.Longitude,
                    City = "GPS Location",
                    Country = "Exact"
                };
            }
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"GPS Failed: {ex.Message}");
        }

        // Basic offline fallback
        return new LocationInfo
        {
            Latitude = 21.4225,
            Longitude = 39.8262,
            City = "Mecca (Fallback)",
            Country = "Saudi Arabia"
        };
    }
}
