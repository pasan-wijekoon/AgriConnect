using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.src.models;

namespace backend.src.config;

public static class DbSeeder
{
    public static readonly Guid DefaultOfficerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid FarmerKamalId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid FarmerNimalId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid FarmerSunimalId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly Guid RegionColomboId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid RegionGampahaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid RegionKandyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static readonly Guid CropTomatoId = Guid.Parse("d1111111-0000-0000-0000-000000000001");
    public static readonly Guid CropCarrotId = Guid.Parse("d1111111-0000-0000-0000-000000000002");
    public static readonly Guid CropGreenBeansId = Guid.Parse("d1111111-0000-0000-0000-000000000003");
    public static readonly Guid CropBellPepperId = Guid.Parse("d1111111-0000-0000-0000-000000000004");
    public static readonly Guid CropCabbageId = Guid.Parse("d1111111-0000-0000-0000-000000000005");

    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync())
        {
            return; // Already seeded
        }

        // 1. Seed Regions & Collection Centres
        var regionColombo = new Region
        {
            Id = RegionColomboId,
            Name = "Western - Colombo"
        };
        var regionGampaha = new Region
        {
            Id = RegionGampahaId,
            Name = "Western - Gampaha"
        };
        var regionKandy = new Region
        {
            Id = RegionKandyId,
            Name = "Central - Kandy"
        };

        context.Regions.AddRange(regionColombo, regionGampaha, regionKandy);
        await context.SaveChangesAsync();

        var centreColombo = new CollectionCentre
        {
            Id = Guid.NewGuid(),
            Name = "Colombo Central Agri Collection Centre",
            Latitude = 6.9271m,
            Longitude = 79.8612m,
            Capacity = 5000,
            RegionId = RegionColomboId
        };
        var centreGampaha = new CollectionCentre
        {
            Id = Guid.NewGuid(),
            Name = "Gampaha Regional Distribution Centre",
            Latitude = 7.0840m,
            Longitude = 80.0098m,
            Capacity = 3500,
            RegionId = RegionGampahaId
        };
        context.CollectionCentres.AddRange(centreColombo, centreGampaha);
        await context.SaveChangesAsync();

        // 2. Seed Users
        var officer = new User
        {
            Id = DefaultOfficerId,
            FullName = "Kamal Gunawardena",
            Email = "officer.kamal@agriconnect.lk",
            Role = UserRole.Officer.ToString(),
            Phone = "+94 77 123 4567",
            RegionId = RegionColomboId,
            PasswordHash = "hashed_password_sample"
        };

        var farmerKamal = new User
        {
            Id = FarmerKamalId,
            FullName = "Sunil Perera (Farmer)",
            Email = "sunil.farmer@agriconnect.lk",
            Role = UserRole.Farmer.ToString(),
            Phone = "+94 71 987 6543",
            RegionId = RegionColomboId,
            PasswordHash = "hashed_password_sample"
        };

        var farmerNimal = new User
        {
            Id = FarmerNimalId,
            FullName = "Nimal Bandara (Farmer)",
            Email = "nimal.farmer@agriconnect.lk",
            Role = UserRole.Farmer.ToString(),
            Phone = "+94 76 555 1234",
            RegionId = RegionGampahaId,
            PasswordHash = "hashed_password_sample"
        };

        var farmerSunimal = new User
        {
            Id = FarmerSunimalId,
            FullName = "Anura Jayasinghe (Farmer)",
            Email = "anura.farmer@agriconnect.lk",
            Role = UserRole.Farmer.ToString(),
            Phone = "+94 78 444 9876",
            RegionId = RegionKandyId,
            PasswordHash = "hashed_password_sample"
        };

        context.Users.AddRange(officer, farmerKamal, farmerNimal, farmerSunimal);
        await context.SaveChangesAsync();

        // 3. Seed Crops
        var cropTomato = new Crop { Id = CropTomatoId, Name = "Tomatoes (Thalathuoya)", Category = "Vegetables" };
        var cropCarrot = new Crop { Id = CropCarrotId, Name = "Carrots (Nuwara Eliya)", Category = "Vegetables" };
        var cropBeans = new Crop { Id = CropGreenBeansId, Name = "Green Beans", Category = "Vegetables" };
        var cropPepper = new Crop { Id = CropBellPepperId, Name = "Bell Peppers (Yellow/Red)", Category = "Vegetables" };
        var cropCabbage = new Crop { Id = CropCabbageId, Name = "Green Cabbage", Category = "Vegetables" };

        context.Crops.AddRange(cropTomato, cropCarrot, cropBeans, cropPepper, cropCabbage);
        await context.SaveChangesAsync();

        // 4. Seed Listings
        // Listing 1: Pending Inspection - Tomatoes
        var listing1Id = Guid.Parse("e1111111-1111-1111-1111-111111111111");
        var listing1 = new Listing
        {
            Id = listing1Id,
            FarmerId = FarmerKamalId,
            CropId = CropTomatoId,
            RegionId = RegionColomboId,
            Quantity = 250m,
            Unit = "kg",
            ClaimedGrade = QualityGrade.GradeA,
            PickupWindowStart = DateTime.UtcNow.AddDays(1),
            PickupWindowEnd = DateTime.UtcNow.AddDays(3),
            Status = ListingStatus.PendingApproval,
            MinPrice = 280.00m,
            CreatedAt = DateTime.UtcNow.AddHours(-4)
        };
        listing1.Photos.Add(new ListingPhoto { Id = Guid.NewGuid(), ListingId = listing1Id, Url = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?auto=format&fit=crop&w=600&q=80" });

        // Listing 2: Pending Inspection - Carrots
        var listing2Id = Guid.Parse("e2222222-2222-2222-2222-222222222222");
        var listing2 = new Listing
        {
            Id = listing2Id,
            FarmerId = FarmerNimalId,
            CropId = CropCarrotId,
            RegionId = RegionGampahaId,
            Quantity = 180m,
            Unit = "kg",
            ClaimedGrade = QualityGrade.GradeA,
            PickupWindowStart = DateTime.UtcNow.AddDays(2),
            PickupWindowEnd = DateTime.UtcNow.AddDays(4),
            Status = ListingStatus.PendingApproval,
            MinPrice = 320.00m,
            CreatedAt = DateTime.UtcNow.AddHours(-12)
        };
        listing2.Photos.Add(new ListingPhoto { Id = Guid.NewGuid(), ListingId = listing2Id, Url = "https://images.unsplash.com/photo-1598170845058-32b9d6a5da37?auto=format&fit=crop&w=600&q=80" });

        // Listing 3: Inspected with Discrepancy (Claimed Grade A, Confirmed Grade B)
        var listing3Id = Guid.Parse("e3333333-3333-3333-3333-333333333333");
        var listing3 = new Listing
        {
            Id = listing3Id,
            FarmerId = FarmerSunimalId,
            CropId = CropBellPepperId,
            RegionId = RegionKandyId,
            Quantity = 120m,
            Unit = "kg",
            ClaimedGrade = QualityGrade.GradeA,
            PickupWindowStart = DateTime.UtcNow.AddDays(1),
            PickupWindowEnd = DateTime.UtcNow.AddDays(2),
            Status = ListingStatus.PendingApproval,
            MinPrice = 450.00m,
            CreatedAt = DateTime.UtcNow.AddHours(-24)
        };
        listing3.Photos.Add(new ListingPhoto { Id = Guid.NewGuid(), ListingId = listing3Id, Url = "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=600&q=80" });

        // Listing 4: Inspected & Published (Tomatoes)
        var listing4Id = Guid.Parse("e4444444-4444-4444-4444-444444444444");
        var listing4 = new Listing
        {
            Id = listing4Id,
            FarmerId = FarmerKamalId,
            CropId = CropGreenBeansId,
            RegionId = RegionColomboId,
            Quantity = 300m,
            Unit = "kg",
            ClaimedGrade = QualityGrade.GradeA,
            PickupWindowStart = DateTime.UtcNow.AddDays(1),
            PickupWindowEnd = DateTime.UtcNow.AddDays(5),
            Status = ListingStatus.Published,
            MinPrice = 210.00m,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        listing4.Photos.Add(new ListingPhoto { Id = Guid.NewGuid(), ListingId = listing4Id, Url = "https://images.unsplash.com/photo-1551893665-f843f600794e?auto=format&fit=crop&w=600&q=80" });

        context.Listings.AddRange(listing1, listing2, listing3, listing4);
        await context.SaveChangesAsync();

        // 5. Seed Inspections & Discrepancies
        // Inspection for Listing 3 (Grade B mismatch)
        var insp3Id = Guid.NewGuid();
        var inspection3 = new Inspection
        {
            Id = insp3Id,
            ListingId = listing3Id,
            OfficerId = DefaultOfficerId,
            ConfirmedGrade = QualityGrade.GradeB,
            Notes = "Produce size is inconsistent with Grade A export standards (diameter varies between 4-7cm). Slight skin blemishes on ~15% of samples. Downgraded to Grade B standard commercial grade.",
            InspectedAt = DateTime.UtcNow.AddHours(-8)
        };
        inspection3.Photos.Add(new InspectionPhoto { Id = Guid.NewGuid(), InspectionId = insp3Id, Url = "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?auto=format&fit=crop&w=600&q=80" });

        var flag3 = new GradeDiscrepancyFlag
        {
            Id = Guid.NewGuid(),
            ListingId = listing3Id,
            InspectionId = insp3Id,
            ClaimedGrade = QualityGrade.GradeA,
            ConfirmedGrade = QualityGrade.GradeB,
            FlaggedAt = DateTime.UtcNow.AddHours(-8)
        };

        // Inspection for Listing 4 (Grade A verified)
        var insp4Id = Guid.NewGuid();
        var inspection4 = new Inspection
        {
            Id = insp4Id,
            ListingId = listing4Id,
            OfficerId = DefaultOfficerId,
            ConfirmedGrade = QualityGrade.GradeA,
            Notes = "Fresh harvest, uniform green color, crisp pods, zero pest damage. Passed Grade A verification criteria.",
            InspectedAt = DateTime.UtcNow.AddDays(-1)
        };
        inspection4.Photos.Add(new InspectionPhoto { Id = Guid.NewGuid(), InspectionId = insp4Id, Url = "https://images.unsplash.com/photo-1551893665-f843f600794e?auto=format&fit=crop&w=600&q=80" });

        context.Inspections.AddRange(inspection3, inspection4);
        context.GradeDiscrepancyFlags.Add(flag3);

        context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = DefaultOfficerId,
            Action = "PublishListingApproved",
            EntityType = "Listing",
            EntityId = listing4Id,
            Timestamp = DateTime.UtcNow.AddDays(-1),
            Details = "{\"ListingId\":\"" + listing4Id + "\",\"ConfirmedGrade\":\"Grade A\",\"Status\":\"Published\"}"
        });

        await context.SaveChangesAsync();
    }
}
