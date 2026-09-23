using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriConnect.Api.Config;

/// <summary>
/// Seeds the shared reference tables (Crop, Region, User) with the canonical
/// development fixtures.
///
/// GUIDs are taken from <see cref="AnalyticsFixtures"/> so that Component D
/// price-trend and supply-event records — which already use those IDs as
/// foreign-key values — resolve correctly once FK constraints are added.
///
/// Safe to call multiple times; existing records are never duplicated (upsert
/// by name / email).
/// </summary>
public static class SharedReferenceSeeder
{
    public record SeedResult(int CropsAdded, int RegionsAdded, int UsersAdded);

    /// <summary>
    /// Inserts Crops, Regions, and seed Users that do not already exist.
    /// Must be called <em>before</em> <see cref="DataSeeder.SeedAsync"/> so
    /// that the Component D fixtures can reference the correct IDs.
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

        // The three crops requested in the spec. Additional crops from AnalyticsFixtures
        // are listed as commented entries so they can be enabled when Component A lands.
        var candidateCrops = new List<Crop>
        {
            new() { Id = AnalyticsFixtures.CropCarrot.Id,     Name = "Carrot",  CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = AnalyticsFixtures.CropTomato.Id,     Name = "Tomato",  CreatedAt = DateTimeOffset.UtcNow },
            // Cabbage is a spec requirement; its GUID is defined here because the
            // AnalyticsFixtures reference set does not currently include it.
            new() { Id = Guid.Parse("3f2a0006-0000-0000-0000-000000000006"),
                    Name = "Cabbage", CreatedAt = DateTimeOffset.UtcNow },
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
            new() { Id = AnalyticsFixtures.RegionNuwaraEliya.Id, Name = "Nuwara Eliya", CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = AnalyticsFixtures.RegionDambulla.Id,    Name = "Dambulla",     CreatedAt = DateTimeOffset.UtcNow },
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
                Id        = AnalyticsFixtures.AdminUserId,
                Email     = "admin@agriconnect.lk",
                Role      = "Administrator",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id        = AnalyticsFixtures.OfficerUserId,
                Email     = "officer@agriconnect.lk",
                Role      = "Officer",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id        = AnalyticsFixtures.FarmerUserId,
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
