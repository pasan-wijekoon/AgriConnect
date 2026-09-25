namespace AgriConnect.Api.Dtos;

public record NotificationResponse(
    Guid Id,
    string Type,
    string Message,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);
