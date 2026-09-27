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
///   - Component B             : Order &amp; Collection-Centre Logistics
///   - Shared / Cross-Cutting  : Audit &amp; Notifications (DFD §6.3)
///   - Shared reference tables : Crop, Region, User
///   - Component D             : Market Price Analytics &amp; Reporting (FR15–FR18)
/// </summary>
public class AgriConnectDbContext : DbContext
{
    public AgriConnectDbContext(DbContextOptions<AgriConnectDbContext> options)
        : base(options)
    {
    }

    // ---- Component B — Order & Collection-Centre Logistics -------------------
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<PickupSchedule> PickupSchedules => Set<PickupSchedule>();
    public DbSet<CollectionCentre> CollectionCentres => Set<CollectionCentre>();

    // ---- Shared / Cross-Cutting — Audit & Notifications (DFD §6.3) -----------
    // Not owned by any single component; Component B is the first to need them
    // (plan §12), so this is the minimal shared shape, built and announced here
    // rather than folded into "Component B". Other components should reuse these
    // tables rather than building their own — see PROGRESS.md's Phase 11 entry.
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

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
        ConfigureComponentB(modelBuilder);
        ConfigureShared(modelBuilder);
        ConfigureSharedTables(modelBuilder);
        ConfigureComponentD(modelBuilder);
    }

    /// <summary>
    /// Component B schema, mapped to match the design spec (DFD 6.3) rather than
    /// EF defaults.
    ///
    /// Note on foreign keys: Order.ListingId, Order.BuyerId and
    /// CollectionCentre.RegionId reference tables owned by other components
    /// (Listing from Component A, User from shared auth, Region from Component A)
    /// which do not exist yet. They are mapped here as indexed Guid columns
    /// without FK constraints so this migration applies standalone. A follow-up
    /// migration adds the real constraints once those tables land — see the
    /// logistics plan, §4.3.
    /// </summary>
    private static void ConfigureComponentB(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Order", t =>
            {
                t.HasCheckConstraint("CK_Order_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_Order_Status",
                    "\"Status\" IN ('Pending','Approved','Scheduled','Completed','Cancelled')");
                t.HasCheckConstraint(
                    "CK_Order_DeliveryPreference",
                    "\"DeliveryPreference\" IN ('Pickup','Delivery')");
            });

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Quantity).HasColumnType("numeric(10,2)");

            // Stored as text, not an int, so the CHECK constraints above are
            // readable in the database and survive enum reordering in C#.
            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(e => e.DeliveryPreference)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.ListingId).HasDatabaseName("IX_Order_ListingId");
            entity.HasIndex(e => e.BuyerId).HasDatabaseName("IX_Order_BuyerId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Order_Status");

            entity.HasOne(e => e.StockReservation)
                  .WithOne(r => r.Order)
                  .HasForeignKey<StockReservation>(r => r.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PickupSchedule)
                  .WithOne(p => p.Order)
                  .HasForeignKey<PickupSchedule>(p => p.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("StockReservation", t =>
                t.HasCheckConstraint("CK_StockReservation_ReservedQuantity", "\"ReservedQuantity\" > 0"));

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ReservedQuantity).HasColumnType("numeric(10,2)");
            entity.Property(e => e.ExpiresAt).IsRequired();

            entity.HasIndex(e => e.ListingId).HasDatabaseName("IX_StockReservation_ListingId");
            entity.HasIndex(e => e.OrderId).IsUnique().HasDatabaseName("IX_StockReservation_OrderId");

            // Read by the expiry sweep (FR9): finds reservations whose window has
            // lapsed without scanning the whole table.
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_StockReservation_ExpiresAt");
        });

        modelBuilder.Entity<PickupSchedule>(entity =>
        {
            entity.ToTable("PickupSchedule", t =>
            {
                t.HasCheckConstraint("CK_PickupSchedule_SlotWindow", "\"SlotEnd\" > \"SlotStart\"");
                t.HasCheckConstraint(
                    "CK_PickupSchedule_Status",
                    "\"Status\" IN ('Proposed','Confirmed','Cancelled')");
            });

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(e => e.SlotStart).IsRequired();
            entity.Property(e => e.SlotEnd).IsRequired();

            entity.HasIndex(e => e.OrderId).IsUnique().HasDatabaseName("IX_PickupSchedule_OrderId");

            entity.HasOne(e => e.CollectionCentre)
                  .WithMany(c => c.PickupSchedules)
                  .HasForeignKey(e => e.CollectionCentreId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Prevents double-booking a centre's capacity window at the DB level —
            // the service layer also rejects overlapping Confirmed bookings before
            // ever reaching this constraint (defense in depth, plan §4.1/§8.3).
            entity.HasIndex(e => new { e.CollectionCentreId, e.SlotStart, e.SlotEnd })
                  .HasDatabaseName("IX_PickupSchedule_Centre_Slot_Confirmed")
                  .HasFilter("\"Status\" = 'Confirmed'")
                  .IsUnique();
        });

        modelBuilder.Entity<CollectionCentre>(entity =>
        {
            entity.ToTable("CollectionCentre", t =>
                t.HasCheckConstraint("CK_CollectionCentre_Capacity", "\"Capacity\" > 0"));

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Latitude).HasColumnType("numeric(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("numeric(9,6)");

            entity.HasIndex(e => e.RegionId).HasDatabaseName("IX_CollectionCentre_RegionId");
        });
    }

    /// <summary>
    /// Shared/cross-cutting schema (DFD §6.3), not owned by any one component.
    /// No CHECK constraint on Action/Type/EntityType: other components will write
    /// their own values into these shared tables, so enumerating only Component
    /// B's values here would incorrectly reject their writes.
    /// </summary>
    private static void ConfigureShared(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Details).HasColumnType("jsonb");
            entity.Property(e => e.Timestamp).IsRequired();

            entity.HasIndex(e => e.ActorId).HasDatabaseName("IX_AuditLog_ActorId");
            entity.HasIndex(e => e.EntityId).HasDatabaseName("IX_AuditLog_EntityId");
            entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_AuditLog_Timestamp");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Notification_UserId");
        });
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
            entity.Property(e => e.ListingPrice).HasColumnType("numeric(12,2)");

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
            entity.HasIndex(e => e.CropId)
                  .HasDatabaseName("IX_PriceAnomalyFlag_CropId");
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
