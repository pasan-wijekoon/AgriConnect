using System.Text;
using backend.Config;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database — PostgreSQL via connection string in appsettings.json
// For quick local testing without a DB server, comment out the Npgsql line
// and uncomment the InMemory line below:
// builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("AgriConnect"));
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// HTTP Client & Agentic AI service
builder.Services.AddHttpClient();
builder.Services.AddScoped<AgenticAiService>();

// Business services
builder.Services.AddScoped<ListingService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TodayPriceCatalogService>();

// CORS — restricted to configured origins only (web dashboard, Flutter dev
// server). AllowAnyOrigin() combined with token-bearing requests is a CSRF/
// credential-forwarding risk, so the allowlist must be explicit.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key is not configured. Set it via appsettings.Development.json, " +
        "an environment variable (Jwt__Key), or `dotnet user-secrets`.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgriConnect",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AgriConnect",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-apply migrations and seed data on startup (dev only)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Could not connect to PostgreSQL database on startup ({Message}). Configure your database credentials in appsettings.Development.json.", ex.Message);
    }
}

app.Run();