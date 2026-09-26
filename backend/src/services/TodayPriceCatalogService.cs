using backend.Config;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// Admin CRUD over the "Today's Prices" catalog (which crops appear on the
/// marketplace discovery page) plus assembling that page's live response by
/// calling the Fair-Price Estimation Agent per item.
/// </summary>
public class TodayPriceCatalogService
{
    private readonly AppDbContext _db;
    private readonly AgenticAiService _agenticAi;

    public TodayPriceCatalogService(AppDbContext db, AgenticAiService agenticAi)
    {
        _db = db;
        _agenticAi = agenticAi;
    }

    public async Task<List<TodayPriceCatalogItemDto>> GetAll()
    {
        return await _db.TodayPriceCatalogItems
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<TodayPriceCatalogItemDto> Create(CreateTodayPriceCatalogItemDto dto)
    {
        var item = new TodayPriceCatalogItem
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "kg" : dto.Unit,
            DefaultRegion = dto.DefaultRegion.Trim(),
            ImageUrl = dto.ImageUrl,
            DisplayOrder = dto.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.TodayPriceCatalogItems.Add(item);
        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<TodayPriceCatalogItemDto> Update(Guid id, UpdateTodayPriceCatalogItemDto dto)
    {
        var item = await _db.TodayPriceCatalogItems.FindAsync(id)
            ?? throw new KeyNotFoundException("Catalog item not found.");

        if (dto.Name != null) item.Name = dto.Name.Trim();
        if (dto.Category != null) item.Category = dto.Category.Trim();
        if (dto.Unit != null) item.Unit = dto.Unit;
        if (dto.DefaultRegion != null) item.DefaultRegion = dto.DefaultRegion.Trim();
        if (dto.ImageUrl != null) item.ImageUrl = dto.ImageUrl;
        if (dto.DisplayOrder.HasValue) item.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;

        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task Delete(Guid id)
    {
        var item = await _db.TodayPriceCatalogItems.FindAsync(id)
            ?? throw new KeyNotFoundException("Catalog item not found.");

        _db.TodayPriceCatalogItems.Remove(item);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Assembles the live "Today's Prices" response: active catalog items in
    /// display order, each priced by the Fair-Price Estimation Agent (real
    /// Dambulla DEC data when available). Replaces the old approach of
    /// delegating straight to the Python service's own hardcoded catalog.
    /// </summary>
    public async Task<TodayPricesResponseDto> GetLivePrices(string? region, string? grade)
    {
        var gradeVal = string.IsNullOrWhiteSpace(grade) ? "A" : grade.ToUpperInvariant();

        var catalogItems = await _db.TodayPriceCatalogItems
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        // Fan out one estimate call per catalog item concurrently — sequential
        // awaits here would be painfully slow once an LLM provider is wired in
        // (each call can take a few seconds), especially with 15-20+ items.
        var itemTasks = catalogItems.Select(async c =>
        {
            var targetRegion = (!string.IsNullOrWhiteSpace(region) && region != "All") ? region : c.DefaultRegion;

            var est = await _agenticAi.EstimateFairPriceAsync(
                cropId: c.Id.ToString(),
                regionId: targetRegion,
                quantity: 100m,
                claimedGrade: gradeVal,
                cropName: c.Name,
                regionName: targetRegion);

            return new TodayPriceItemDto
            {
                CropId = c.Id.ToString(),
                Name = c.Name,
                Category = c.Category,
                Unit = c.Unit,
                Region = targetRegion,
                Grade = gradeVal,
                SuggestedPriceMin = est.SuggestedPriceMin,
                SuggestedPriceMax = est.SuggestedPriceMax,
                AveragePrice = est.AveragePrice,
                Confidence = est.Confidence,
                Change24h = est.Change24h,
                Trend = est.Trend,
                ImageUrl = c.ImageUrl ?? string.Empty,
                Reasoning = est.ReasoningSummary,
                BenchmarkWholesale = est.BenchmarkWholesale
            };
        });

        var items = (await Task.WhenAll(itemTasks)).ToList();

        return new TodayPricesResponseDto
        {
            Date = "Today",
            TotalCrops = items.Count,
            SelectedGrade = gradeVal,
            SelectedRegion = region ?? "All Regions",
            MarketStatus = "Active Trading",
            Items = items
        };
    }

    private static TodayPriceCatalogItemDto MapToDto(TodayPriceCatalogItem t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Category = t.Category,
        Unit = t.Unit,
        DefaultRegion = t.DefaultRegion,
        ImageUrl = t.ImageUrl,
        DisplayOrder = t.DisplayOrder,
        IsActive = t.IsActive
    };
}
