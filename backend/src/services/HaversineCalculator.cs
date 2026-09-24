namespace AgriConnect.Api.Services;

/// <summary>
/// Straight-line (great-circle) distance between two coordinates. Used as the
/// graceful-degraded fallback (plan §9) when the Maps/Distance API is unreachable
/// — never an approximation of driving distance, just a last-resort estimate the
/// client should show as "approximate" (the <c>Degraded</c> flag on
/// <see cref="DistanceResult"/> signals this).
/// </summary>
public static class HaversineCalculator
{
    private const double EarthRadiusKm = 6371.0;

    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
