using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Config;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<PriceSuggestion> PriceSuggestions => Set<PriceSuggestion>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TodayPriceCatalogItem> TodayPriceCatalogItems => Set<TodayPriceCatalogItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Listing ──────────────────────────────────────────────
        mb.Entity<Listing>(e =>
        {
            e.HasKey(l => l.Id);

            e.Property(l => l.Quantity).IsRequired();
            e.Property(l => l.PickupWindowStart).IsRequired();
            e.Property(l => l.PickupWindowEnd).IsRequired();

            // Composite index for search/filter/sort (FR6)
            e.HasIndex(l => new { l.CropId, l.RegionId, l.Status, l.ClaimedGrade });

            e.HasIndex(l => l.FarmerId);

            e.HasMany(l => l.Photos)
             .WithOne(p => p.Listing)
             .HasForeignKey(p => p.ListingId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.PriceSuggestion)
             .WithOne(ps => ps.Listing)
             .HasForeignKey<PriceSuggestion>(ps => ps.ListingId);

            e.HasOne(l => l.Crop)
             .WithMany()
             .HasForeignKey(l => l.CropId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(l => l.Region)
             .WithMany()
             .HasForeignKey(l => l.RegionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ListingPhoto ─────────────────────────────────────────
        mb.Entity<ListingPhoto>(e =>
        {
            e.HasKey(p => p.Id);
            // Allow unlimited-length URLs (base64 data URIs can be very large)
            e.Property(p => p.Url).HasColumnType("text");
        });

        // ── PriceSuggestion ──────────────────────────────────────
        mb.Entity<PriceSuggestion>(e =>
        {
            e.HasKey(ps => ps.Id);
            e.HasIndex(ps => ps.ListingId).IsUnique(); // 1-to-1
        });

        // ── Crop ─────────────────────────────────────────────────
        mb.Entity<Crop>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Name).IsUnique();
        });

        // ── Region ───────────────────────────────────────────────
        mb.Entity<Region>(e =>
        {
            e.HasKey(r => r.Id);
        });

        // ── User ─────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
        });

        // ── TodayPriceCatalogItem ──────────────────────────────────
        mb.Entity<TodayPriceCatalogItem>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.DisplayOrder);
        });

        // ── Seed Data ────────────────────────────────────────────
        SeedData(mb);
    }

    private static void SeedData(ModelBuilder mb)
    {
        // Common Sri Lankan crops
        var crops = new[]
        {
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "Rice",      Category = "Grains" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), Name = "Tea",       Category = "Beverages" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"), Name = "Coconut",   Category = "Fruits" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"), Name = "Tomatoes",  Category = "Vegetables" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"), Name = "Carrots",   Category = "Vegetables" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000006"), Name = "Chili",     Category = "Spices" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000007"), Name = "Onions",    Category = "Vegetables" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000008"), Name = "Potatoes",  Category = "Vegetables" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000009"), Name = "Cinnamon",  Category = "Spices" },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000a"), Name = "Banana",    Category = "Fruits" },
        };
        mb.Entity<Crop>().HasData(crops);

        // All 25 Sri Lankan Districts
        var regions = new[]
        {
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000001"), Name = "Colombo" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000002"), Name = "Kandy" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000003"), Name = "Galle" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000004"), Name = "Jaffna" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000005"), Name = "Anuradhapura" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000006"), Name = "Matara" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000007"), Name = "Kurunegala" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000008"), Name = "Nuwara Eliya" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000009"), Name = "Gampaha" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000a"), Name = "Kalutara" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000b"), Name = "Matale" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000c"), Name = "Ratnapura" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000d"), Name = "Kegalle" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000e"), Name = "Badulla" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000f"), Name = "Monaragala" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000010"), Name = "Hambantota" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000011"), Name = "Trincomalee" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000012"), Name = "Batticaloa" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000013"), Name = "Ampara" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000014"), Name = "Puttalam" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000015"), Name = "Polonnaruwa" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000016"), Name = "Kilinochchi" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000017"), Name = "Mannar" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000018"), Name = "Mullaitivu" },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000019"), Name = "Vavuniya" },
        };
        mb.Entity<Region>().HasData(regions);

        // Demo users (password for all: "password")
        // PBKDF2-HMAC-SHA256, format "{iterations}.{saltBase64}.{hashBase64}" — see AuthService.HashPassword
        var passwordHash = "600000.ezjZQwN7EZYdigiik+HqbA==.iE33PwV1IisZPhE+tOJgA6uVMgcWO0OqPdC6pWkc+9w=";

        var users = new[]
        {
            new User
            {
                Id = Guid.Parse("f0000000-0000-0000-0000-000000000001"),
                FullName = "Kamal Perera",
                Email = "farmer@agriconnect.lk",
                PasswordHash = passwordHash,
                Role = "Farmer",
                Phone = "+94771234567",
                Region = "Nuwara Eliya",
                AvatarUrl = null,
                CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new User
            {
                Id = Guid.Parse("f0000000-0000-0000-0000-000000000002"),
                FullName = "Saman Silva",
                Email = "farmer2@agriconnect.lk",
                PasswordHash = passwordHash,
                Role = "Farmer",
                Phone = "+94779876543",
                Region = "Kandy",
                AvatarUrl = null,
                CreatedAt = new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new User
            {
                Id = Guid.Parse("f0000000-0000-0000-0000-000000000010"),
                FullName = "Nihal Fernando",
                Email = "buyer@agriconnect.lk",
                PasswordHash = passwordHash,
                Role = "Buyer",
                Phone = "+94701234567",
                Region = "Colombo",
                AvatarUrl = null,
                CreatedAt = new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new User
            {
                Id = Guid.Parse("f0000000-0000-0000-0000-000000000099"),
                FullName = "N. Perera",
                Email = "admin@agriconnect.lk",
                PasswordHash = passwordHash,
                Role = "Admin",
                Phone = "+94112345678",
                Region = "Nuwara Eliya",
                AvatarUrl = null,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
        };
        mb.Entity<User>().HasData(users);

        // Today's Prices catalog — admin-managed list of crops shown on the
        // marketplace discovery page. Seeded once from the platform's original
        // fixed 20-item list; from here on it's fully editable via the Admin UI.
        var fixedTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var todayPriceCatalog = new[]
        {
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000001"), Name = "Tomatoes", Category = "Vegetables", Unit = "kg", DefaultRegion = "Dambulla", ImageUrl = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=500&auto=format&fit=crop", DisplayOrder = 1, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000002"), Name = "Carrots", Category = "Vegetables", Unit = "kg", DefaultRegion = "Nuwara Eliya", ImageUrl = "https://images.unsplash.com/photo-1598170845058-32b9d6a5c317?w=500&auto=format&fit=crop", DisplayOrder = 2, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000003"), Name = "Potatoes", Category = "Vegetables", Unit = "kg", DefaultRegion = "Nuwara Eliya", ImageUrl = "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=500&auto=format&fit=crop", DisplayOrder = 3, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000004"), Name = "Onions", Category = "Vegetables", Unit = "kg", DefaultRegion = "Dambulla", ImageUrl = "https://images.unsplash.com/photo-1618512496248-a07fe83aa8cb?w=500&auto=format&fit=crop", DisplayOrder = 4, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000005"), Name = "Chili", Category = "Spices", Unit = "kg", DefaultRegion = "Jaffna", ImageUrl = "https://images.unsplash.com/photo-1588252303782-cb80119abd6d?w=500&auto=format&fit=crop", DisplayOrder = 5, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000006"), Name = "Rice (Keeri Samba)", Category = "Grains", Unit = "kg", DefaultRegion = "Anuradhapura", ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=500&auto=format&fit=crop", DisplayOrder = 6, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000007"), Name = "Banana (Ambul)", Category = "Fruits", Unit = "kg", DefaultRegion = "Kurunegala", ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=500&auto=format&fit=crop", DisplayOrder = 7, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000008"), Name = "Coconut", Category = "Fruits", Unit = "nut", DefaultRegion = "Kurunegala", ImageUrl = "https://images.unsplash.com/photo-1544376798-89aa6b82c6cd?w=500&auto=format&fit=crop", DisplayOrder = 8, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000009"), Name = "Tea (BOP)", Category = "Beverages", Unit = "kg", DefaultRegion = "Nuwara Eliya", ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=500&auto=format&fit=crop", DisplayOrder = 9, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000a"), Name = "Cinnamon (Alba)", Category = "Spices", Unit = "kg", DefaultRegion = "Matara", ImageUrl = "https://images.unsplash.com/photo-1509358271058-acd22cc93898?w=500&auto=format&fit=crop", DisplayOrder = 10, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000b"), Name = "Leeks", Category = "Vegetables", Unit = "kg", DefaultRegion = "Nuwara Eliya", ImageUrl = "https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=500&auto=format&fit=crop", DisplayOrder = 11, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000c"), Name = "Cabbage", Category = "Vegetables", Unit = "kg", DefaultRegion = "Nuwara Eliya", ImageUrl = "https://images.unsplash.com/photo-1594282486552-05b4d80fbb9f?w=500&auto=format&fit=crop", DisplayOrder = 12, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000d"), Name = "Pumpkin", Category = "Vegetables", Unit = "kg", DefaultRegion = "Kurunegala", ImageUrl = "https://images.unsplash.com/photo-1570586437263-ab629fccc818?w=500&auto=format&fit=crop", DisplayOrder = 13, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000e"), Name = "Beans", Category = "Vegetables", Unit = "kg", DefaultRegion = "Badulla", ImageUrl = "https://images.unsplash.com/photo-1567375698348-5d9d5ae10c3a?w=500&auto=format&fit=crop", DisplayOrder = 14, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-00000000000f"), Name = "Papaya", Category = "Fruits", Unit = "kg", DefaultRegion = "Gampaha", ImageUrl = "https://images.unsplash.com/photo-1517282009859-f000ec3b26fe?w=500&auto=format&fit=crop", DisplayOrder = 15, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000010"), Name = "Mango", Category = "Fruits", Unit = "kg", DefaultRegion = "Jaffna", ImageUrl = "https://images.unsplash.com/photo-1553279768-865429fa0078?w=500&auto=format&fit=crop", DisplayOrder = 16, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000011"), Name = "Pepper (Black)", Category = "Spices", Unit = "kg", DefaultRegion = "Matale", ImageUrl = "https://images.unsplash.com/photo-1599909533601-aa1e5c0fb0a4?w=500&auto=format&fit=crop", DisplayOrder = 17, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000012"), Name = "Drumstick (Murunga)", Category = "Vegetables", Unit = "kg", DefaultRegion = "Jaffna", ImageUrl = "https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=500&auto=format&fit=crop", DisplayOrder = 18, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000013"), Name = "Brinjal (Eggplant)", Category = "Vegetables", Unit = "kg", DefaultRegion = "Dambulla", ImageUrl = "https://images.unsplash.com/photo-1613881553903-4bedfcea4dd1?w=500&auto=format&fit=crop", DisplayOrder = 19, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
            new TodayPriceCatalogItem { Id = Guid.Parse("d0000000-0000-0000-0000-000000000014"), Name = "Lime", Category = "Fruits", Unit = "kg", DefaultRegion = "Colombo", ImageUrl = "https://images.unsplash.com/photo-1590502593747-42a996133562?w=500&auto=format&fit=crop", DisplayOrder = 20, CreatedAt = fixedTimestamp, UpdatedAt = fixedTimestamp },
        };
        mb.Entity<TodayPriceCatalogItem>().HasData(todayPriceCatalog);
    }
}
