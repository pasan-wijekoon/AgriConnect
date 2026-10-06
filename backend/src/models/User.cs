using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Models;

/// <summary>
/// Shared User identity and role record — real auth (Component A), not a placeholder.
///
/// Used across all components for authentication, authorisation, and audit-trail attribution.
/// Roles are stored as strings so the CHECK constraint remains human-readable in the database
/// and survives C# enum reordering without a migration.
///
/// Shared — Component A (Listings/FarmerId), Component B (Orders), Component D (ReportExport.RequestedBy),
/// and the auth middleware all reference this table.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// One of: "Farmer", "Buyer", "Officer", "Administrator" (Component A's own seed data
    /// and register endpoint currently only issue "Farmer"/"Buyer"/"Admin" — "Admin" is
    /// normalized to "Administrator" and "Officer" added at the DB CHECK constraint level
    /// during integration so the same table serves every component's role set).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Officers only: the collection centre this officer works at. When set, the
    /// officer only sees/acts on orders for that centre (its region's listings or
    /// schedules booked there). Null = unscoped (e.g. non-officer roles). No FK
    /// constraint — same no-FK pattern as the other cross-component references.
    /// </summary>
    public Guid? CollectionCentreId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; set; } = true;
}
