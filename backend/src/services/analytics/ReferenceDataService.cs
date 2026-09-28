using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos.Analytics;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services.Analytics;

/// <summary>Crop and region names for the analytics filters, so clients never show raw GUIDs.</summary>
public class ReferenceDataService(AgriConnectDbContext db)
{
    public async Task<AnalyticsFiltersDto> GetFiltersAsync()
    {
        var cropsWithPrices = await db.PriceTrendSnapshots.AsNoTracking()
            .Where(s => s.SampleCount > 0)
            .Select(s => s.CropId)
            .Distinct()
            .ToListAsync();

        return new AnalyticsFiltersDto
        {
            Crops = await db.Crops.AsNoTracking().OrderBy(c => c.Name)
                .Select(c => new CropFilterDto { Id = c.Id, Name = c.Name, HasPriceHistory = cropsWithPrices.Contains(c.Id) })
                .ToListAsync(),
            Regions = await db.Regions.AsNoTracking().OrderBy(r => r.Name)
                .Select(r => new NamedItemDto { Id = r.Id, Name = r.Name }).ToListAsync(),
        };
    }
}
