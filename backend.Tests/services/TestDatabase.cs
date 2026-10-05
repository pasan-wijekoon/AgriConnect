using Microsoft.Extensions.Configuration;

namespace backend.Tests.services;

/// <summary>
/// Single source of the Postgres connection string for the tests that talk to a real
/// database directly (the integration suite gets the same value through
/// WebApplicationFactory). Resolution order matches the API host: environment variable
/// ConnectionStrings__Default, then the backend's user-secrets, then appsettings.json.
/// </summary>
internal static class TestDatabase
{
    private const string BackendUserSecretsId = "agriconnect-backend-orders-component";

    public static string ConnectionString { get; } = Resolve();

    private static string Resolve()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=localhost;Database=agriconnect;Username=postgres;Password=postgres"
            })
            .AddUserSecrets(BackendUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        return config.GetConnectionString("Default")!;
    }
}
