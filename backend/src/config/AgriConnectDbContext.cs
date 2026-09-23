using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Config;

/// <summary>
/// EF Core context for AgriConnect.
///
/// Components add their own DbSets and configuration here as they land.
/// Keep each component's configuration in its own private method below so the
/// file stays mergeable when several people extend it at once.
///
/// Current coverage:
///   - Shared reference tables : Crop, Region, User
///   - Component D             : Market Price Analytics &amp; Reporting (FR15–FR18)
/// </summary>
public class AgriConnectDbContext : DbContext
{
    public AgriConnectDbContext(DbContextOptions<AgriConnectDbContext> options)
        : base(options)
    {
    }

    // ---- Shared Reference Tables -------------------------------------------
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<User> Users => Set<User>();

    // ---- Component D — Market Price Analytics & Reporting -------------------
    public DbSet<PriceTrendSnapshot> PriceTrendSnapshots => Set<PriceTrendSnapshot>();
    public DbSet<ShortageOversupplyEvent> ShortageOversupplyEvents => Set<ShortageOversupplyEvent>();
    public DbSet<PriceAnomalyFlag> PriceAnomalyFlags => Set<PriceAnomalyFlag>();
    public DbSet<ReportExport> ReportExports => Set<ReportExport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureSharedTables(modelBuilder);
        ConfigureComponentD(modelBuilder);
    }

    /// <summary>
    /// Shared reference tables — Crop, Region, User.
    /// Unique indexes are enforced so that the seeder can use upsert-style logic
    /// (check by name / email before inserting) without risking duplicates.
    /// </summary>
    private static void ConfigureSharedTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Crop>(entity =>
        {
            entity.ToTable("Crop");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name)
                  .IsUnique()
                  .HasDatabaseName("IX_Crop_Name");
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.ToTable("Region");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name)
                  .IsUnique()
                  .HasDatabaseName("IX_Region_Name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", t =>
                t.HasCheckConstraint(
                    "CK_User_Role",
                    "\"Role\" IN ('Farmer','Buyer','Officer','Administrator')"));

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);

            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_User_Email");
        });
    }

    /// <summary>
    /// Component D schema, mapped to match the design spec (DFD 6.3) rather than
    /// EF defaults.
    ///
    /// Note on foreign keys: CropId, RegionId, ListingId and RequestedBy reference
    /// the Crop, Region, and User tables above. They are currently stored as plain
    /// indexed Guid columns without EF navigation properties or FK constraints, so
    /// the Component D migration can be applied independently. A follow-up migration
    /// will add real FK constraints once Component A (Listings) lands.
    /// </summary>
    private static void ConfigureComponentD(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceTrendSnapshot>(entity =>
        {
            entity.ToTable("PriceTrendSnapshot", t =>
                t.HasCheckConstraint("CK_PriceTrendSnapshot_SampleCount", "\"SampleCount\" >= 0"));

            entity.HasKey(e => e.Id);

            // One snapshot per crop, per region, per period bucket. The aggregation
            // job upserts against this index, which is what makes a rebuild idempotent.
            entity.HasIndex(e => new { e.CropId, e.RegionId, e.Period })
                  .IsUnique()
                  .HasDatabaseName("IX_PriceTrendSnapshot_Crop_Region_Period");

            entity.Property(e => e.Period).HasColumnType("date");
            entity.Property(e => e.AvgPrice).HasColumnType("numeric(12,2)");
            entity.Property(e => e.MinPrice).HasColumnType("numeric(12,2)");
            entity.Property(e => e.MaxPrice).HasColumnType("numeric(12,2)");
        });

        modelBuilder.Entity<ShortageOversupplyEvent>(entity =>
        {
            entity.ToTable("ShortageOversupplyEvent", t =>
            {
                t.HasCheckConstraint(
                    "CK_ShortageOversupplyEvent_Type",
                    "\"Type\" IN ('Shortage','Oversupply')");
                t.HasCheckConstraint(
                    "CK_ShortageOversupplyEvent_Severity",
                    "\"Severity\" IN ('Low','Medium','High')");
            });

            entity.HasKey(e => e.Id);

            // Stored as text, not an int, so the CHECK constraints above are
            // readable in the database and survive enum reordering in C#.
            entity.Property(e => e.Type)
                  .HasConversion<string>()
                  .HasMaxLength(15)
                  .IsRequired();

            entity.Property(e => e.Severity)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.DetectedAt).IsRequired();
            entity.Property(e => e.Notes).HasColumnType("text");

            entity.HasIndex(e => new { e.CropId, e.RegionId })
                  .HasDatabaseName("IX_ShortageOversupplyEvent_Crop_Region");
        });

        modelBuilder.Entity<PriceAnomalyFlag>(entity =>
        {
            entity.ToTable("PriceAnomalyFlag", t =>
                t.HasCheckConstraint(
                    "CK_PriceAnomalyFlag_Status",
                    "\"Status\" IN ('Open','Reviewed','Dismissed')"));

            entity.HasKey(e => e.Id);

            entity.Property(e => e.DeviationPercent).HasColumnType("numeric(5,2)");

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(15)
                  .IsRequired();

            entity.Property(e => e.FlaggedAt).IsRequired();

            // The officer review queue filters by listing and by status.
            entity.HasIndex(e => e.ListingId)
                  .HasDatabaseName("IX_PriceAnomalyFlag_ListingId");
            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("IX_PriceAnomalyFlag_Status");
        });

        modelBuilder.Entity<ReportExport>(entity =>
        {
            entity.ToTable("ReportExport");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type).HasMaxLength(30).IsRequired();
            entity.Property(e => e.DateRangeStart).HasColumnType("date");
            entity.Property(e => e.DateRangeEnd).HasColumnType("date");
            entity.Property(e => e.GeneratedAt).IsRequired();
            entity.Property(e => e.FileUrl).HasMaxLength(500).IsRequired();

            entity.HasIndex(e => e.RequestedBy)
                  .HasDatabaseName("IX_ReportExport_RequestedBy");
        });
    }
}
