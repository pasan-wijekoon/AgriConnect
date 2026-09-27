using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriConnect.Api.Config;

/// <summary>
/// Seeds the shared reference tables (Crop, Region, User) with the canonical
/// development fixtures.
///
/// GUIDs are fixed so every developer's database gets the same IDs. Other seeders
/// should still look rows up by name rather than depend on these values.
///
/// Safe to call multiple times; existing records are never duplicated (upsert
/// by name / email).
/// </summary>
public static class SharedReferenceSeeder
{
    public record SeedResult(int CropsAdded, int RegionsAdded, int UsersAdded);

    private static readonly Guid CarrotId = Guid.Parse("3f2a0001-0000-0000-0000-000000000001");
    private static readonly Guid TomatoId = Guid.Parse("3f2a0002-0000-0000-0000-000000000002");
    private static readonly Guid CabbageId = Guid.Parse("3f2a0006-0000-0000-0000-000000000006");
    private static readonly Guid NuwaraEliyaId = Guid.Parse("8b1c0001-0000-0000-0000-000000000001");
    private static readonly Guid DambullaId = Guid.Parse("8b1c0002-0000-0000-0000-000000000002");
    private static readonly Guid AdminUserId = Guid.Parse("a1111111-0000-0000-0000-000000000001");
    private static readonly Guid OfficerUserId = Guid.Parse("a2222222-0000-0000-0000-000000000001");
    private static readonly Guid FarmerUserId = Guid.Parse("f1111111-0000-0000-0000-000000000001");

    /// <summary>
    /// Inserts Crops, Regions, and seed Users that do not already exist.
    /// Must run <em>before</em> <see cref="AnalyticsFixtures.Seed"/>, which looks
    /// crops and regions up by name.
    /// </summary>
    public static async Task<SeedResult> SeedAsync(
        AgriConnectDbContext context,
        ILogger? logger = null)
    {
        logger?.LogInformation("[SharedReferenceSeeder] Checking shared reference fixtures...");

        // ---- Crops ------------------------------------------------------------
        var existingCropNames = (await context.Crops
            .AsNoTracking()
            .Select(c => c.Name)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidateCrops = new List<Crop>
        {
            new() { Id = CarrotId,  Name = "Carrot",  CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = TomatoId,  Name = "Tomato",  CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = CabbageId, Name = "Cabbage", CreatedAt = DateTimeOffset.UtcNow },
        };

        var newCrops = candidateCrops
            .Where(c => !existingCropNames.Contains(c.Name))
            .ToList();

        if (newCrops.Count > 0)
        {
            await context.Crops.AddRangeAsync(newCrops);
            logger?.LogInformation("[SharedReferenceSeeder] Adding {Count} crops: {Names}.",
                newCrops.Count, string.Join(", ", newCrops.Select(c => c.Name)));
        }

        // ---- Regions ----------------------------------------------------------
        var existingRegionNames = (await context.Regions
            .AsNoTracking()
            .Select(r => r.Name)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidateRegions = new List<Region>
        {
            new() { Id = NuwaraEliyaId, Name = "Nuwara Eliya", CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = DambullaId,    Name = "Dambulla",     CreatedAt = DateTimeOffset.UtcNow },
        };

        var newRegions = candidateRegions
            .Where(r => !existingRegionNames.Contains(r.Name))
            .ToList();

        if (newRegions.Count > 0)
        {
            await context.Regions.AddRangeAsync(newRegions);
            logger?.LogInformation("[SharedReferenceSeeder] Adding {Count} regions: {Names}.",
                newRegions.Count, string.Join(", ", newRegions.Select(r => r.Name)));
        }

        // ---- Users (dev seed only — real users come from auth registration) ---
        var existingUserEmails = (await context.Users
            .AsNoTracking()
            .Select(u => u.Email)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidateUsers = new List<User>
        {
            new()
            {
                Id        = AdminUserId,
                Email     = "admin@agriconnect.lk",
                Role      = "Administrator",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id        = OfficerUserId,
                Email     = "officer@agriconnect.lk",
                Role      = "Officer",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id        = FarmerUserId,
                Email     = "farmer@agriconnect.lk",
                Role      = "Farmer",
                CreatedAt = DateTimeOffset.UtcNow
            },
        };

        var newUsers = candidateUsers
            .Where(u => !existingUserEmails.Contains(u.Email))
            .ToList();

        if (newUsers.Count > 0)
        {
            await context.Users.AddRangeAsync(newUsers);
            logger?.LogInformation("[SharedReferenceSeeder] Adding {Count} seed users: {Emails}.",
                newUsers.Count, string.Join(", ", newUsers.Select(u => u.Email)));
        }

        int totalNew = newCrops.Count + newRegions.Count + newUsers.Count;
        if (totalNew > 0)
        {
            await context.SaveChangesAsync();
            logger?.LogInformation("[SharedReferenceSeeder] Saved {Count} new reference rows.", totalNew);
        }
        else
        {
            logger?.LogInformation("[SharedReferenceSeeder] All shared reference fixtures already present; no changes needed.");
        }

        return new SeedResult(newCrops.Count, newRegions.Count, newUsers.Count);
    }
}
