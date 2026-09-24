namespace AgriConnect.Api.Services;

/// <summary>A requested resource does not exist. Controllers map this to 404.</summary>
public class NotFoundException(string message) : Exception(message);
