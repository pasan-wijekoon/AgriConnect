using AgriConnect.Api.Config;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services.Analytics;

/// <summary>Recurring shortage and oversupply events per crop and region (FR17).</summary>
public class ShortageDetectionService(AgriConnectDbContext db)
{
    public async Task<List<ShortageOversupplyEvent>> GetEventsAsync(
        Guid? cropId, Guid? regionId, string? type, string? severity)
    {
        var query = db.ShortageOversupplyEvents.AsNoTracking();

        if (cropId is { } crop)
        {
            query = query.Where(e => e.CropId == crop);
        }
        if (regionId is { } region)
        {
            query = query.Where(e => e.RegionId == region);
        }
        if (type is not null)
        {
            var parsed = EnumInput.Parse<SupplyEventType>(type, "type");
            query = query.Where(e => e.Type == parsed);
        }
        if (severity is not null)
        {
            var parsed = EnumInput.Parse<SupplyEventSeverity>(severity, "severity");
            query = query.Where(e => e.Severity == parsed);
        }

        return await query.OrderByDescending(e => e.DetectedAt).ToListAsync();
    }
}
