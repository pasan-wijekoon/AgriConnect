namespace AgriConnect.Api.Services;

/// <summary>
/// What OrderService needs to know about a listing before an order can be placed
/// against it. Mirrors the subset of the (not-yet-existing) Listing table Component
/// B actually reads (plan §3).
/// </summary>
public record ListingAvailability(
    Guid ListingId,
    Guid FarmerId,
    Guid RegionId,
    string Status,
    decimal AvailableQuantity);

/// <summary>
/// Seam between OrderService and Component A's Listing table, which does not exist
/// yet anywhere in the repo. Swap <see cref="FixtureListingAvailabilityPort"/> for a
/// real EF-Core-backed implementation once Listing lands — this interface's shape
/// should not need to change, so the swap is a one-file DI registration change
/// (plan §3).
/// </summary>
public interface IListingAvailabilityPort
{
    /// <summary>Returns null if no listing with this id exists.</summary>
    Task<ListingAvailability?> GetAvailabilityAsync(Guid listingId, CancellationToken cancellationToken = default);
}
