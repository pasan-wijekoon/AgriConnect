using backend.Config;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ListingService
{
    private readonly AppDbContext _db;
    private readonly AgenticAiService _agenticAi;

    public ListingService(AppDbContext db, AgenticAiService agenticAi)
    {
        _db = db;
        _agenticAi = agenticAi;
    }

    // ── Create Listing (FR3) ──────────────────────────────────
    public async Task<ListingResponseDto> CreateListing(Guid farmerId, CreateListingDto dto)
    {
        // Validate pickup window
        if (dto.PickupWindowEnd <= dto.PickupWindowStart)
            throw new ArgumentException("Pickup window end must be after start.");

        // Validate crop and region exist
        var crop = await _db.Crops.FindAsync(dto.CropId)
            ?? throw new KeyNotFoundException("Crop not found.");
        var region = await _db.Regions.FindAsync(dto.RegionId)
            ?? throw new KeyNotFoundException("Region not found.");

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            FarmerId = farmerId,
            CropId = dto.CropId,
            RegionId = dto.RegionId,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            ClaimedGrade = dto.ClaimedGrade,
            PickupWindowStart = dto.PickupWindowStart,
            PickupWindowEnd = dto.PickupWindowEnd,
            MinPrice = dto.MinPrice,
            Description = dto.Description,
            Status = "Published",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add photos (FR3 requires at least one)
        foreach (var url in dto.PhotoUrls)
        {
            listing.Photos.Add(new ListingPhoto
            {
                Id = Guid.NewGuid(),
                Url = url,
                UploadedAt = DateTime.UtcNow
            });
        }

        _db.Listings.Add(listing);
        await _db.SaveChangesAsync();

        // Auto-generate a price suggestion for the new listing (FR4)
        await GeneratePriceSuggestion(listing.Id);

        return await GetListingById(listing.Id);
    }

    // ── Search / Filter / Sort / Paginate (FR6) ───────────────
    public async Task<PagedResult<ListingResponseDto>> GetListings(ListingSearchQuery query)
    {
        var q = _db.Listings
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.PriceSuggestion)
            .AsQueryable();

        // Exclude own items (for public marketplace)
        if (query.ExcludeFarmerId.HasValue)
            q = q.Where(l => l.FarmerId != query.ExcludeFarmerId.Value);

        // Filters
        if (query.CropId.HasValue)
            q = q.Where(l => l.CropId == query.CropId.Value);

        if (query.RegionId.HasValue)
            q = q.Where(l => l.RegionId == query.RegionId.Value);

        if (!string.IsNullOrEmpty(query.Status))
            q = q.Where(l => l.Status == query.Status);

        if (!string.IsNullOrEmpty(query.Grade))
            q = q.Where(l => l.ClaimedGrade == query.Grade);

        if (query.MinPrice.HasValue)
            q = q.Where(l => l.MinPrice >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(l => l.MinPrice <= query.MaxPrice.Value);

        if (!string.IsNullOrEmpty(query.Search))
            q = q.Where(l => l.Crop.Name.Contains(query.Search));

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "price" => query.SortDir == "asc"
                ? q.OrderBy(l => l.MinPrice)
                : q.OrderByDescending(l => l.MinPrice),
            "quantity" => query.SortDir == "asc"
                ? q.OrderBy(l => l.Quantity)
                : q.OrderByDescending(l => l.Quantity),
            _ => query.SortDir == "asc"
                ? q.OrderBy(l => l.CreatedAt)
                : q.OrderByDescending(l => l.CreatedAt)
        };

        // Pagination
        var totalCount = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<ListingResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // ── Get Listings by Farmer ────────────────────────────────
    public async Task<PagedResult<ListingResponseDto>> GetListingsByFarmer(Guid farmerId, ListingSearchQuery query)
    {
        var q = _db.Listings
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.PriceSuggestion)
            .Where(l => l.FarmerId == farmerId)
            .AsQueryable();

        // Apply same filters
        if (!string.IsNullOrEmpty(query.Status))
            q = q.Where(l => l.Status == query.Status);

        if (!string.IsNullOrEmpty(query.Search))
            q = q.Where(l => l.Crop.Name.Contains(query.Search));

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "price" => query.SortDir == "asc"
                ? q.OrderBy(l => l.MinPrice)
                : q.OrderByDescending(l => l.MinPrice),
            _ => query.SortDir == "asc"
                ? q.OrderBy(l => l.CreatedAt)
                : q.OrderByDescending(l => l.CreatedAt)
        };

        var totalCount = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<ListingResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // ── Get Single Listing ────────────────────────────────────
    public async Task<ListingResponseDto> GetListingById(Guid id)
    {
        var listing = await _db.Listings
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.PriceSuggestion)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException("Listing not found.");

        return MapToDto(listing);
    }

    // ── Update Listing (FR7) ──────────────────────────────────
    public async Task<ListingResponseDto> UpdateListing(Guid id, Guid farmerId, UpdateListingDto dto)
    {
        var listing = await _db.Listings
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException("Listing not found.");

        // Only the owner can edit
        if (listing.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You can only edit your own listings.");

        // Cannot edit if orders exist (FR7 — checked via status)
        if (listing.Status == "SoldOut")
            throw new InvalidOperationException("Cannot edit a sold-out listing.");

        // Apply partial updates
        if (dto.CropId.HasValue) listing.CropId = dto.CropId.Value;
        if (dto.RegionId.HasValue) listing.RegionId = dto.RegionId.Value;
        if (dto.Quantity.HasValue) listing.Quantity = dto.Quantity.Value;
        if (dto.Unit != null) listing.Unit = dto.Unit;
        if (dto.ClaimedGrade != null) listing.ClaimedGrade = dto.ClaimedGrade;
        if (dto.PickupWindowStart.HasValue) listing.PickupWindowStart = dto.PickupWindowStart.Value;
        if (dto.PickupWindowEnd.HasValue) listing.PickupWindowEnd = dto.PickupWindowEnd.Value;
        if (dto.MinPrice.HasValue) listing.MinPrice = dto.MinPrice.Value;
        if (dto.Description != null) listing.Description = dto.Description;

        // Replace the photo set only when the edit form actually sent one.
        // The farmer's edit form always sends its current photo list (whether
        // changed or not) — previously this field didn't exist at all, so any
        // photo edit was silently discarded on save.
        if (dto.PhotoUrls != null)
        {
            if (dto.PhotoUrls.Count == 0)
                throw new ArgumentException("A listing must have at least one photo.");

            // ExecuteDeleteAsync issues a direct bulk DELETE rather than tracked
            // per-row deletes, so it can't throw DbUpdateConcurrencyException if
            // a duplicate/overlapping request already removed these rows (e.g. a
            // double form-submit) — it just deletes whatever is still there.
            await _db.ListingPhotos.Where(p => p.ListingId == listing.Id).ExecuteDeleteAsync();
            foreach (var stale in listing.Photos)
                _db.Entry(stale).State = EntityState.Detached;

            var newPhotos = dto.PhotoUrls.Select(url => new ListingPhoto
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                Url = url,
                UploadedAt = DateTime.UtcNow
            }).ToList();

            // Explicitly AddRange rather than assigning to listing.Photos and
            // letting EF's navigation-fixup infer the state: because these
            // entities already carry a non-default (client-generated) key, EF's
            // default heuristic can mark them Modified instead of Added when
            // discovered via a tracked parent's collection, producing a no-op
            // UPDATE for a row that was never inserted (the exact concurrency
            // bug this comment used to not warn about).
            _db.ListingPhotos.AddRange(newPhotos);
            listing.Photos = newPhotos;
        }

        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetListingById(id);
    }

    // ── Withdraw Listing (FR7) ────────────────────────────────
    public async Task WithdrawListing(Guid id, Guid farmerId)
    {
        var listing = await _db.Listings.FindAsync(id)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You can only withdraw your own listings.");

        if (listing.Status == "SoldOut")
            throw new InvalidOperationException("Cannot withdraw a sold-out listing.");

        listing.Status = "Withdrawn";
        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── Approve Listing (Admin) ───────────────────────────────
    public async Task<ListingResponseDto> ApproveListing(Guid id)
    {
        var listing = await _db.Listings
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.PriceSuggestion)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.Status != "PendingApproval")
            throw new InvalidOperationException($"Cannot approve a listing with status '{listing.Status}'.");

        listing.Status = "Published";
        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToDto(listing);
    }

    // ── Reject Listing (Admin) ────────────────────────────────
    public async Task<ListingResponseDto> RejectListing(Guid id)
    {
        var listing = await _db.Listings
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.PriceSuggestion)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.Status != "PendingApproval")
            throw new InvalidOperationException($"Cannot reject a listing with status '{listing.Status}'.");

        listing.Status = "Withdrawn";
        listing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToDto(listing);
    }

    // ── Add Photos ────────────────────────────────────────────
    public async Task<List<PhotoDto>> AddPhotos(Guid listingId, Guid farmerId, AddPhotosDto dto)
    {
        var listing = await _db.Listings.FindAsync(listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        if (listing.FarmerId != farmerId)
            throw new UnauthorizedAccessException("You can only add photos to your own listings.");

        var photos = dto.PhotoUrls.Select(url => new ListingPhoto
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            Url = url,
            UploadedAt = DateTime.UtcNow
        }).ToList();

        _db.ListingPhotos.AddRange(photos);
        await _db.SaveChangesAsync();

        return photos.Select(p => new PhotoDto
        {
            Id = p.Id,
            Url = p.Url,
            UploadedAt = p.UploadedAt
        }).ToList();
    }

    // ── Get / Trigger Price Suggestion (FR4 — Business-Specific) ──
    public async Task<PriceSuggestionDto> GetPriceSuggestion(Guid listingId)
    {
        var listing = await _db.Listings
            .Include(l => l.PriceSuggestion)
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .FirstOrDefaultAsync(l => l.Id == listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        // If no suggestion exists yet, generate one
        if (listing.PriceSuggestion == null)
        {
            await GeneratePriceSuggestion(listingId);
            await _db.Entry(listing).Reference(l => l.PriceSuggestion).LoadAsync();
        }

        return MapPriceSuggestion(listing.PriceSuggestion!);
    }

    // ── AI Price Suggestion Generator (FR4 — Agentic AI Workflow) ─────
    // Runs the actual LangGraph coordinator in the Python agentic-ai service:
    // planner -> fair-price agent -> deterministic validator -> conditional
    // anomaly routing -> human-approval checkpoint. The result always lands
    // as "Proposed" (awaiting an officer's Approve/Reject/Revise) unless the
    // deterministic validator itself rejected it as an outlier, in which case
    // it's recorded as auto-rejected and never shown to an officer for action.
    private async Task GeneratePriceSuggestion(Guid listingId)
    {
        var listing = await _db.Listings
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .FirstAsync(l => l.Id == listingId);

        // Recent local sale data for the same crop/region, blended into the
        // agent's market lookup alongside live wholesale benchmarks.
        var recentPrices = await _db.Listings
            .Where(l => l.CropId == listing.CropId
                     && l.RegionId == listing.RegionId
                     && l.MinPrice.HasValue
                     && l.Id != listingId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(20)
            .Select(l => (object)l.MinPrice!.Value)
            .ToListAsync();

        var result = await _agenticAi.RunFairPriceOrchestrationAsync(
            listingId: listingId,
            cropId: listing.CropId.ToString(),
            cropName: listing.Crop.Name,
            regionId: listing.RegionId.ToString(),
            regionName: listing.Region.Name,
            quantity: listing.Quantity,
            claimedGrade: listing.ClaimedGrade,
            recentSaleData: recentPrices);

        var autoRejected = result.ApprovalStatus == "RejectedByValidation";

        var suggestion = new PriceSuggestion
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            SuggestedPriceMin = result.SuggestedPriceMin,
            SuggestedPriceMax = result.SuggestedPriceMax,
            Confidence = result.Confidence,
            ReasoningSummary = result.ReasoningSummary,
            Status = autoRejected ? "Rejected" : "Proposed",
            AutoRejectedByValidation = autoRejected,
            ValidationSummary = result.ValidationSummary,
            CheckpointName = result.CheckpointName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.PriceSuggestions.Add(suggestion);
        await _db.SaveChangesAsync();
    }

    // ── Officer Decision on a Price Suggestion (Approve / Reject) ─────
    public async Task<PriceSuggestionDto> DecidePriceSuggestion(Guid listingId, string decision, Guid officerId, string? officerNote)
    {
        if (decision != "Approved" && decision != "Rejected")
            throw new ArgumentException("Decision must be 'Approved' or 'Rejected'.");

        var listing = await _db.Listings
            .Include(l => l.PriceSuggestion)
            .FirstOrDefaultAsync(l => l.Id == listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        var suggestion = listing.PriceSuggestion
            ?? throw new KeyNotFoundException("No price suggestion exists for this listing.");

        if (suggestion.Status != "Proposed")
            throw new InvalidOperationException($"Cannot decide a price suggestion with status '{suggestion.Status}'.");

        suggestion.Status = decision;
        suggestion.DecidedByUserId = officerId;
        suggestion.DecidedAt = DateTime.UtcNow;
        suggestion.OfficerNote = officerNote;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapPriceSuggestion(suggestion);
    }

    // ── Officer Revision of a Price Suggestion (Request Revision) ─────
    public async Task<PriceSuggestionDto> RevisePriceSuggestion(Guid listingId, Guid officerId, RevisePriceSuggestionDto dto)
    {
        if (dto.RevisedPriceMax < dto.RevisedPriceMin)
            throw new ArgumentException("Revised maximum price cannot be lower than the revised minimum.");

        var listing = await _db.Listings
            .Include(l => l.PriceSuggestion)
            .FirstOrDefaultAsync(l => l.Id == listingId)
            ?? throw new KeyNotFoundException("Listing not found.");

        var suggestion = listing.PriceSuggestion
            ?? throw new KeyNotFoundException("No price suggestion exists for this listing.");

        if (suggestion.Status != "Proposed")
            throw new InvalidOperationException($"Cannot revise a price suggestion with status '{suggestion.Status}'.");

        suggestion.SuggestedPriceMin = dto.RevisedPriceMin;
        suggestion.SuggestedPriceMax = dto.RevisedPriceMax;
        suggestion.Status = "Revised";
        suggestion.DecidedByUserId = officerId;
        suggestion.DecidedAt = DateTime.UtcNow;
        suggestion.OfficerNote = dto.OfficerNote;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapPriceSuggestion(suggestion);
    }

    private static PriceSuggestionDto MapPriceSuggestion(PriceSuggestion ps) => new()
    {
        Id = ps.Id,
        SuggestedPriceMin = ps.SuggestedPriceMin,
        SuggestedPriceMax = ps.SuggestedPriceMax,
        Confidence = ps.Confidence,
        ReasoningSummary = ps.ReasoningSummary,
        Status = ps.Status,
        CheckpointName = ps.CheckpointName,
        DecidedByUserId = ps.DecidedByUserId,
        DecidedAt = ps.DecidedAt,
        OfficerNote = ps.OfficerNote
    };

    // ── Get All Crops (reference data) ────────────────────────
    public async Task<List<CropDto>> GetAllCrops()
    {
        return await _db.Crops
            .OrderBy(c => c.Name)
            .Select(c => new CropDto { Id = c.Id, Name = c.Name, Category = c.Category })
            .ToListAsync();
    }

    // ── Get All Regions (reference data) ──────────────────────
    public async Task<List<RegionDto>> GetAllRegions()
    {
        return await _db.Regions
            .OrderBy(r => r.Name)
            .Select(r => new RegionDto { Id = r.Id, Name = r.Name })
            .ToListAsync();
    }

    // ── Mapping Helper ────────────────────────────────────────
    private static ListingResponseDto MapToDto(Listing l)
    {
        return new ListingResponseDto
        {
            Id = l.Id,
            FarmerId = l.FarmerId,
            CropName = l.Crop.Name,
            CropCategory = l.Crop.Category,
            RegionName = l.Region.Name,
            Quantity = l.Quantity,
            Unit = l.Unit,
            ClaimedGrade = l.ClaimedGrade,
            PickupWindowStart = l.PickupWindowStart,
            PickupWindowEnd = l.PickupWindowEnd,
            Status = l.Status,
            MinPrice = l.MinPrice,
            Description = l.Description,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            Photos = l.Photos.Select(p => new PhotoDto
            {
                Id = p.Id,
                Url = p.Url,
                UploadedAt = p.UploadedAt
            }).ToList(),
            PriceSuggestion = l.PriceSuggestion == null ? null : MapPriceSuggestion(l.PriceSuggestion)
        };
    }
}
