using AgriConnect.Api.Config;

namespace AgriConnect.Api.Services;

/// <summary>
/// Fixture-backed stand-in for the real Listing table (Component A, not yet
/// landed). Treats every listing in <see cref="OrderLogisticsFixtures.Listings"/>
/// as Published with its full seeded quantity available — enough to build and
/// test OrderService against until Component A's table exists (plan §3).
/// </summary>
public class FixtureListingAvailabilityPort : IListingAvailabilityPort
{
    private static readonly IReadOnlyDictionary<Guid, OrderLogisticsFixtures.ListingReference> ListingsById =
        OrderLogisticsFixtures.Listings.ToDictionary(l => l.Id);

    public Task<ListingAvailability?> GetAvailabilityAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        if (!ListingsById.TryGetValue(listingId, out var listing))
        {
            return Task.FromResult<ListingAvailability?>(null);
        }

        var availability = new ListingAvailability(
            listing.Id,
            listing.FarmerId,
            listing.RegionId,
            Status: "Published",
            AvailableQuantity: listing.AvailableQuantity);

        return Task.FromResult<ListingAvailability?>(availability);
    }
}
