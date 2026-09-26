using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class User
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Farmer";
    // Valid: Farmer, Buyer, Admin

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
