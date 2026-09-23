using System.ComponentModel.DataAnnotations;

namespace AgriConnect.Api.Models;

/// <summary>
/// Shared User identity and role record.
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
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// One of: "Farmer", "Buyer", "Officer", "Administrator".
    /// Stored as a string — matches the DB CHECK constraint on this column.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
