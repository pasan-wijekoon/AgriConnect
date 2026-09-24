using System.Text.Json.Serialization;
using AgriConnect.Api.Config;
using AgriConnect.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Enums serialize/deserialize as their string names (e.g. "Pickup", not 0), matching
// the enum-as-string convention already used for the DB layer (plan §0.2).
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// PostgreSQL via EF Core.
builder.Services.AddDbContext<AgriConnectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---- Component B — Dev auth seam (plan §6) ----
// No shared User/Auth/JWT implementation exists yet anywhere in the repo, so this
// scheme reads X-Dev-Role/X-Dev-UserId headers into a ClaimsPrincipal. Switch
// Auth:Mode to "Jwt" and register real bearer validation here once shared auth
// lands — controllers never change, since they only read User.FindFirst(...).
var authMode = builder.Configuration["Auth:Mode"] ?? "Dev";
switch (authMode)
{
    case "Dev":
        builder.Services
            .AddAuthentication(DevAuthDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(DevAuthDefaults.Scheme, options => { });
        break;
    default:
        throw new NotSupportedException(
            $"Auth:Mode '{authMode}' is not supported yet. Only 'Dev' is implemented until shared JWT auth lands (plan §6).");
}
builder.Services.AddAuthorization();

// ---- Component B — Listing availability seam (plan §3) ----
// Swap for a real Component-A-backed implementation once the Listing table lands.
builder.Services.AddScoped<IListingAvailabilityPort, FixtureListingAvailabilityPort>();

// ---- Component B — Order services (FR8/FR9/FR11) ----
builder.Services.AddScoped<IStockReservationService, StockReservationService>();
builder.Services.AddScoped<OrderService>();

// Swashbuckle (classic Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || args.Contains("--seed") || args.Contains("--seed-only"))
{
    // ---- Component B — Seed collection centres & demo order fixtures ----
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var result = await DataSeeder.SeedAsync(db, logger);
        Console.WriteLine($"[Component B] Seeded: {result.CollectionCentresAdded} collection centres, {result.OrdersAdded} orders, {result.ReservationsAdded} reservations, {result.SchedulesAdded} schedules.");
    }
}

// Support running with --verify-seed to inspect database counts and sample values
if (args.Contains("--verify-seed"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AgriConnectDbContext>();
        var centreCount = await db.CollectionCentres.CountAsync();
        var orderCount = await db.Orders.CountAsync();
        var reservationCount = await db.StockReservations.CountAsync();
        var scheduleCount = await db.PickupSchedules.CountAsync();
        Console.WriteLine($"[VERIFY] CollectionCentres: {centreCount}");
        Console.WriteLine($"[VERIFY] Orders: {orderCount}");
        Console.WriteLine($"[VERIFY] StockReservations: {reservationCount}");
        Console.WriteLine($"[VERIFY] PickupSchedules: {scheduleCount}");

        var sampleOrder = await db.Orders.FirstAsync();
        Console.WriteLine($"[VERIFY] Sample Order: Status={sampleOrder.Status}, Quantity={sampleOrder.Quantity}, DeliveryPreference={sampleOrder.DeliveryPreference}");
    }
    return;
}

// Support running with --seed-only to seed and terminate cleanly (for CI/scripts)
if (args.Contains("--seed-only"))
{
    Console.WriteLine("Seeding completed. Exiting (--seed-only flag specified).");
    return;
}

app.MapControllers();
app.Run();
