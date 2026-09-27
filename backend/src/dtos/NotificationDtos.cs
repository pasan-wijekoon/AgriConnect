namespace AgriConnect.Api.Dtos;

public record NotificationResponse(
    Guid Id,
    string Type,
    string? Title,
    string Message,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);
