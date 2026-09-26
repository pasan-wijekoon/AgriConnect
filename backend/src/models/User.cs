using System;
using System.Collections.Generic;

namespace backend.src.models;

public enum UserRole
{
    Farmer,
    Buyer,
    Officer,
    Administrator
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = UserRole.Farmer.ToString();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? RegionId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Region? Region { get; set; }
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
