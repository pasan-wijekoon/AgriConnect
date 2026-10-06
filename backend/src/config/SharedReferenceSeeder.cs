using AgriConnect.Api.Models;
using AgriConnect.Api.Services;
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

    private static readonly Guid AdminUserId = Guid.Parse("a1111111-0000-0000-0000-000000000001");
    private static readonly Guid OfficerUserId = Guid.Parse("a2222222-0000-0000-0000-000000000001");
    private static readonly Guid Officer2UserId = Guid.Parse("a2222222-0000-0000-0000-000000000002");
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

        // ---- Crops / Regions ----------------------------------------------------
        // No longer seeded here as of the 2026-09-27 integration: Component A's real
        // Crop/Region data (10 crops, 25 districts) now lands via migration-time
        // HasData() seeding (AgriConnectDbContext.SeedComponentAReferenceData), which
        // always runs before this method does. This method's own candidate lists used
        // singular/differently-cased names ("Carrot", "Dambulla") that don't match
        // Component A's real rows ("Carrots", district names) — upserting them by name
        // no longer recognized them as duplicates and kept inserting confusing,
        // functionally-orphaned extra rows next to the real ones. Left as a no-op
        // (rather than deleted outright) so this method's shape/doc comment stay
        // intact for whoever revisits Crop/Region seeding next.
        var newCrops = new List<Crop>();
        var newRegions = new List<Region>();

        // ---- Users (dev seed only — real users come from auth registration) ---
        // Demo passwords are the documented dev value "password" (same as
        // backend/create_users.sql's demo accounts) — dev/demo seed data only.
        var demoPasswordHash = AuthService.HashPassword("password");
        var kandyCentreId = OrderLogisticsFixtures.GetCollectionCentres()[0].Id;
        var colomboCentreId = OrderLogisticsFixtures.GetCollectionCentres()[4].Id;

        var candidateUsers = new List<User>
        {
            new()
            {
                Id        = AdminUserId,
                FullName  = "System Administrator",
                Email     = "admin@agriconnect.lk",
                PasswordHash = demoPasswordHash,
                Role      = "Administrator",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id        = OfficerUserId,
                FullName  = "Kandy Centre Officer",
                Email     = "officer@agriconnect.lk",
                PasswordHash = demoPasswordHash,
                Role      = "Officer",
                Region    = "Kandy",
                CollectionCentreId = kandyCentreId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id        = Officer2UserId,
                FullName  = "Colombo Centre Officer",
                Email     = "officer2@agriconnect.lk",
                PasswordHash = demoPasswordHash,
                Role      = "Officer",
                Region    = "Colombo",
                CollectionCentreId = colomboCentreId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id        = FarmerUserId,
                FullName  = "Demo Farmer",
                Email     = "farmer@agriconnect.lk",
                Role      = "Farmer",
                CreatedAt = DateTimeOffset.UtcNow
            },
        };

        var existingUsers = await context.Users.ToListAsync();
        var newUsers = new List<User>();
        foreach (var candidate in candidateUsers)
        {
            var existing = existingUsers.FirstOrDefault(
                u => string.Equals(u.Email, candidate.Email, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                newUsers.Add(candidate);
                continue;
            }

            // Officers seeded by older versions had no name/password (couldn't log
            // in at all) and no centre binding — backfill without touching accounts
            // that already have credentials.
            if (existing.Role == "Officer" || existing.Role == "Administrator")
            {
                if (string.IsNullOrEmpty(existing.PasswordHash)) existing.PasswordHash = candidate.PasswordHash;
                if (string.IsNullOrEmpty(existing.FullName)) existing.FullName = candidate.FullName;
                if (existing.Role == "Officer" && existing.CollectionCentreId is null)
                    existing.CollectionCentreId = candidate.CollectionCentreId;
            }
        }

        if (newUsers.Count > 0)
        {
            await context.Users.AddRangeAsync(newUsers);
            logger?.LogInformation("[SharedReferenceSeeder] Adding {Count} seed users: {Emails}.",
                newUsers.Count, string.Join(", ", newUsers.Select(u => u.Email)));
        }

        int totalNew = newCrops.Count + newRegions.Count + newUsers.Count;
        if (totalNew > 0 || context.ChangeTracker.HasChanges())
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
