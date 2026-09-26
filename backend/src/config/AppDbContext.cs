using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.src.models;

namespace backend.src.config;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<CollectionCentre> CollectionCentres => Set<CollectionCentre>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionPhoto> InspectionPhotos => Set<InspectionPhoto>();
    public DbSet<GradeDiscrepancyFlag> GradeDiscrepancyFlags => Set<GradeDiscrepancyFlag>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();

            entity.HasOne(e => e.Region)
                  .WithMany(r => r.Users)
                  .HasForeignKey(e => e.RegionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Crop
        modelBuilder.Entity<Crop>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
        });

        // Region
        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(80).IsRequired();
        });

        // CollectionCentre
        modelBuilder.Entity<CollectionCentre>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Latitude).HasPrecision(9, 6);
            entity.Property(e => e.Longitude).HasPrecision(9, 6);

            entity.HasOne(e => e.Region)
                  .WithOne(r => r.CollectionCentre)
                  .HasForeignKey<CollectionCentre>(e => e.RegionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Listing
        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(10, 2);
            entity.Property(e => e.MinPrice).HasPrecision(12, 2);
            entity.Property(e => e.Unit).HasMaxLength(10).IsRequired();
            entity.Property(e => e.ClaimedGrade).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();

            entity.HasIndex(e => new { e.CropId, e.RegionId, e.Status, e.ClaimedGrade });
            entity.HasIndex(e => e.FarmerId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Farmer)
                  .WithMany(u => u.Listings)
                  .HasForeignKey(e => e.FarmerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Crop)
                  .WithMany(c => c.Listings)
                  .HasForeignKey(e => e.CropId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Region)
                  .WithMany(r => r.Listings)
                  .HasForeignKey(e => e.RegionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ListingPhoto
        modelBuilder.Entity<ListingPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.Photos)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Inspection
        modelBuilder.Entity<Inspection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfirmedGrade).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.ListingId);
            entity.HasIndex(e => e.OfficerId);

            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.Inspections)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Officer)
                  .WithMany(u => u.Inspections)
                  .HasForeignKey(e => e.OfficerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // InspectionPhoto
        modelBuilder.Entity<InspectionPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.Inspection)
                  .WithMany(i => i.Photos)
                  .HasForeignKey(e => e.InspectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // GradeDiscrepancyFlag
        modelBuilder.Entity<GradeDiscrepancyFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClaimedGrade).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ConfirmedGrade).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.ListingId);

            entity.HasOne(e => e.Listing)
                  .WithMany(l => l.GradeDiscrepancyFlags)
                  .HasForeignKey(e => e.ListingId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResolvedByOfficer)
                  .WithMany()
                  .HasForeignKey(e => e.ResolvedByOfficerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.Actor)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.ActorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(20).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Property("CreatedAt") != null && (DateTime)entry.Property("CreatedAt").CurrentValue! == default)
                {
                    entry.Property("CreatedAt").CurrentValue = now;
                }
                if (entry.Property("UpdatedAt") != null && (DateTime)entry.Property("UpdatedAt").CurrentValue! == default)
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Property("UpdatedAt") != null)
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
