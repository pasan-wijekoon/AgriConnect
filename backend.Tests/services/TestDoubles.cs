using AgriConnect.Api.Models;
using AgriConnect.Api.Services;

namespace backend.Tests.services;

internal class FakeListingAvailabilityPort : IListingAvailabilityPort
{
    private readonly Dictionary<Guid, ListingAvailability> _listings = new();

    public FakeListingAvailabilityPort Add(ListingAvailability listing)
    {
        _listings[listing.ListingId] = listing;
        return this;
    }

    public Task<ListingAvailability?> GetAvailabilityAsync(Guid listingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_listings.GetValueOrDefault(listingId));
}

/// <summary>Configurable stand-in for the real HTTP-backed matching agent
/// client — returns whatever <see cref="Result"/> is set (including null, for
/// "the agent call itself didn't succeed") without making any real HTTP call.</summary>
internal class FakeBuyerFarmerMatchingPort(MatchResult? result) : IBuyerFarmerMatchingPort
{
    public Guid? LastOrderIdPassedIn { get; private set; }
    public IReadOnlyList<MatchCandidateCentre>? LastCandidatesPassedIn { get; private set; }

    public Task<MatchResult?> MatchAsync(
        Guid orderId, decimal buyerLat, decimal buyerLng, Guid listingId, decimal requestedQuantity,
        IReadOnlyList<MatchCandidateCentre> candidateCentres, CancellationToken cancellationToken = default)
    {
        LastOrderIdPassedIn = orderId;
        LastCandidatesPassedIn = candidateCentres;
        return Task.FromResult(result);
    }
}

internal class FakeStockReservationService : IStockReservationService
{
    private readonly bool _succeeds;
    private readonly string? _failureReason;

    public FakeStockReservationService(bool succeeds, string? failureReason = null)
    {
        _succeeds = succeeds;
        _failureReason = failureReason;
    }

    public Order? LastOrderPassedIn { get; private set; }

    public Task<ReservationResult> ReserveAndPlaceOrderAsync(
        Order order, decimal availableQuantity, CancellationToken cancellationToken = default)
    {
        LastOrderPassedIn = order;

        if (!_succeeds)
        {
            return Task.FromResult(new ReservationResult(false, null, _failureReason ?? "Insufficient stock."));
        }

        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            ListingId = order.ListingId,
            OrderId = order.Id,
            ReservedQuantity = order.Quantity,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        return Task.FromResult(new ReservationResult(true, reservation, null));
    }
}
