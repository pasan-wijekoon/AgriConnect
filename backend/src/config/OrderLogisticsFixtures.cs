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

    public static readonly RegionReference RegionCentral = new(
        Guid.Parse("4f2b0001-0000-0000-0000-000000000001"), "Central Province");

    public static readonly RegionReference RegionSouthern = new(
        Guid.Parse("4f2b0002-0000-0000-0000-000000000002"), "Southern Province");

    public static readonly IReadOnlyList<RegionReference> Regions = [RegionCentral, RegionSouthern];

    // =========================================================================
    // 2. Reference Listings (Shared Reference Data / Component A)
    // =========================================================================

    public record ListingReference(Guid Id, string CropName, Guid FarmerId, Guid RegionId, decimal AvailableQuantity);

    public static readonly ListingReference ListingCarrots = new(
        Guid.Parse("4f2b1001-0000-0000-0000-000000000001"), "Carrots",
        Guid.Parse("4f2b2001-0000-0000-0000-000000000001"), RegionCentral.Id, 500m);

    public static readonly ListingReference ListingTomatoes = new(
        Guid.Parse("4f2b1002-0000-0000-0000-000000000002"), "Tomatoes",
        Guid.Parse("4f2b2002-0000-0000-0000-000000000002"), RegionSouthern.Id, 300m);

    public static readonly IReadOnlyList<ListingReference> Listings = [ListingCarrots, ListingTomatoes];

    // =========================================================================
    // 3. Reference Buyers (Shared Reference Data / Auth)
    // =========================================================================

    public static readonly Guid BuyerOne = Guid.Parse("4f2b3001-0000-0000-0000-000000000001");
    public static readonly Guid BuyerTwo = Guid.Parse("4f2b3002-0000-0000-0000-000000000002");

    // =========================================================================
    // 4. Collection Centres (own data)
    // =========================================================================

    public static List<CollectionCentre> GetCollectionCentres() =>
    [
        new CollectionCentre
        {
            Id = Guid.Parse("4f2b4001-0000-0000-0000-000000000001"),
            Name = "Kandy Central Collection Centre",
            Latitude = 7.290572m,
            Longitude = 80.633728m,
            Capacity = 5,
            RegionId = RegionCentral.Id
        },
        new CollectionCentre
        {
            Id = Guid.Parse("4f2b4002-0000-0000-0000-000000000002"),
            Name = "Matale Collection Centre",
            Latitude = 7.469720m,
            Longitude = 80.623200m,
            Capacity = 3,
            RegionId = RegionCentral.Id
        },
        new CollectionCentre
        {
            Id = Guid.Parse("4f2b4003-0000-0000-0000-000000000003"),
            Name = "Galle Southern Collection Centre",
            Latitude = 6.053519m,
            Longitude = 80.220978m,
            Capacity = 4,
            RegionId = RegionSouthern.Id
        },
        new CollectionCentre
        {
            Id = Guid.Parse("4f2b4004-0000-0000-0000-000000000004"),
            Name = "Matara Collection Centre",
            Latitude = 5.948730m,
            Longitude = 80.548170m,
            Capacity = 3,
            RegionId = RegionSouthern.Id
        }
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
                ExpiresAt = now.AddMinutes(28)
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
