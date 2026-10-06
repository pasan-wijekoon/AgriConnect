using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

/// <summary>
/// Unit tests for Component C's InspectionService (FR5, FR12–FR14), using the
/// EF Core InMemory provider — mirrors OrderServiceTests' conventions. Focused
/// on the FR5 publish gate (the property this integration pass makes the sole
/// path to Listing.Status = Published, replacing Component A's removed ungated
/// approve endpoint — see PROGRESS.md) and the FR14 discrepancy-detection path.
/// </summary>
public class InspectionServiceTests
{
    private static readonly Guid FarmerId = Guid.NewGuid();
    private static readonly Guid OfficerId = Guid.NewGuid();
    private static readonly Guid CropId = Guid.NewGuid();
    private static readonly Guid RegionId = Guid.NewGuid();

    private static AgriConnectDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static InspectionService NewService(AgriConnectDbContext db) =>
        new(db, new AuditLogService(db), new NotificationService(db));

    private static async Task SeedUsersAndReferenceDataAsync(AgriConnectDbContext db)
    {
        db.Users.Add(new User { Id = FarmerId, FullName = "Farmer", Email = $"{FarmerId}@test.lk", PasswordHash = "x", Role = "Farmer" });
        db.Users.Add(new User { Id = OfficerId, FullName = "Officer", Email = $"{OfficerId}@test.lk", PasswordHash = "x", Role = "Officer" });
        db.Crops.Add(new Crop { Id = CropId, Name = $"Crop-{CropId}", Category = "Vegetables" });
        db.Regions.Add(new Region { Id = RegionId, Name = $"Region-{RegionId}" });
        await db.SaveChangesAsync();
    }

    private static async Task<Listing> SeedListingAsync(AgriConnectDbContext db, string claimedGrade = "Grade A", string status = "PendingApproval")
    {
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            FarmerId = FarmerId,
            CropId = CropId,
            RegionId = RegionId,
            Quantity = 50m,
            Unit = "kg",
            ClaimedGrade = claimedGrade,
            Status = status,
            PickupWindowStart = DateTime.UtcNow.AddDays(1),
            PickupWindowEnd = DateTime.UtcNow.AddDays(2),
        };
        db.Listings.Add(listing);
        await db.SaveChangesAsync();
        return listing;
    }

    [Fact]
    public async Task RecordInspectionAsync_WithMismatchedGrade_FlagsDiscrepancy()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db, claimedGrade: QualityGrade.GradeA);
        var service = NewService(db);

        var result = await service.RecordInspectionAsync(
            new CreateInspectionRequest { ListingId = listing.Id, ConfirmedGrade = QualityGrade.GradeB }, OfficerId);

        Assert.True(result.HasDiscrepancy);
        Assert.Single(db.GradeDiscrepancyFlags);
        Assert.Contains(db.Notifications, n => n.UserId == FarmerId && n.Type == "Warning");
    }

    [Fact]
    public async Task RecordInspectionAsync_WithMatchingGrade_NoDiscrepancy()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db, claimedGrade: QualityGrade.GradeA);
        var service = NewService(db);

        var result = await service.RecordInspectionAsync(
            new CreateInspectionRequest { ListingId = listing.Id, ConfirmedGrade = QualityGrade.GradeA }, OfficerId);

        Assert.False(result.HasDiscrepancy);
        Assert.Empty(db.GradeDiscrepancyFlags);
    }

    [Fact]
    public async Task RecordInspectionAsync_WithInvalidGrade_ThrowsArgumentException()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db);
        var service = NewService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordInspectionAsync(new CreateInspectionRequest { ListingId = listing.Id, ConfirmedGrade = "A+" }, OfficerId));
    }

    [Fact]
    public async Task PublishListingWithGateCheckAsync_WithNoInspection_IsBlocked()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db);
        var service = NewService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PublishListingWithGateCheckAsync(listing.Id, OfficerId));

        Assert.Contains("not been inspected", ex.Message);
        Assert.Equal("PendingApproval", (await db.Listings.FindAsync(listing.Id))!.Status);
    }

    [Fact]
    public async Task PublishListingWithGateCheckAsync_WithRejectedGrade_IsBlocked()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db);
        var service = NewService(db);
        await service.RecordInspectionAsync(
            new CreateInspectionRequest { ListingId = listing.Id, ConfirmedGrade = QualityGrade.Rejected }, OfficerId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PublishListingWithGateCheckAsync(listing.Id, OfficerId));

        Assert.Contains("Rejected", ex.Message);
    }

    [Fact]
    public async Task PublishListingWithGateCheckAsync_WithUnresolvedDiscrepancy_IsBlocked()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db, claimedGrade: QualityGrade.GradeA);
        var service = NewService(db);
        await service.RecordInspectionAsync(
            new CreateInspectionRequest { ListingId = listing.Id, ConfirmedGrade = QualityGrade.GradeB }, OfficerId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PublishListingWithGateCheckAsync(listing.Id, OfficerId));

        Assert.Contains("unresolved grade discrepancy", ex.Message);
    }

    [Fact]
    public async Task PublishListingWithGateCheckAsync_AfterPassingInspection_Publishes()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db, claimedGrade: QualityGrade.GradeA);
        var service = NewService(db);
        await service.RecordInspectionAsync(
            new CreateInspectionRequest { ListingId = listing.Id, ConfirmedGrade = QualityGrade.GradeA }, OfficerId);

        var result = await service.PublishListingWithGateCheckAsync(listing.Id, OfficerId);

        Assert.Equal(ListingStatus.Published, result.Status);
        Assert.Equal(ListingStatus.Published, (await db.Listings.FindAsync(listing.Id))!.Status);
        Assert.Contains(db.AuditLogs, a => a.Action == "PublishListingApproved" && a.EntityId == listing.Id);
    }

    [Fact]
    public async Task PublishListingWithGateCheckAsync_WithResolvedDiscrepancy_Publishes()
    {
        await using var db = NewInMemoryDb();
        await SeedUsersAndReferenceDataAsync(db);
        var listing = await SeedListingAsync(db, claimedGrade: QualityGrade.GradeA);
        var service = NewService(db);
        await service.RecordInspectionAsync(
            new CreateInspectionRequest { ListingId = listing.Id, ConfirmedGrade = QualityGrade.GradeB }, OfficerId);
        var flag = Assert.Single(db.GradeDiscrepancyFlags);
        await service.ResolveDiscrepancyAsync(flag.Id, new ResolveDiscrepancyRequest { ResolutionNotes = "Re-checked, accepted." }, OfficerId);

        var result = await service.PublishListingWithGateCheckAsync(listing.Id, OfficerId);

        Assert.Equal(ListingStatus.Published, result.Status);
    }
}
