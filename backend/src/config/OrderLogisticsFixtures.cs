using AgriConnect.Api.Models;

namespace AgriConnect.Api.Config;

/// <summary>
/// Reference data models and deterministic fixtures for Component B (Order &amp;
/// Collection-Centre Logistics).
///
/// Since Listing, Region and User tables are owned by other components and do not
/// yet exist in the shared database, these fixtures provide deterministic GUIDs and
/// realistic sample data for local development, demoing, and automated tests —
/// swapped for the real tables once those components land (plan §3/§4.3).
/// </summary>
public static class OrderLogisticsFixtures
{
    // =========================================================================
    // 1. Reference Regions (Shared Reference Data / Component A)
    // =========================================================================

    public record RegionReference(Guid Id, string Name);

    // These now point at Component A's real seeded Region rows (Kandy/Galle —
    // matching the collection centre names below) rather than a fictional id,
    // now that Listing.RegionId is a real, enforced FK (2026-09-27 integration
    // audit fix — see PROGRESS.md). CollectionCentre.RegionId itself still has
    // no FK constraint (plan §4.3), but keeping it consistent with a real
    // region rather than a fictional one is strictly more correct.
    public static readonly RegionReference RegionCentral = new(
        Guid.Parse("b1000000-0000-0000-0000-000000000002"), "Kandy");

    public static readonly RegionReference RegionSouthern = new(
        Guid.Parse("b1000000-0000-0000-0000-000000000003"), "Galle");

    public static readonly IReadOnlyList<RegionReference> Regions = [RegionCentral, RegionSouthern];

    // =========================================================================
    // 2. Reference Listings (Shared Reference Data / Component A)
    // =========================================================================

    public record ListingReference(Guid Id, string CropName, Guid FarmerId, Guid RegionId, decimal AvailableQuantity);

    // Real Crop ids (Component A's seeded reference data) — Listing.CropId is a
    // real, enforced FK.
    public static readonly Guid CropCarrotsId = Guid.Parse("a1000000-0000-0000-0000-000000000005");
    public static readonly Guid CropTomatoesId = Guid.Parse("a1000000-0000-0000-0000-000000000004");

    // Real seeded Farmer ids (farmer@agriconnect.lk / farmer2@agriconnect.lk) —
    // Listing.FarmerId is now a real, enforced FK (2026-09-27 integration audit
    // fix). The old 4f2b2001.../4f2b2002... ids were never real User rows.
    public static readonly Guid FarmerKamalId = Guid.Parse("f0000000-0000-0000-0000-000000000001");
    public static readonly Guid FarmerSamanId = Guid.Parse("f0000000-0000-0000-0000-000000000002");

    public static readonly ListingReference ListingCarrots = new(
        Guid.Parse("4f2b1001-0000-0000-0000-000000000001"), "Carrots",
        FarmerKamalId, RegionCentral.Id, 500m);

    public static readonly ListingReference ListingTomatoes = new(
        Guid.Parse("4f2b1002-0000-0000-0000-000000000002"), "Tomatoes",
        FarmerSamanId, RegionSouthern.Id, 300m);

    public static readonly IReadOnlyList<ListingReference> Listings = [ListingCarrots, ListingTomatoes];

    /// <summary>
    /// Builds the real <see cref="Listing"/> rows backing the two references
    /// above, so <c>DbListingAvailabilityPort</c> — the real, DB-backed
    /// implementation now wired in Program.cs — actually finds them.
    /// Component A owns the Listing table/creation flow; this is Component B's
    /// own demo data using it, the same way it already seeds demo Orders.
    /// </summary>
    public static List<Listing> GetDemoListings()
    {
        var now = DateTime.UtcNow;
        return
        [
            new Listing
            {
                Id = ListingCarrots.Id,
                FarmerId = ListingCarrots.FarmerId,
                CropId = CropCarrotsId,
                RegionId = ListingCarrots.RegionId,
                Quantity = ListingCarrots.AvailableQuantity,
                Unit = "kg",
                ClaimedGrade = "Grade A",
                PickupWindowStart = now.AddDays(1),
                PickupWindowEnd = now.AddDays(3),
                Status = ListingStatus.Published,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7),
                Photos =
                [
                    new ListingPhoto
                    {
                        Id = Guid.Parse("4f2b1001-0000-0000-0000-0000000000f1"),
                        Url = "https://images.unsplash.com/photo-1598170845058-32b9d6a5c317?w=500&auto=format&fit=crop",
                        UploadedAt = now.AddDays(-7)
                    }
                ]
            },
            new Listing
            {
                Id = ListingTomatoes.Id,
                FarmerId = ListingTomatoes.FarmerId,
                CropId = CropTomatoesId,
                RegionId = ListingTomatoes.RegionId,
                Quantity = ListingTomatoes.AvailableQuantity,
                Unit = "kg",
                ClaimedGrade = "Grade A",
                PickupWindowStart = now.AddDays(1),
                PickupWindowEnd = now.AddDays(3),
                Status = ListingStatus.Published,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7),
                Photos =
                [
                    new ListingPhoto
                    {
                        Id = Guid.Parse("4f2b1002-0000-0000-0000-0000000000f2"),
                        Url = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=500&auto=format&fit=crop",
                        UploadedAt = now.AddDays(-7)
                    }
                ]
            }
        ];
    }

    // =========================================================================
    // 3. Reference Buyers (Shared Reference Data / Auth)
    // =========================================================================

    public static readonly Guid BuyerOne = Guid.Parse("4f2b3001-0000-0000-0000-000000000001");
    public static readonly Guid BuyerTwo = Guid.Parse("4f2b3002-0000-0000-0000-000000000002");

    // =========================================================================
    // 4. Collection Centres (own data)
    // =========================================================================

    // Region ids are Component A's real seeded Region rows (b1000000-...-0000000000NN,
    // see AgriConnectDbContext.SeedComponentAReferenceData). Every region a listing
    // can be created in has its own centre, so scheduling never dead-ends on
    // "no collection centre in the listing's region". The first four ids/order are
    // unchanged from the original fixtures (demo schedules index into them).
    private static Guid RegionId(int n) => Guid.Parse($"b1000000-0000-0000-0000-{n:x12}");

    private static CollectionCentre Centre(int n, string name, decimal lat, decimal lng, int capacity, int regionNo) => new()
    {
        Id = Guid.Parse($"4f2b4{n:000}-0000-0000-0000-{n:x12}"),
        Name = name,
        Latitude = lat,
        Longitude = lng,
        Capacity = capacity,
        RegionId = RegionId(regionNo)
    };

    public static List<CollectionCentre> GetCollectionCentres() =>
    [
        Centre(1, "Kandy Central Collection Centre", 7.290572m, 80.633728m, 5, 2),
        Centre(2, "Matale Collection Centre", 7.469720m, 80.623200m, 3, 0x0b),
        Centre(3, "Galle Southern Collection Centre", 6.053519m, 80.220978m, 4, 3),
        Centre(4, "Matara Collection Centre", 5.948730m, 80.548170m, 3, 6),
        Centre(5, "Colombo Metro Collection Centre", 6.927079m, 79.861244m, 6, 1),
        Centre(6, "Jaffna Northern Collection Centre", 9.661498m, 80.025547m, 3, 4),
        Centre(7, "Anuradhapura Collection Centre", 8.311400m, 80.403700m, 4, 5),
        Centre(8, "Kurunegala Collection Centre", 7.486300m, 80.362300m, 4, 7),
        Centre(9, "Nuwara Eliya Highland Collection Centre", 6.949700m, 80.789100m, 4, 8),
        Centre(10, "Gampaha Collection Centre", 7.087300m, 79.999200m, 4, 9),
        Centre(11, "Kalutara Collection Centre", 6.583100m, 79.960700m, 3, 0x0a),
        Centre(12, "Ratnapura Collection Centre", 6.682800m, 80.399200m, 3, 0x0c),
        Centre(13, "Kegalle Collection Centre", 7.251300m, 80.346600m, 3, 0x0d),
        Centre(14, "Badulla Collection Centre", 6.989700m, 81.055000m, 3, 0x0e),
        Centre(15, "Monaragala Collection Centre", 6.871500m, 81.350600m, 3, 0x0f)
    ];

    // =========================================================================
    // 5. Demo Orders — one per OrderStatus, with the reservation/schedule rows
    //    that would realistically accompany each status, so the tracking UI and
    //    officer queue have something to render before Component A's real
    //    listings exist.
    // =========================================================================

    public record DemoOrderSet(
        List<Order> Orders,
        List<StockReservation> Reservations,
        List<PickupSchedule> Schedules);

    public static DemoOrderSet GetDemoOrders()
    {
        var now = DateTimeOffset.UtcNow;
        var centres = GetCollectionCentres();

        var pendingOrder = new Order
        {
            Id = Guid.Parse("4f2b5001-0000-0000-0000-000000000001"),
            ListingId = ListingCarrots.Id,
            BuyerId = BuyerOne,
            Quantity = 50m,
            Status = OrderStatus.Pending,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-2)
        };

        var approvedOrder = new Order
        {
            Id = Guid.Parse("4f2b5002-0000-0000-0000-000000000002"),
            ListingId = ListingCarrots.Id,
            BuyerId = BuyerTwo,
            Quantity = 80m,
            Status = OrderStatus.Approved,
            DeliveryPreference = DeliveryPreference.Delivery,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddHours(-5)
        };

        var scheduledOrder = new Order
        {
            Id = Guid.Parse("4f2b5003-0000-0000-0000-000000000003"),
            ListingId = ListingTomatoes.Id,
            BuyerId = BuyerOne,
            Quantity = 40m,
            Status = OrderStatus.Scheduled,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-1)
        };

        var completedOrder = new Order
        {
            Id = Guid.Parse("4f2b5004-0000-0000-0000-000000000004"),
            ListingId = ListingTomatoes.Id,
            BuyerId = BuyerTwo,
            Quantity = 60m,
            Status = OrderStatus.Completed,
            DeliveryPreference = DeliveryPreference.Pickup,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-5)
        };

        var cancelledOrder = new Order
        {
            Id = Guid.Parse("4f2b5005-0000-0000-0000-000000000005"),
            ListingId = ListingCarrots.Id,
            BuyerId = BuyerOne,
            Quantity = 20m,
            Status = OrderStatus.Cancelled,
            DeliveryPreference = DeliveryPreference.Delivery,
            CreatedAt = now.AddDays(-4),
            UpdatedAt = now.AddDays(-4).AddHours(1)
        };

        var orders = new List<Order>
        {
            pendingOrder, approvedOrder, scheduledOrder, completedOrder, cancelledOrder
        };

        // Active reservations exist for every order that has not been cancelled and
        // still holds stock (Pending/Approved/Scheduled/Completed all reserved stock
        // at some point; a Cancelled order's reservation is what expiry/cancellation
        // released, so it is intentionally omitted here).
        var reservations = new List<StockReservation>
        {
            new StockReservation
            {
                Id = Guid.Parse("4f2b6001-0000-0000-0000-000000000001"),
                ListingId = pendingOrder.ListingId,
                OrderId = pendingOrder.Id,
                ReservedQuantity = pendingOrder.Quantity,
                ExpiresAt = now.AddDays(2)
            },
            new StockReservation
            {
                Id = Guid.Parse("4f2b6002-0000-0000-0000-000000000002"),
                ListingId = approvedOrder.ListingId,
                OrderId = approvedOrder.Id,
                ReservedQuantity = approvedOrder.Quantity,
                ExpiresAt = now.AddDays(2)
            },
            new StockReservation
            {
                Id = Guid.Parse("4f2b6003-0000-0000-0000-000000000003"),
                ListingId = scheduledOrder.ListingId,
                OrderId = scheduledOrder.Id,
                ReservedQuantity = scheduledOrder.Quantity,
                ExpiresAt = now.AddDays(3)
            },
            new StockReservation
            {
                Id = Guid.Parse("4f2b6004-0000-0000-0000-000000000004"),
                ListingId = completedOrder.ListingId,
                OrderId = completedOrder.Id,
                ReservedQuantity = completedOrder.Quantity,
                ExpiresAt = now.AddDays(-5)
            }
        };

        var schedules = new List<PickupSchedule>
        {
            new PickupSchedule
            {
                Id = Guid.Parse("4f2b7001-0000-0000-0000-000000000001"),
                OrderId = scheduledOrder.Id,
                CollectionCentreId = centres[2].Id, // Galle Southern — matches the tomato listing's region
                SlotStart = new DateTimeOffset(now.AddDays(1).Date, TimeSpan.Zero).AddHours(9),
                SlotEnd = new DateTimeOffset(now.AddDays(1).Date, TimeSpan.Zero).AddHours(10),
                Status = ScheduleStatus.Confirmed
            },
            new PickupSchedule
            {
                Id = Guid.Parse("4f2b7002-0000-0000-0000-000000000002"),
                OrderId = completedOrder.Id,
                CollectionCentreId = centres[2].Id,
                SlotStart = new DateTimeOffset(now.AddDays(-5).Date, TimeSpan.Zero).AddHours(9),
                SlotEnd = new DateTimeOffset(now.AddDays(-5).Date, TimeSpan.Zero).AddHours(10),
                Status = ScheduleStatus.Confirmed
            }
        };

        return new DemoOrderSet(orders, reservations, schedules);
    }
}
