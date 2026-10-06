namespace AgriConnect.Api.Services.Analytics;

internal static class EnumInput
{
    /// <summary>
    /// Parses an enum by name only (case-insensitive). Enum.TryParse alone would also
    /// accept numbers, letting "1" through as a valid status.
    /// </summary>
    public static T Parse<T>(string value, string field) where T : struct, Enum
    {
        var name = Enum.GetNames<T>().FirstOrDefault(n => n.Equals(value, StringComparison.OrdinalIgnoreCase));
        return name is not null
            ? Enum.Parse<T>(name)
            : throw new ArgumentException(
                $"Unknown {field} '{value}'. Expected one of: {string.Join(", ", Enum.GetNames<T>())}.");
    }
}
