using AgriConnect.Api.Config;
using AgriConnect.Api.Services;

namespace backend.Tests.services;

public class FixtureListingAvailabilityPortTests
{
    private readonly FixtureListingAvailabilityPort _port = new();

    [Fact]
    public async Task GetAvailabilityAsync_WithKnownFixtureListing_ReturnsPublishedWithFullQuantity()
    {
        var listing = OrderLogisticsFixtures.ListingCarrots;

        var result = await _port.GetAvailabilityAsync(listing.Id);

        Assert.NotNull(result);
        Assert.Equal(listing.Id, result!.ListingId);
        Assert.Equal(listing.FarmerId, result.FarmerId);
        Assert.Equal(listing.RegionId, result.RegionId);
        Assert.Equal("Published", result.Status);
        Assert.Equal(listing.AvailableQuantity, result.AvailableQuantity);
    }

    [Fact]
    public async Task GetAvailabilityAsync_WithUnknownListingId_ReturnsNull()
    {
        var result = await _port.GetAvailabilityAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
