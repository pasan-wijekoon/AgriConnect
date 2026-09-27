# AgriConnect — Backend API

ASP.NET Core Web API (.NET 10). This is the **only** layer with database write access, and the single source of truth for identity, authorization, validation, and persistence.

Both clients (Flutter and React) talk only to this API. See [`../documentation/AgriConnect_DFD.md`](../documentation/AgriConnect_DFD.md) §2.5 for the full interaction rules.

---

## Prerequisites

| Tool | Version | Check with |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` |
| PostgreSQL | 17 | `psql --version` |

PostgreSQL can be either a local install or the Docker container from [`../docker/`](../docker/).

---

## Setup

Restore packages and the local `dotnet-ef` tool:

```powershell
dotnet restore
```

```powershell
dotnet tool restore
```

`dotnet tool restore` is required before any `dotnet ef` command — the EF CLI is pinned per-project in [`dotnet-tools.json`](dotnet-tools.json), not installed globally. This keeps the whole team on the same EF version.

Create the database schema:

```powershell
dotnet ef database update
```

Run the API:

```powershell
dotnet run
```

Swagger UI is available at `/swagger` once the app is running.

---

## Configuration

The connection string lives in [`appsettings.json`](appsettings.json) under `ConnectionStrings:Default`. The committed default targets a local PostgreSQL:

```
Host=localhost;Database=agriconnect;Username=postgres;Password=postgres
```

Override it locally in `appsettings.Development.json` rather than editing the committed file — that avoids everyone fighting over the same line in git.

> **Note:** these are development-only credentials. Production credentials come from environment variables, never from a committed file.

---

## Project Structure

```
backend/
├── Program.cs              # Entry point, DI registration, middleware pipeline
├── appsettings.json        # Configuration (connection string, logging)
├── backend.csproj          # Package references
├── dotnet-tools.json       # Pinned dotnet-ef version
└── src/
    ├── config/             # DbContext and EF Fluent API configuration
    ├── controllers/        # HTTP endpoints — thin, no business logic
    ├── services/           # Business logic
    ├── models/             # EF entities (database shape)
    ├── dtos/               # Request/response shapes (API shape)
    └── migrations/         # EF Core migrations
```

**Layering rule:** controllers call services, services call the DbContext. Never return an EF entity directly from a controller — map it to a DTO first. The entity is the database's shape; the DTO is the API's contract, and they change for different reasons.

---

## Working with Migrations

Add a migration after changing an entity or the DbContext:

```powershell
dotnet ef migrations add YourMigrationName
```

Apply pending migrations:

```powershell
dotnet ef database update
```

List migrations and their applied status:

```powershell
dotnet ef migrations list
```

Roll back to a previous migration:

```powershell
dotnet ef database update PreviousMigrationName
```

### Migration rules for the team

Migrations are the easiest place for four people to break each other's work. Three rules:

1. **Never edit a migration that is already pushed.** Others may have applied it. Add a new one instead.
2. **Name migrations after what they do**, scoped to your component — `AddComponentDAnalyticsTables`, not `Update1`.
3. **Pull and re-run `database update` before adding a migration.** EF generates a diff against the current model snapshot; building on a stale snapshot produces a broken migration.

See [`../CONTRIBUTING.md`](../CONTRIBUTING.md) for how migration ownership is split across components.

---

## DbContext Ownership

[`src/config/AgriConnectDbContext.cs`](src/config/AgriConnectDbContext.cs) is shared by all four components. To keep it mergeable when several people extend it at once, each component's Fluent API configuration lives in its **own private method**:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    ConfigureComponentD(modelBuilder);
    // Each component adds one line here and one method below.
}
```

Add your `DbSet` properties under your component's comment block, and your configuration in a matching `ConfigureComponentX` method. Two people editing different methods merges cleanly; two people editing one giant method does not.

---

## Current Status

**Component D — Market Price Analytics & Reporting** is the only component with schema in place:

| Table | Purpose |
|---|---|
| `PriceTrendSnapshot` | Materialized price aggregates per crop/region/period (FR15) |
| `PriceAnomalyFlag` | Listings deviating from the AI-suggested price (FR16) |
| `ShortageOversupplyEvent` | Detected supply imbalances (FR17) |
| `ReportExport` | Audit record of generated reports (FR18) |

### Foreign keys to other components

`CropId`, `RegionId`, `ListingId`, and `RequestedBy` reference tables owned by other components that **do not exist yet**. They are currently mapped as indexed `Guid` columns **without FK constraints**, so the migration applies standalone.

A follow-up migration adds the real constraints once `Crop`, `Region`, `Listing`, and `User` land. If you are the one landing those tables, coordinate before adding constraints — see the note in `AgriConnectDbContext.ConfigureComponentD`.

---

## Testing the API

[`backend.http`](backend.http) holds sample requests, runnable directly from VS Code (REST Client extension) or Visual Studio. Add your endpoints there as you build them — it is the fastest way for a teammate to try your component without reading your controller.
