using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services.Reports;

/// <summary>
/// Generates summary reports as CSV files under wwwroot/reports and keeps an audit row
/// for each one (FR18).
/// </summary>
public class ReportExportService(AgriConnectDbContext db, IWebHostEnvironment environment)
{
    public const string ReportsFolder = "reports";

    public static readonly IReadOnlyList<string> ValidTypes = ["PriceTrends", "Listings", "Orders"];

    public async Task<ReportExport> GenerateAsync(string type, DateOnly from, DateOnly to, Guid requestedBy)
    {
        var canonicalType = ValidTypes.FirstOrDefault(t => t.Equals(type, StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException(
                $"Unknown report type '{type}'. Expected one of: {string.Join(", ", ValidTypes)}.");
        if (from > to)
        {
            throw new ValidationException("'dateRangeStart' must be earlier than or equal to 'dateRangeEnd'.");
        }

        var csv = canonicalType switch
        {
            "PriceTrends" => await BuildPriceTrendsCsvAsync(from, to),
            "Listings" => PlaceholderCsv("ListingId,CropId,RegionId,Price,Status,CreatedAt", "Component A"),
            _ => PlaceholderCsv("OrderId,ListingId,Quantity,Status,CreatedAt", "Component B"),
        };

        var fileName = $"{Guid.NewGuid()}.csv";
        var folder = Path.Combine(WebRoot, ReportsFolder);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);
        // UTF-8 with BOM so Excel opens names correctly.
        await File.WriteAllTextAsync(path, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var report = new ReportExport
        {
            Id = Guid.NewGuid(),
            RequestedBy = requestedBy,
            Type = canonicalType,
            DateRangeStart = from,
            DateRangeEnd = to,
            GeneratedAt = DateTimeOffset.UtcNow,
            FileUrl = $"/{ReportsFolder}/{fileName}",
        };

        try
        {
            db.ReportExports.Add(report);
            await db.SaveChangesAsync();
        }
        catch
        {
            // No audit row means nobody can find the file; don't leave it behind.
            File.Delete(path);
            throw;
        }

        return report;
    }

    public Task<ReportExport?> GetByIdAsync(Guid id) =>
        db.ReportExports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    // WebRootPath is null when wwwroot did not exist at startup.
    private string WebRoot => environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");

    private async Task<string> BuildPriceTrendsCsvAsync(DateOnly from, DateOnly to)
    {
        var snapshots = await db.PriceTrendSnapshots
            .AsNoTracking()
            .Where(s => s.Period >= from && s.Period <= to && s.SampleCount > 0)
            .ToListAsync();
        var cropNames = await db.Crops.ToDictionaryAsync(c => c.Id, c => c.Name);
        var regionNames = await db.Regions.ToDictionaryAsync(r => r.Id, r => r.Name);

        var rows = snapshots
            .Select(s => new
            {
                s,
                CropName = cropNames.GetValueOrDefault(s.CropId),
                RegionName = regionNames.GetValueOrDefault(s.RegionId),
            })
            .OrderBy(x => x.CropName)
            .ThenBy(x => x.RegionName)
            .ThenBy(x => x.s.Period);

        var csv = new StringBuilder();
        csv.AppendLine("CropId,CropName,RegionId,RegionName,Period,AvgPrice,MinPrice,MaxPrice,SampleCount");
        foreach (var row in rows)
        {
            var s = row.s;
            csv.AppendLine(string.Join(',',
                s.CropId,
                Escape(row.CropName),
                s.RegionId,
                Escape(row.RegionName),
                s.Period.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                s.AvgPrice.ToString("0.00", CultureInfo.InvariantCulture),
                s.MinPrice.ToString("0.00", CultureInfo.InvariantCulture),
                s.MaxPrice.ToString("0.00", CultureInfo.InvariantCulture),
                s.SampleCount.ToString(CultureInfo.InvariantCulture)));
        }

        return csv.ToString();
    }

    private static string PlaceholderCsv(string header, string owner) =>
        $"# Data available once {owner} entities are integrated.{Environment.NewLine}{header}{Environment.NewLine}";

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
