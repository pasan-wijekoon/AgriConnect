using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

// ── Auth Request DTOs ─────────────────────────────────────────

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(4)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(4), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Farmer";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }
}

// ── Auth Response DTO ─────────────────────────────────────────

public class AuthResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Region { get; set; }
    public string? AvatarUrl { get; set; }
    public string Token { get; set; } = string.Empty; // simple token for now
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Region { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
