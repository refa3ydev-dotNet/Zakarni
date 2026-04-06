namespace Zakarni.Core.Models;

public class LocationInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public bool IsValid => Latitude != 0 || Longitude != 0;

    public override string ToString() =>
        string.IsNullOrWhiteSpace(City) ? $"{Latitude:F2}°, {Longitude:F2}°" : $"{City}, {Country}";
}
