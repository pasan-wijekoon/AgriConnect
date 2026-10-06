using AgriConnect.Api.Services;

namespace backend.Tests.services;

public class HaversineCalculatorTests
{
    [Fact]
    public void DistanceKm_BetweenSamePoint_IsZero()
    {
        var distance = HaversineCalculator.DistanceKm(7.2906, 80.6337, 7.2906, 80.6337);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void DistanceKm_KandyToColombo_IsRoughlyCorrect()
    {
        // Kandy (7.2906, 80.6337) to Colombo (6.9271, 79.8612) — known straight-line
        // distance is ~94-95 km. A wide tolerance keeps this robust to minor
        // reference-value differences while still catching a badly broken formula.
        var distance = HaversineCalculator.DistanceKm(7.2906, 80.6337, 6.9271, 79.8612);

        Assert.InRange(distance, 85, 105);
    }

    [Fact]
    public void DistanceKm_IsSymmetric()
    {
        var forward = HaversineCalculator.DistanceKm(7.2906, 80.6337, 6.9271, 79.8612);
        var backward = HaversineCalculator.DistanceKm(6.9271, 79.8612, 7.2906, 80.6337);

        Assert.Equal(forward, backward, precision: 9);
    }
}
