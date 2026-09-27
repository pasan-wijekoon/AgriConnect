using AgriConnect.Api.Config;
using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriConnect.Api.Services;

/// <summary>
/// Component C — Quality Grading &amp; Inspection (FR5, FR12–FR14). Folded onto
/// the shared AgriConnectDbContext (and the shared AuditLogService/NotificationService,
/// same convention as OrderService/SchedulingService) during integration —
/// originally written against Component C's own AppDbContext with raw
/// db.AuditLogs.Add(...)/db.Notifications.Add(...) calls. See PROGRESS.md.
/// </summary>
public class InspectionService(AgriConnectDbContext context, AuditLogService auditLog, NotificationService notifications) : IInspectionService
{
    public async Task<InspectionResponseDto> RecordInspectionAsync(CreateInspectionRequest request, Guid officerId)
    {
        if (string.IsNullOrWhiteSpace(request.ConfirmedGrade) || !QualityGrade.IsValid(request.ConfirmedGrade))
        {
            throw new ArgumentException($"Invalid quality grade '{request.ConfirmedGrade}'. Must be one of: Grade A, Grade B, Grade C, Rejected.");
        }

        var listing = await context.Listings
            .Include(l => l.Farmer)
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId);

        if (listing == null)
        {
            throw new KeyNotFoundException($"Listing with ID {request.ListingId} was not found.");
        }

        var officer = await context.Users.FindAsync(officerId);
        if (officer == null)
        {
            throw new KeyNotFoundException($"Officer with ID {officerId} was not found.");
        }

        // A plain SaveChangesAsync below is already one atomic unit of work for
        // all the Add() calls collected before it — no explicit transaction
        // needed (unlike StockReservationService, this isn't a concurrency-
        // critical read-check-write; it's a single Officer-driven action).
        {
            var inspection = new Inspection
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                OfficerId = officerId,
                ConfirmedGrade = request.ConfirmedGrade,
                Notes = request.Notes,
                InspectedAt = DateTime.UtcNow
            };

            foreach (var url in request.PhotoUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                inspection.Photos.Add(new InspectionPhoto
                {
                    Id = Guid.NewGuid(),
                    InspectionId = inspection.Id,
                    Url = url,
                    UploadedAt = DateTime.UtcNow
                });
            }

            context.Inspections.Add(inspection);

            // FR14: Check claimed vs confirmed grade discrepancy
            bool hasDiscrepancy = !string.Equals(listing.ClaimedGrade.Trim(), request.ConfirmedGrade.Trim(), StringComparison.OrdinalIgnoreCase);
            if (hasDiscrepancy)
            {
                context.GradeDiscrepancyFlags.Add(new GradeDiscrepancyFlag
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    InspectionId = inspection.Id,
                    ClaimedGrade = listing.ClaimedGrade,
                    ConfirmedGrade = request.ConfirmedGrade,
                    FlaggedAt = DateTime.UtcNow
                });

                notifications.Notify(
                    listing.FarmerId,
                    "Warning",
                    $"Your listing for {listing.Crop?.Name ?? "produce"} ({listing.Quantity}{listing.Unit}) was inspected. Claimed grade '{listing.ClaimedGrade}' differed from confirmed grade '{request.ConfirmedGrade}'.",
                    "Grade Discrepancy Flagged");
            }
            else
            {
                notifications.Notify(
                    listing.FarmerId,
                    "Success",
                    $"Your listing for {listing.Crop?.Name ?? "produce"} has been verified as {request.ConfirmedGrade}.",
                    "Inspection Completed");
            }

            // FR13 & Audit Logging
            auditLog.Log(officerId, "RecordInspection", "Inspection", inspection.Id, new
            {
                ListingId = listing.Id,
                Crop = listing.Crop?.Name,
                ClaimedGrade = listing.ClaimedGrade,
                ConfirmedGrade = request.ConfirmedGrade,
                HasDiscrepancy = hasDiscrepancy,
                Notes = request.Notes,
                PhotoCount = request.PhotoUrls.Count
            });

            await context.SaveChangesAsync();

            return ToResponseDto(inspection, listing, officer, hasDiscrepancy);
        }
    }

    public async Task<InspectionPagedResult<InspectionResponseDto>> GetInspectionsAsync(InspectionFilterParams filter)
    {
        var query = context.Inspections
            .Include(i => i.Listing).ThenInclude(l => l!.Crop)
            .Include(i => i.Listing).ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing).ThenInclude(l => l!.Region)
            .Include(i => i.Officer)
            .Include(i => i.Photos)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(i =>
                (i.Listing != null && i.Listing.Crop != null && i.Listing.Crop.Name.ToLower().Contains(search)) ||
                (i.Listing != null && i.Listing.Farmer != null && i.Listing.Farmer.FullName.ToLower().Contains(search)) ||
                (i.Officer != null && i.Officer.FullName.ToLower().Contains(search)) ||
                (i.Notes != null && i.Notes.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Grade))
        {
            query = query.Where(i => i.ConfirmedGrade.ToLower() == filter.Grade.Trim().ToLower());
        }

        if (filter.CropId.HasValue)
        {
            query = query.Where(i => i.Listing != null && i.Listing.CropId == filter.CropId.Value);
        }

        if (filter.RegionId.HasValue)
        {
            query = query.Where(i => i.Listing != null && i.Listing.RegionId == filter.RegionId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(i => i.InspectedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(i => i.InspectedAt <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(i => i.InspectedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new InspectionPagedResult<InspectionResponseDto>
        {
            Items = items.Select(i => ToResponseDto(i, i.Listing, i.Officer, HasDiscrepancy(i.Listing, i.ConfirmedGrade))).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<InspectionResponseDto?> GetInspectionByIdAsync(Guid id)
    {
        var inspection = await context.Inspections
            .Include(i => i.Listing).ThenInclude(l => l!.Crop)
            .Include(i => i.Listing).ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing).ThenInclude(l => l!.Region)
            .Include(i => i.Officer)
            .Include(i => i.Photos)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection == null) return null;

        return ToResponseDto(inspection, inspection.Listing, inspection.Officer, HasDiscrepancy(inspection.Listing, inspection.ConfirmedGrade));
    }

    public async Task<List<InspectionResponseDto>> GetInspectionsByListingIdAsync(Guid listingId)
    {
        var inspections = await context.Inspections
            .Where(i => i.ListingId == listingId)
            .Include(i => i.Listing).ThenInclude(l => l!.Crop)
            .Include(i => i.Listing).ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing).ThenInclude(l => l!.Region)
            .Include(i => i.Officer)
            .Include(i => i.Photos)
            .OrderByDescending(i => i.InspectedAt)
            .ToListAsync();

        return inspections.Select(i => ToResponseDto(i, i.Listing, i.Officer, HasDiscrepancy(i.Listing, i.ConfirmedGrade))).ToList();
    }

    public async Task<InspectionResponseDto> AmendInspectionAsync(Guid id, UpdateInspectionRequest request, Guid officerId)
    {
        if (string.IsNullOrWhiteSpace(request.ConfirmedGrade) || !QualityGrade.IsValid(request.ConfirmedGrade))
        {
            throw new ArgumentException($"Invalid quality grade '{request.ConfirmedGrade}'. Must be one of: Grade A, Grade B, Grade C, Rejected.");
        }

        if (string.IsNullOrWhiteSpace(request.ReasonForAmendment))
        {
            throw new ArgumentException("Reason for amendment is required for audit trail tracking.");
        }

        var inspection = await context.Inspections
            .Include(i => i.Listing).ThenInclude(l => l!.Crop)
            .Include(i => i.Listing).ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing).ThenInclude(l => l!.Region)
            .Include(i => i.Photos)
            .Include(i => i.Officer)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection == null)
        {
            throw new KeyNotFoundException($"Inspection with ID {id} was not found.");
        }

        var officer = await context.Users.FindAsync(officerId);
        if (officer == null)
        {
            throw new KeyNotFoundException($"Officer with ID {officerId} was not found.");
        }

        var oldGrade = inspection.ConfirmedGrade;
        var oldNotes = inspection.Notes;

        inspection.ConfirmedGrade = request.ConfirmedGrade;
        inspection.Notes = request.Notes;
        inspection.UpdatedAt = DateTime.UtcNow;

        foreach (var url in request.PhotoUrls.Where(u => !string.IsNullOrWhiteSpace(u) && !inspection.Photos.Any(p => p.Url == u)))
        {
            inspection.Photos.Add(new InspectionPhoto
            {
                Id = Guid.NewGuid(),
                InspectionId = inspection.Id,
                Url = url,
                UploadedAt = DateTime.UtcNow
            });
        }

        // FR13: Record immutable audit entry
        auditLog.Log(officerId, "AmendInspection", "Inspection", inspection.Id, new
        {
            PreviousGrade = oldGrade,
            NewGrade = request.ConfirmedGrade,
            PreviousNotes = oldNotes,
            NewNotes = request.Notes,
            Reason = request.ReasonForAmendment,
            AmendedBy = officer.FullName
        });

        await context.SaveChangesAsync();

        return ToResponseDto(inspection, inspection.Listing, officer, HasDiscrepancy(inspection.Listing, inspection.ConfirmedGrade));
    }

    public async Task<List<GradeDiscrepancyDto>> GetDiscrepanciesAsync(bool? onlyUnresolved = true)
    {
        var query = context.GradeDiscrepancyFlags
            .Include(f => f.Listing).ThenInclude(l => l!.Crop)
            .Include(f => f.Listing).ThenInclude(l => l!.Farmer)
            .Include(f => f.ResolvedByOfficer)
            .AsNoTracking();

        if (onlyUnresolved == true)
        {
            query = query.Where(f => !f.ResolvedAt.HasValue);
        }

        var flags = await query.OrderByDescending(f => f.FlaggedAt).ToListAsync();

        return flags.Select(ToDiscrepancyDto).ToList();
    }

    public async Task<GradeDiscrepancyDto> ResolveDiscrepancyAsync(Guid flagId, ResolveDiscrepancyRequest request, Guid officerId)
    {
        var flag = await context.GradeDiscrepancyFlags
            .Include(f => f.Listing).ThenInclude(l => l!.Crop)
            .Include(f => f.Listing).ThenInclude(l => l!.Farmer)
            .FirstOrDefaultAsync(f => f.Id == flagId);

        if (flag == null)
        {
            throw new KeyNotFoundException($"Grade discrepancy flag with ID {flagId} was not found.");
        }

        var officer = await context.Users.FindAsync(officerId);
        if (officer == null)
        {
            throw new KeyNotFoundException($"Officer with ID {officerId} was not found.");
        }

        flag.ResolvedAt = DateTime.UtcNow;
        flag.ResolutionNotes = request.ResolutionNotes;
        flag.ResolvedByOfficerId = officerId;
        flag.UpdatedAt = DateTime.UtcNow;

        auditLog.Log(officerId, "ResolveGradeDiscrepancy", "GradeDiscrepancyFlag", flag.Id, new
        {
            ListingId = flag.ListingId,
            ClaimedGrade = flag.ClaimedGrade,
            ConfirmedGrade = flag.ConfirmedGrade,
            ResolutionNotes = request.ResolutionNotes,
            ResolvedBy = officer.FullName
        });

        if (flag.Listing != null)
        {
            notifications.Notify(
                flag.Listing.FarmerId,
                "Info",
                $"The grade discrepancy on your listing for {flag.Listing.Crop?.Name ?? "produce"} was resolved. Notes: {request.ResolutionNotes}",
                "Grade Discrepancy Resolved");
        }

        await context.SaveChangesAsync();

        return new GradeDiscrepancyDto
        {
            Id = flag.Id,
            ListingId = flag.ListingId,
            CropName = flag.Listing?.Crop?.Name ?? "Unknown",
            FarmerName = flag.Listing?.Farmer?.FullName ?? "Unknown",
            Quantity = flag.Listing?.Quantity ?? 0,
            Unit = flag.Listing?.Unit ?? "kg",
            ClaimedGrade = flag.ClaimedGrade,
            ConfirmedGrade = flag.ConfirmedGrade,
            FlaggedAt = flag.FlaggedAt,
            ResolvedAt = flag.ResolvedAt,
            ResolutionNotes = flag.ResolutionNotes,
            ResolvedByOfficerName = officer.FullName
        };
    }

    /// <summary>
    /// FR5 gate: a listing becomes Published ONLY after passing quality inspection
    /// AND receiving officer approval — the sole path to Listing.Status = Published.
    /// Component A's own PATCH /api/listings/{id}/approve (Administrator role) was
    /// removed during integration because it set Published directly with no
    /// inspection check at all — a real compliance hole against FR5, not just
    /// redundant code. See PROGRESS.md, Decisions.
    /// </summary>
    public async Task<ListingSummaryDto> PublishListingWithGateCheckAsync(Guid listingId, Guid officerId)
    {
        var listing = await context.Listings
            .Include(l => l.Crop)
            .Include(l => l.Farmer)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.Inspections)
            .Include(l => l.GradeDiscrepancyFlags)
            .FirstOrDefaultAsync(l => l.Id == listingId);

        if (listing == null)
        {
            throw new KeyNotFoundException($"Listing with ID {listingId} was not found.");
        }

        var officer = await context.Users.FindAsync(officerId);
        if (officer == null)
        {
            throw new KeyNotFoundException($"Officer with ID {officerId} was not found.");
        }

        // FR5 Rule 1: Must have at least one recorded inspection
        var latestInspection = listing.Inspections.OrderByDescending(i => i.InspectedAt).FirstOrDefault();
        if (latestInspection == null)
        {
            throw new InvalidOperationException("Quality Verification Gate Blocked: The listing cannot be published because it has not been inspected by an officer (FR5). Please record an inspection first.");
        }

        // FR5 Rule 2: Grade cannot be Rejected
        if (string.Equals(latestInspection.ConfirmedGrade, QualityGrade.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Quality Verification Gate Blocked: The listing was confirmed as 'Rejected' during quality inspection and cannot be published to the buyer marketplace.");
        }

        // FR5 Rule 3: Unresolved discrepancies must be resolved first
        var openDiscrepancy = listing.GradeDiscrepancyFlags.FirstOrDefault(f => !f.ResolvedAt.HasValue);
        if (openDiscrepancy != null)
        {
            throw new InvalidOperationException($"Quality Verification Gate Blocked: There is an active unresolved grade discrepancy (Claimed: {openDiscrepancy.ClaimedGrade} vs Confirmed: {openDiscrepancy.ConfirmedGrade}). Resolve the discrepancy before publishing.");
        }

        listing.Status = ListingStatus.Published;
        listing.UpdatedAt = DateTime.UtcNow;

        auditLog.Log(officerId, "PublishListingApproved", "Listing", listing.Id, new
        {
            ListingId = listing.Id,
            Crop = listing.Crop?.Name,
            ConfirmedGrade = latestInspection.ConfirmedGrade,
            ApprovedByOfficer = officer.FullName
        });

        notifications.Notify(
            listing.FarmerId,
            "Success",
            $"Your listing for {listing.Crop?.Name ?? "produce"} ({listing.Quantity}{listing.Unit}, {latestInspection.ConfirmedGrade}) has been verified and published! It is now visible to buyers.",
            "Listing Published to Marketplace");

        await context.SaveChangesAsync();

        return ToSummaryDto(listing, latestInspection.ConfirmedGrade, hasUnresolvedDiscrepancy: false);
    }

    public async Task<List<ListingSummaryDto>> GetPendingListingsForInspectionAsync()
    {
        var listings = await context.Listings
            .Where(l => l.Status == ListingStatus.PendingApproval || l.Status == ListingStatus.Draft)
            .Include(l => l.Crop)
            .Include(l => l.Farmer)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.Inspections)
            .Include(l => l.GradeDiscrepancyFlags)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return listings.Select(ToSummaryDto).ToList();
    }

    public async Task<List<ListingSummaryDto>> GetFarmerListingsAsync(Guid farmerId)
    {
        var listings = await context.Listings
            .Where(l => l.FarmerId == farmerId)
            .Include(l => l.Crop)
            .Include(l => l.Farmer)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.Inspections)
            .Include(l => l.GradeDiscrepancyFlags)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return listings.Select(ToSummaryDto).ToList();
    }

    public async Task<QualityDashboardStatsDto> GetDashboardStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var pendingInspections = await context.Listings.CountAsync(l => l.Status == ListingStatus.PendingApproval && !l.Inspections.Any());
        var completedToday = await context.Inspections.CountAsync(i => i.InspectedAt >= today);
        var totalInspections = await context.Inspections.CountAsync();
        var activeDiscrepancies = await context.GradeDiscrepancyFlags.CountAsync(f => !f.ResolvedAt.HasValue);
        var publishedListings = await context.Listings.CountAsync(l => l.Status == ListingStatus.Published);

        var gradeACount = await context.Inspections.CountAsync(i => i.ConfirmedGrade == QualityGrade.GradeA);
        decimal gradeARate = totalInspections > 0 ? Math.Round((decimal)gradeACount / totalInspections * 100, 1) : 0;

        return new QualityDashboardStatsDto
        {
            PendingInspections = pendingInspections,
            CompletedToday = completedToday,
            TotalInspections = totalInspections,
            ActiveDiscrepancies = activeDiscrepancies,
            PublishedListings = publishedListings,
            GradeAComplianceRate = gradeARate
        };
    }

    private static bool HasDiscrepancy(Listing? listing, string confirmedGrade) =>
        listing != null && !string.Equals(listing.ClaimedGrade, confirmedGrade, StringComparison.OrdinalIgnoreCase);

    private static InspectionResponseDto ToResponseDto(Inspection inspection, Listing? listing, User? officer, bool hasDiscrepancy) => new()
    {
        Id = inspection.Id,
        ListingId = inspection.ListingId,
        CropName = listing?.Crop?.Name ?? "Unknown",
        Quantity = listing?.Quantity ?? 0,
        Unit = listing?.Unit ?? "kg",
        ClaimedGrade = listing?.ClaimedGrade ?? string.Empty,
        ConfirmedGrade = inspection.ConfirmedGrade,
        Notes = inspection.Notes,
        InspectedAt = inspection.InspectedAt,
        OfficerId = inspection.OfficerId,
        OfficerName = officer?.FullName ?? "Unknown",
        FarmerName = listing?.Farmer?.FullName ?? "Unknown",
        RegionName = listing?.Region?.Name ?? "Unknown",
        ListingStatus = listing?.Status ?? "Unknown",
        HasDiscrepancy = hasDiscrepancy,
        Photos = inspection.Photos.Select(p => new InspectionPhotoDto { Id = p.Id, Url = p.Url, UploadedAt = p.UploadedAt }).ToList()
    };

    private static GradeDiscrepancyDto ToDiscrepancyDto(GradeDiscrepancyFlag f) => new()
    {
        Id = f.Id,
        ListingId = f.ListingId,
        CropName = f.Listing?.Crop?.Name ?? "Unknown",
        FarmerName = f.Listing?.Farmer?.FullName ?? "Unknown",
        Quantity = f.Listing?.Quantity ?? 0,
        Unit = f.Listing?.Unit ?? "kg",
        ClaimedGrade = f.ClaimedGrade,
        ConfirmedGrade = f.ConfirmedGrade,
        FlaggedAt = f.FlaggedAt,
        ResolvedAt = f.ResolvedAt,
        ResolutionNotes = f.ResolutionNotes,
        ResolvedByOfficerName = f.ResolvedByOfficer?.FullName
    };

    private static ListingSummaryDto ToSummaryDto(Listing l)
    {
        var latestInspection = l.Inspections.OrderByDescending(i => i.InspectedAt).FirstOrDefault();
        var hasUnresolvedDiscrepancy = l.GradeDiscrepancyFlags.Any(f => !f.ResolvedAt.HasValue);
        return ToSummaryDto(l, latestInspection?.ConfirmedGrade, hasUnresolvedDiscrepancy);
    }

    private static ListingSummaryDto ToSummaryDto(Listing l, string? latestConfirmedGrade, bool hasUnresolvedDiscrepancy) => new()
    {
        Id = l.Id,
        FarmerId = l.FarmerId,
        FarmerName = l.Farmer?.FullName ?? "Unknown",
        FarmerPhone = l.Farmer?.Phone ?? string.Empty,
        CropId = l.CropId,
        CropName = l.Crop?.Name ?? "Unknown",
        Category = l.Crop?.Category ?? "Unknown",
        RegionId = l.RegionId,
        RegionName = l.Region?.Name ?? "Unknown",
        Quantity = l.Quantity,
        Unit = l.Unit,
        ClaimedGrade = l.ClaimedGrade,
        LatestConfirmedGrade = latestConfirmedGrade,
        PickupWindowStart = l.PickupWindowStart,
        PickupWindowEnd = l.PickupWindowEnd,
        Status = l.Status,
        MinPrice = l.MinPrice,
        CreatedAt = l.CreatedAt,
        InspectionCount = l.Inspections.Count,
        HasUnresolvedDiscrepancy = hasUnresolvedDiscrepancy,
        ListingPhotos = l.Photos.Select(p => p.Url).ToList()
    };
}
