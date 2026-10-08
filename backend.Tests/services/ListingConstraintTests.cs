using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.services;

/// <summary>
/// DB-03 / DB-04 (plan: database-level CHECK constraints for the Listings table).
/// Every other write-heavy table in the schema (Order, StockReservation,
/// PickupSchedule, CollectionCentre) has a CHECK constraint guarding its core
/// numeric invariant — Listing never got one. These tests go straight at a real
/// PostgreSQL instance (not the InMemory provider, which does not enforce CHECK
/// constraints at all) so a passing result here means the database itself, not
/// just application code, refuses an invalid row.
/// </summary>
public class ListingConstraintTests
{
    private static AgriConnectDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AgriConnectDbContext>()
            .UseNpgsql(TestDatabase.ConnectionString)
            .Options;
        return new AgriConnectDbContext(options);
    }

    private static async Task<(Guid cropId, Guid regionId, Guid farmerId)> SeedCropRegionAndFarmerAsync(AgriConnectDbContext db)
    {
        var crop = new Crop { Id = Guid.NewGuid(), Name = $"TestCrop-{Guid.NewGuid():N}", Category = "Vegetables" };
        var region = new Region { Id = Guid.NewGuid(), Name = $"TestRegion-{Guid.NewGuid():N}" };
        var farmer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test Farmer",
            Email = $"farmer-{Guid.NewGuid():N}@test.local",
            PasswordHash = "not-a-real-hash",
            Role = "Farmer",
            IsActive = true
        };
        db.Crops.Add(crop);
        db.Regions.Add(region);
        db.Users.Add(farmer);
        await db.SaveChangesAsync();
        return (crop.Id, region.Id, farmer.Id);
    }

    private static Listing NewListing(Guid cropId, Guid regionId, Guid farmerId, decimal quantity, decimal? minPrice) => new()
    {
        Id = Guid.NewGuid(),
        FarmerId = farmerId,
        CropId = cropId,
        RegionId = regionId,
        Quantity = quantity,
        Unit = "kg",
        ClaimedGrade = "Grade A",
        PickupWindowStart = DateTime.UtcNow.AddDays(1),
        PickupWindowEnd = DateTime.UtcNow.AddDays(3),
        Status = "Draft",
        MinPrice = minPrice,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task DB03_NegativeMinPrice_IsRejectedByTheDatabase()
    {
        await using var db = NewContext();
        var (cropId, regionId, farmerId) = await SeedCropRegionAndFarmerAsync(db);

        db.Listings.Add(NewListing(cropId, regionId, farmerId, quantity: 10m, minPrice: -5m));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DB03_ZeroMinPrice_IsRejectedByTheDatabase()
    {
        await using var db = NewContext();
        var (cropId, regionId, farmerId) = await SeedCropRegionAndFarmerAsync(db);

        db.Listings.Add(NewListing(cropId, regionId, farmerId, quantity: 10m, minPrice: 0m));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DB03_PositiveMinPrice_IsAccepted()
    {
        await using var db = NewContext();
        var (cropId, regionId, farmerId) = await SeedCropRegionAndFarmerAsync(db);

        db.Listings.Add(NewListing(cropId, regionId, farmerId, quantity: 10m, minPrice: 0.01m));

        await db.SaveChangesAsync(); // must not throw
    }

    [Fact]
    public async Task DB03_NullMinPrice_IsAccepted_FloorPriceIsOptional()
    {
        await using var db = NewContext();
        var (cropId, regionId, farmerId) = await SeedCropRegionAndFarmerAsync(db);

        db.Listings.Add(NewListing(cropId, regionId, farmerId, quantity: 10m, minPrice: null));

        await db.SaveChangesAsync(); // must not throw — MinPrice is nullable by design (FR3)
    }

    [Fact]
    public async Task DB04_NegativeQuantity_IsRejectedByTheDatabase()
    {
        await using var db = NewContext();
        var (cropId, regionId, farmerId) = await SeedCropRegionAndFarmerAsync(db);

        db.Listings.Add(NewListing(cropId, regionId, farmerId, quantity: -1m, minPrice: 100m));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DB04_ZeroQuantity_IsRejectedByTheDatabase()
    {
        await using var db = NewContext();
        var (cropId, regionId, farmerId) = await SeedCropRegionAndFarmerAsync(db);

        db.Listings.Add(NewListing(cropId, regionId, farmerId, quantity: 0m, minPrice: 100m));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DB04_PositiveQuantity_IsAccepted()
    {
        await using var db = NewContext();
        var (cropId, regionId, farmerId) = await SeedCropRegionAndFarmerAsync(db);

        db.Listings.Add(NewListing(cropId, regionId, farmerId, quantity: 0.01m, minPrice: 100m));

        await db.SaveChangesAsync(); // must not throw
    }
}
