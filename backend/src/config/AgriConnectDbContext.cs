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
///   - Shared reference tables : Crop, Region, User (real auth — Component A)
///   - Component D             : Market Price Analytics &amp; Reporting (FR15–FR18)
///   - Component A             : Produce Listings &amp; Price Discovery (FR1–FR4, FR6, FR7)
///   - Component C             : Quality Grading &amp; Inspection (FR5, FR12–FR14)
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

    // ---- Component A — Produce Listings & Price Discovery --------------------
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<PriceSuggestion> PriceSuggestions => Set<PriceSuggestion>();
    public DbSet<TodayPriceCatalogItem> TodayPriceCatalogItems => Set<TodayPriceCatalogItem>();

    // ---- Component C — Quality Grading & Inspection (FR12–FR14) --------------
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionPhoto> InspectionPhotos => Set<InspectionPhoto>();
    public DbSet<GradeDiscrepancyFlag> GradeDiscrepancyFlags => Set<GradeDiscrepancyFlag>();
    public DbSet<AgentWorkflow> AgentWorkflows => Set<AgentWorkflow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureComponentB(modelBuilder);
        ConfigureShared(modelBuilder);
        ConfigureSharedTables(modelBuilder);
        ConfigureComponentD(modelBuilder);
        ConfigureComponentA(modelBuilder);
        ConfigureComponentC(modelBuilder);
        SeedComponentAReferenceData(modelBuilder);
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
            entity.Property(e => e.Title).HasMaxLength(150);
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
            entity.Property(e => e.Category).HasMaxLength(50);
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
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);

            entity.HasIndex(e => e.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_User_Email");
        });
    }

    /// <summary>
    /// Component A schema (Produce Listings &amp; Price Discovery, FR1–FR4/FR6/FR7),
    /// mapped exactly as it was on their own branch — this is a real, complete feature
    /// (listings, photos, AI price suggestions, the admin-curated Today's Prices catalog),
    /// just moved onto the shared AgriConnectDbContext during integration rather than
    /// their own separate AppDbContext.
    /// </summary>
    private static void ConfigureComponentA(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.PickupWindowStart).IsRequired();
            entity.Property(e => e.PickupWindowEnd).IsRequired();

            // Composite index for search/filter/sort (FR6)
            entity.HasIndex(e => new { e.CropId, e.RegionId, e.Status, e.ClaimedGrade });
            entity.HasIndex(e => e.FarmerId);

            entity.HasMany(e => e.Photos)
                  .WithOne(p => p.Listing)
                  .HasForeignKey(p => p.ListingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PriceSuggestion)
                  .WithOne(ps => ps.Listing)
                  .HasForeignKey<PriceSuggestion>(ps => ps.ListingId);

            entity.HasOne(e => e.Crop)
                  .WithMany()
                  .HasForeignKey(e => e.CropId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Region)
                  .WithMany()
                  .HasForeignKey(e => e.RegionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ListingPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Allow unlimited-length URLs (base64 data URIs can be very large)
            entity.Property(e => e.Url).HasColumnType("text");
        });

        modelBuilder.Entity<PriceSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ListingId).IsUnique(); // 1-to-1
        });

        modelBuilder.Entity<TodayPriceCatalogItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DisplayOrder);
        });
    }

    /// <summary>
    /// Component C schema (Quality Grading &amp; Inspection, FR12–FR14, plus the
    /// FR5 quality-gated publish workflow), folded in from their own separate
    /// AppDbContext during integration (2026-09-27) the same way Component A's
    /// AppDbContext was — see PROGRESS.md. Listing/User/Crop/Region are the
    /// already-shared tables above; only the tables genuinely new to this
    /// component (Inspection/InspectionPhoto/GradeDiscrepancyFlag/AgentWorkflow)
    /// are configured here.
    /// </summary>
    private static void ConfigureComponentC(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasOne(e => e.Farmer)
                  .WithMany()
                  .HasForeignKey(e => e.FarmerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Inspection>(entity =>
        {
            entity.ToTable("Inspection");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfirmedGrade).HasMaxLength(20).IsRequired();

            entity.HasIndex(e => e.ListingId).HasDatabaseName("IX_Inspection_ListingId");
            entity.HasIndex(e => e.OfficerId).HasDatabaseName("IX_Inspection_OfficerId");

            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.Inspections)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Officer)
                  .WithMany()
                  .HasForeignKey(e => e.OfficerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InspectionPhoto>(entity =>
        {
            entity.ToTable("InspectionPhoto");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).HasColumnType("text");

            entity.HasOne(e => e.Inspection)
                  .WithMany(i => i.Photos)
                  .HasForeignKey(e => e.InspectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GradeDiscrepancyFlag>(entity =>
        {
            entity.ToTable("GradeDiscrepancyFlag");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClaimedGrade).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ConfirmedGrade).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.ListingId).HasDatabaseName("IX_GradeDiscrepancyFlag_ListingId");

            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.GradeDiscrepancyFlags)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Inspection)
                  .WithMany()
                  .HasForeignKey(e => e.InspectionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResolvedByOfficer)
                  .WithMany()
                  .HasForeignKey(e => e.ResolvedByOfficerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentWorkflow>(entity =>
        {
            entity.ToTable("AgentWorkflow", t =>
                t.HasCheckConstraint(
                    "CK_AgentWorkflow_ApprovalStatus",
                    "\"ApprovalStatus\" IN ('Pending','Approved','Rejected','RevisionRequested')"));

            entity.HasKey(e => e.Id);
            entity.Property(e => e.TriggerType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ObjectiveText).IsRequired();
            entity.Property(e => e.PlanSteps).HasColumnType("jsonb");
            entity.Property(e => e.ToolCallLog).HasColumnType("jsonb");
            entity.Property(e => e.ValidationResult).HasColumnType("jsonb");
            entity.Property(e => e.ApprovalStatus).HasMaxLength(30).IsRequired();

            entity.HasIndex(e => e.TriggerEntityId).HasDatabaseName("IX_AgentWorkflow_TriggerEntityId");
            entity.HasIndex(e => e.ApprovalStatus).HasDatabaseName("IX_AgentWorkflow_ApprovalStatus");

            entity.HasOne(e => e.ApprovedByOfficer)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovedByOfficerId)
                  .OnDelete(DeleteBehavior.SetNull);
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

    /// <summary>
    /// Component A's original demo dataset (moved here verbatim from their own
    /// AppDbContext's SeedData, migration-time HasData seeding — a different
    /// convention from the runtime DataSeeder/SharedReferenceSeeder classes other
    /// components use, kept as-is since HasData is what their migration will encode).
    ///
    /// Two changes from their original data, both integration-driven:
    ///  - Every "Admin" role string is now "Administrator" (matches the CK_User_Role
    ///    constraint and every [Authorize(Roles = "Administrator")] attribute already
    ///    in this codebase — Component A's own code used "Admin" consistently, so this
    ///    was a latent bug that only real Administrator-gated endpoints would expose).
    ///  - One real Officer demo account was added (not in Component A's original list,
    ///    which only covered Farmer/Buyer/Admin) — Component B's and Component D's
    ///    Officer-gated endpoints had nothing to log in as with real auth otherwise.
    /// </summary>
    private static void SeedComponentAReferenceData(ModelBuilder modelBuilder)
    {
        // EF Core's HasData() snapshots whatever CreatedAt evaluates to at migration-generation
        // time; Crop/Region's model default (= DateTimeOffset.UtcNow) is non-deterministic across
        // builds, which trips PendingModelChangesWarning on every future `dotnet ef` command unless
        // every seeded row sets a fixed value explicitly here.
        var seedTimestamp = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var crops = new[]
        {
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "Rice", Category = "Grains", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), Name = "Tea", Category = "Beverages", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000003"), Name = "Coconut", Category = "Fruits", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000004"), Name = "Tomatoes", Category = "Vegetables", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000005"), Name = "Carrots", Category = "Vegetables", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000006"), Name = "Chili", Category = "Spices", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000007"), Name = "Onions", Category = "Vegetables", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000008"), Name = "Potatoes", Category = "Vegetables", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-000000000009"), Name = "Cinnamon", Category = "Spices", CreatedAt = seedTimestamp },
            new Crop { Id = Guid.Parse("a1000000-0000-0000-0000-00000000000a"), Name = "Banana", Category = "Fruits", CreatedAt = seedTimestamp },
        };
        modelBuilder.Entity<Crop>().HasData(crops);

        var regions = new[]
        {
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000001"), Name = "Colombo", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000002"), Name = "Kandy", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000003"), Name = "Galle", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000004"), Name = "Jaffna", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000005"), Name = "Anuradhapura", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000006"), Name = "Matara", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000007"), Name = "Kurunegala", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000008"), Name = "Nuwara Eliya", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000009"), Name = "Gampaha", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000a"), Name = "Kalutara", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000b"), Name = "Matale", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000c"), Name = "Ratnapura", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000d"), Name = "Kegalle", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000e"), Name = "Badulla", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-00000000000f"), Name = "Monaragala", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000010"), Name = "Hambantota", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000011"), Name = "Trincomalee", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000012"), Name = "Batticaloa", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000013"), Name = "Ampara", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000014"), Name = "Puttalam", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000015"), Name = "Polonnaruwa", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000016"), Name = "Kilinochchi", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000017"), Name = "Mannar", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000018"), Name = "Mullaitivu", CreatedAt = seedTimestamp },
            new Region { Id = Guid.Parse("b1000000-0000-0000-0000-000000000019"), Name = "Vavuniya", CreatedAt = seedTimestamp },
        };
        modelBuilder.Entity<Region>().HasData(regions);

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
                CreatedAt = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
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
                CreatedAt = new DateTimeOffset(2024, 2, 10, 0, 0, 0, TimeSpan.Zero),
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
                CreatedAt = new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero),
                IsActive = true
            },
            new User
            {
                Id = Guid.Parse("f0000000-0000-0000-0000-000000000099"),
                FullName = "N. Perera",
                Email = "admin@agriconnect.lk",
                PasswordHash = passwordHash,
                Role = "Administrator",
                Phone = "+94112345678",
                Region = "Nuwara Eliya",
                AvatarUrl = null,
                CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsActive = true
            },
            new User
            {
                // Added during integration (2026-09-27) — Component A's original seed
                // only covered Farmer/Buyer/Admin; Component B/D's Officer-gated
                // endpoints need a real account to log in as now that dev-auth is retired.
                Id = Guid.Parse("f0000000-0000-0000-0000-000000000050"),
                FullName = "Officer Demo",
                Email = "officer@agriconnect.lk",
                PasswordHash = passwordHash,
                Role = "Officer",
                Phone = "+94711234567",
                Region = "Kandy",
                AvatarUrl = null,
                CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsActive = true
            },
        };
        modelBuilder.Entity<User>().HasData(users);

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
        modelBuilder.Entity<TodayPriceCatalogItem>().HasData(todayPriceCatalog);
    }
}
