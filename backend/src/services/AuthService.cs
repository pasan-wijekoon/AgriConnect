using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using backend.Config;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    // PBKDF2 parameters. Iteration count follows current OWASP guidance for
    // PBKDF2-HMAC-SHA256 (>= 600,000); salt/hash sizes are HMAC-SHA256-appropriate.
    private const int Pbkdf2IterationCount = 600_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Phone = user.Phone,
            Region = user.Region,
            AvatarUrl = user.AvatarUrl,
            Token = GenerateToken(user)
        };
    }

    public async Task<AuthResponseDto> Register(RegisterDto dto)
    {
        // Validate role
        var validRoles = new[] { "Farmer", "Buyer", "Admin" };
        if (!validRoles.Contains(dto.Role))
            throw new ArgumentException("Invalid role. Must be Farmer, Buyer, or Admin.");

        // Check duplicate email
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            Role = dto.Role,
            Phone = dto.Phone,
            Region = dto.Region,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Phone = user.Phone,
            Region = user.Region,
            AvatarUrl = user.AvatarUrl,
            Token = GenerateToken(user)
        };
    }

    public async Task<UserProfileDto?> GetUserById(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Phone = user.Phone,
            Region = user.Region,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Reads the authenticated user's ID and role from the validated JWT
    /// ClaimsPrincipal that ASP.NET Core's authentication middleware attaches
    /// to the request. Returns null if the principal has no valid claims
    /// (i.e. the caller is unauthenticated) — callers must not fall back to a
    /// default identity in that case.
    /// </summary>
    public static (Guid UserId, string Role)? GetUser(ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = principal.FindFirstValue(ClaimTypes.Role);
        if (idClaim == null || roleClaim == null || !Guid.TryParse(idClaim, out var userId))
            return null;

        return (userId, roleClaim);
    }

    private string GenerateToken(User user)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "AgriConnect";
        var audience = _config["Jwt:Audience"] ?? "AgriConnect";
        var expiryHours = double.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 12;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// PBKDF2-HMAC-SHA256 password hashing with a random per-user salt.
    /// Stored format: "{iterations}.{saltBase64}.{hashBase64}" so the iteration
    /// count can be raised in the future without invalidating older hashes.
    /// </summary>
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2IterationCount, HashAlgorithmName.SHA256, HashSizeBytes);
        return $"{Pbkdf2IterationCount}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        byte[] salt, expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
