namespace AgriConnect.Api.Dtos.Auth;

public class TokenResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public string Role { get; set; } = string.Empty;
}
