using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using backend.src.config;
using backend.src.services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Configure Database Connection (PostgreSQL with SQLite fallback for seamless standalone execution)
var connectionString = builder.Configuration.GetConnectionString("Default");
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite", false);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlite || string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlite("Data Source=agriconnect.db");
    }
    else
    {
        try
        {
            options.UseNpgsql(connectionString);
        }
        catch
        {
            options.UseSqlite("Data Source=agriconnect.db");
        }
    }
});

// Register Domain Services
builder.Services.AddScoped<IInspectionService, InspectionService>();

// CORS policy for React Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto seed demo data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database seeding note: {ex.Message}");
    }
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();