using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AgriConnect.Api.Services;

public class AuthService
{
    private readonly AgriConnectDbContext _db;
    private readonly IConfiguration _config;

    // PBKDF2 parameters. Iteration count follows current OWASP guidance for
    // PBKDF2-HMAC-SHA256 (>= 600,000); salt/hash sizes are HMAC-SHA256-appropriate.
    private const int Pbkdf2IterationCount = 600_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    public AuthService(AgriConnectDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive)
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
            CollectionCentreId = user.CollectionCentreId,
            Token = GenerateToken(user)
        };
    }

    public async Task<AuthResponseDto> Register(RegisterDto dto)
    {
        // Validate role — self-registration is Farmer/Buyer only (FR1: "Officer
        // and Administrator accounts shall be created only by an Administrator").
        // This previously accepted "Administrator" too, letting anyone create a
        // full admin account with no gate at all — found during a full-system
        // integration audit (2026-09-27) and confirmed exploitable via a plain,
        // unauthenticated POST to this endpoint. See PROGRESS.md.
        var validRoles = new[] { Roles.Farmer, Roles.Buyer };
        if (!validRoles.Contains(dto.Role))
            throw new ArgumentException("Invalid role. Self-registration is only available for Farmer or Buyer accounts.");

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
            CreatedAt = DateTimeOffset.UtcNow,
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
            CollectionCentreId = user.CollectionCentreId,
            CollectionCentreName = user.CollectionCentreId == null
                ? null
                : await _db.CollectionCentres.Where(c => c.Id == user.CollectionCentreId)
                    .Select(c => c.Name).FirstOrDefaultAsync(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<(IReadOnlyList<AdminUserResponse> Items, int Total)> ListUsersAsync(string? search, string? role, int page, int size)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
        var total = await query.CountAsync();
        var users = await query.OrderBy(u => u.FullName).Skip((page - 1) * size).Take(size).ToListAsync();
        return (users.Select(ToAdminUser).ToList(), total);
    }

    public async Task<AdminUserResponse> CreateManagedUserAsync(CreateManagedUserRequest dto)
    {
        ValidateManagedRole(dto.Role);
        ValidatePasswordStrength(dto.Password);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            throw new InvalidOperationException("An account with this email already exists.");

        var (centreId, region) = await ResolveCentreAsync(dto.Role, dto.CollectionCentreId);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = HashPassword(dto.Password),
            Role = dto.Role,
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Region = string.IsNullOrWhiteSpace(dto.Region) ? region : dto.Region.Trim(),
            CollectionCentreId = centreId,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        _db.Users.Add(user); await _db.SaveChangesAsync(); return ToAdminUser(user);
    }

    public async Task<AdminUserResponse?> ChangeRoleAsync(Guid id, string role, Guid? collectionCentreId = null)
    {
        ValidateManagedRole(role);
        var user = await _db.Users.FindAsync(id); if (user == null) return null;

        // Becoming an Officer needs a centre (existing one or the one supplied); becoming an
        // Administrator drops the binding.
        var (centreId, _) = await ResolveCentreAsync(role, collectionCentreId ?? user.CollectionCentreId);
        user.Role = role; user.CollectionCentreId = centreId;
        await _db.SaveChangesAsync(); return ToAdminUser(user);
    }

    public async Task<AdminUserResponse?> ChangeStatusAsync(Guid id, bool active, Guid? actorId = null)
    {
        // An administrator who deactivates their own account locks themselves out.
        if (!active && actorId == id)
            throw new InvalidOperationException("You cannot deactivate your own account.");
        var user = await _db.Users.FindAsync(id); if (user == null) return null;
        user.IsActive = active; await _db.SaveChangesAsync(); return ToAdminUser(user);
    }

    public async Task<bool> ResetCredentialsAsync(Guid id, string password)
    {
        ValidatePasswordStrength(password);
        var user = await _db.Users.FindAsync(id); if (user == null) return false;
        user.PasswordHash = HashPassword(password); await _db.SaveChangesAsync(); return true;
    }

    /// <summary>Officers must belong to a collection centre; Administrators never do.</summary>
    private async Task<(Guid? CentreId, string? Region)> ResolveCentreAsync(string role, Guid? centreId)
    {
        if (role != Roles.Officer) return (null, null);
        if (centreId is null)
            throw new ArgumentException("An Officer must be assigned to a collection centre.");
        var centre = await _db.CollectionCentres.AsNoTracking()
            .Where(c => c.Id == centreId)
            .Select(c => new { c.Id, RegionName = _db.Regions.Where(r => r.Id == c.RegionId).Select(r => r.Name).FirstOrDefault() })
            .FirstOrDefaultAsync()
            ?? throw new ArgumentException("The selected collection centre does not exist.");
        return (centre.Id, centre.RegionName);
    }

    private static void ValidatePasswordStrength(string password)
    {
        // Staff accounts get a stricter rule than the self-registration minimum.
        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new ArgumentException("The password must be at least 8 characters and contain a letter and a digit.");
    }

    private static void ValidateManagedRole(string role)
    {
        if (role is not (Roles.Officer or Roles.Admin)) throw new ArgumentException("Administrators may create or assign only Officer or Administrator roles.");
    }

    private static AdminUserResponse ToAdminUser(User u) => new(u.Id, u.FullName, u.Email, u.Role, u.Phone, u.Region, u.IsActive, u.CreatedAt, u.CollectionCentreId);

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
