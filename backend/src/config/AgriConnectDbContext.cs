using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Config;

/// <summary>
/// EF Core context for AgriConnect.
///
/// Currently holds Component B (Order &amp; Collection-Centre Logistics) only.
/// Other components add their own DbSets and configuration here as they land —
/// keep each component's configuration in its own private method below so the
/// file stays mergeable when several people extend it at once.
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureComponentB(modelBuilder);
        ConfigureShared(modelBuilder);
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
}
