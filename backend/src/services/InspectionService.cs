using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using backend.src.config;
using backend.src.dtos;
using backend.src.models;

namespace backend.src.services;

public class InspectionService : IInspectionService
{
    private readonly AppDbContext _context;

    public InspectionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InspectionResponseDto> RecordInspectionAsync(CreateInspectionRequest request, Guid officerId)
    {
        if (string.IsNullOrWhiteSpace(request.ConfirmedGrade) || !QualityGrade.IsValid(request.ConfirmedGrade))
        {
            throw new ArgumentException($"Invalid quality grade '{request.ConfirmedGrade}'. Must be one of: Grade A, Grade B, Grade C, Rejected.");
        }

        var listing = await _context.Listings
            .Include(l => l.Farmer)
            .Include(l => l.Crop)
            .Include(l => l.Region)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId);

        if (listing == null)
        {
            throw new KeyNotFoundException($"Listing with ID {request.ListingId} was not found.");
        }

        var officer = await _context.Users.FindAsync(officerId);
        if (officer == null)
        {
            throw new KeyNotFoundException($"Officer with ID {officerId} was not found.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
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

            if (request.PhotoUrls != null && request.PhotoUrls.Count > 0)
            {
                foreach (var url in request.PhotoUrls)
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        inspection.Photos.Add(new InspectionPhoto
                        {
                            Id = Guid.NewGuid(),
                            InspectionId = inspection.Id,
                            Url = url,
                            UploadedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            _context.Inspections.Add(inspection);

            // FR14: Check claimed vs confirmed grade discrepancy
            bool hasDiscrepancy = !string.Equals(listing.ClaimedGrade.Trim(), request.ConfirmedGrade.Trim(), StringComparison.OrdinalIgnoreCase);
            if (hasDiscrepancy)
            {
                var discrepancyFlag = new GradeDiscrepancyFlag
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    InspectionId = inspection.Id,
                    ClaimedGrade = listing.ClaimedGrade,
                    ConfirmedGrade = request.ConfirmedGrade,
                    FlaggedAt = DateTime.UtcNow
                };
                _context.GradeDiscrepancyFlags.Add(discrepancyFlag);

                // Notification for farmer
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = listing.FarmerId,
                    Title = "Grade Discrepancy Flagged",
                    Message = $"Your listing for {listing.Crop?.Name ?? "produce"} ({listing.Quantity}{listing.Unit}) was inspected. Claimed grade '{listing.ClaimedGrade}' differed from confirmed grade '{request.ConfirmedGrade}'.",
                    Type = "Warning",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Successful inspection notification
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = listing.FarmerId,
                    Title = "Inspection Completed",
                    Message = $"Your listing for {listing.Crop?.Name ?? "produce"} has been verified as {request.ConfirmedGrade}.",
                    Type = "Success",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // FR13 & Audit Logging
            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = officerId,
                Action = "RecordInspection",
                EntityType = "Inspection",
                EntityId = inspection.Id,
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    ListingId = listing.Id,
                    Crop = listing.Crop?.Name,
                    ClaimedGrade = listing.ClaimedGrade,
                    ConfirmedGrade = request.ConfirmedGrade,
                    HasDiscrepancy = hasDiscrepancy,
                    Notes = request.Notes,
                    PhotoCount = request.PhotoUrls?.Count ?? 0
                })
            };
            _context.AuditLogs.Add(audit);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new InspectionResponseDto
            {
                Id = inspection.Id,
                ListingId = listing.Id,
                CropName = listing.Crop?.Name ?? "Unknown",
                Quantity = listing.Quantity,
                Unit = listing.Unit,
                ClaimedGrade = listing.ClaimedGrade,
                ConfirmedGrade = inspection.ConfirmedGrade,
                Notes = inspection.Notes,
                InspectedAt = inspection.InspectedAt,
                OfficerId = officer.Id,
                OfficerName = officer.FullName,
                FarmerName = listing.Farmer?.FullName ?? "Unknown",
                RegionName = listing.Region?.Name ?? "Unknown",
                ListingStatus = listing.Status,
                HasDiscrepancy = hasDiscrepancy,
                Photos = inspection.Photos.Select(p => new InspectionPhotoDto
                {
                    Id = p.Id,
                    Url = p.Url,
                    UploadedAt = p.UploadedAt
                }).ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<InspectionResponseDto>> GetInspectionsAsync(InspectionFilterParams filter)
    {
        var query = _context.Inspections
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Crop)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Region)
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
            .Select(i => new InspectionResponseDto
            {
                Id = i.Id,
                ListingId = i.ListingId,
                CropName = i.Listing != null && i.Listing.Crop != null ? i.Listing.Crop.Name : "Unknown",
                Quantity = i.Listing != null ? i.Listing.Quantity : 0,
                Unit = i.Listing != null ? i.Listing.Unit : "kg",
                ClaimedGrade = i.Listing != null ? i.Listing.ClaimedGrade : string.Empty,
                ConfirmedGrade = i.ConfirmedGrade,
                Notes = i.Notes,
                InspectedAt = i.InspectedAt,
                OfficerId = i.OfficerId,
                OfficerName = i.Officer != null ? i.Officer.FullName : "Unknown",
                FarmerName = i.Listing != null && i.Listing.Farmer != null ? i.Listing.Farmer.FullName : "Unknown",
                RegionName = i.Listing != null && i.Listing.Region != null ? i.Listing.Region.Name : "Unknown",
                ListingStatus = i.Listing != null ? i.Listing.Status : "Unknown",
                HasDiscrepancy = i.Listing != null && !string.Equals(i.Listing.ClaimedGrade, i.ConfirmedGrade, StringComparison.OrdinalIgnoreCase),
                Photos = i.Photos.Select(p => new InspectionPhotoDto
                {
                    Id = p.Id,
                    Url = p.Url,
                    UploadedAt = p.UploadedAt
                }).ToList()
            })
            .ToListAsync();

        return new PagedResult<InspectionResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<InspectionResponseDto?> GetInspectionByIdAsync(Guid id)
    {
        var inspection = await _context.Inspections
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Crop)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Region)
            .Include(i => i.Officer)
            .Include(i => i.Photos)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection == null) return null;

        return new InspectionResponseDto
        {
            Id = inspection.Id,
            ListingId = inspection.ListingId,
            CropName = inspection.Listing?.Crop?.Name ?? "Unknown",
            Quantity = inspection.Listing?.Quantity ?? 0,
            Unit = inspection.Listing?.Unit ?? "kg",
            ClaimedGrade = inspection.Listing?.ClaimedGrade ?? string.Empty,
            ConfirmedGrade = inspection.ConfirmedGrade,
            Notes = inspection.Notes,
            InspectedAt = inspection.InspectedAt,
            OfficerId = inspection.OfficerId,
            OfficerName = inspection.Officer?.FullName ?? "Unknown",
            FarmerName = inspection.Listing?.Farmer?.FullName ?? "Unknown",
            RegionName = inspection.Listing?.Region?.Name ?? "Unknown",
            ListingStatus = inspection.Listing?.Status ?? "Unknown",
            HasDiscrepancy = inspection.Listing != null && !string.Equals(inspection.Listing.ClaimedGrade, inspection.ConfirmedGrade, StringComparison.OrdinalIgnoreCase),
            Photos = inspection.Photos.Select(p => new InspectionPhotoDto
            {
                Id = p.Id,
                Url = p.Url,
                UploadedAt = p.UploadedAt
            }).ToList()
        };
    }

    public async Task<List<InspectionResponseDto>> GetInspectionsByListingIdAsync(Guid listingId)
    {
        return await _context.Inspections
            .Where(i => i.ListingId == listingId)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Crop)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Region)
            .Include(i => i.Officer)
            .Include(i => i.Photos)
            .OrderByDescending(i => i.InspectedAt)
            .Select(i => new InspectionResponseDto
            {
                Id = i.Id,
                ListingId = i.ListingId,
                CropName = i.Listing != null && i.Listing.Crop != null ? i.Listing.Crop.Name : "Unknown",
                Quantity = i.Listing != null ? i.Listing.Quantity : 0,
                Unit = i.Listing != null ? i.Listing.Unit : "kg",
                ClaimedGrade = i.Listing != null ? i.Listing.ClaimedGrade : string.Empty,
                ConfirmedGrade = i.ConfirmedGrade,
                Notes = i.Notes,
                InspectedAt = i.InspectedAt,
                OfficerId = i.OfficerId,
                OfficerName = i.Officer != null ? i.Officer.FullName : "Unknown",
                FarmerName = i.Listing != null && i.Listing.Farmer != null ? i.Listing.Farmer.FullName : "Unknown",
                RegionName = i.Listing != null && i.Listing.Region != null ? i.Listing.Region.Name : "Unknown",
                ListingStatus = i.Listing != null ? i.Listing.Status : "Unknown",
                HasDiscrepancy = i.Listing != null && !string.Equals(i.Listing.ClaimedGrade, i.ConfirmedGrade, StringComparison.OrdinalIgnoreCase),
                Photos = i.Photos.Select(p => new InspectionPhotoDto
                {
                    Id = p.Id,
                    Url = p.Url,
                    UploadedAt = p.UploadedAt
                }).ToList()
            })
            .ToListAsync();
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

        var inspection = await _context.Inspections
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Crop)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Farmer)
            .Include(i => i.Listing)
                .ThenInclude(l => l!.Region)
            .Include(i => i.Photos)
            .Include(i => i.Officer)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection == null)
        {
            throw new KeyNotFoundException($"Inspection with ID {id} was not found.");
        }

        var officer = await _context.Users.FindAsync(officerId);
        if (officer == null)
        {
            throw new KeyNotFoundException($"Officer with ID {officerId} was not found.");
        }

        var oldGrade = inspection.ConfirmedGrade;
        var oldNotes = inspection.Notes;

        inspection.ConfirmedGrade = request.ConfirmedGrade;
        inspection.Notes = request.Notes;
        inspection.UpdatedAt = DateTime.UtcNow;

        if (request.PhotoUrls != null && request.PhotoUrls.Count > 0)
        {
            foreach (var url in request.PhotoUrls)
            {
                if (!string.IsNullOrWhiteSpace(url) && !inspection.Photos.Any(p => p.Url == url))
                {
                    inspection.Photos.Add(new InspectionPhoto
                    {
                        Id = Guid.NewGuid(),
                        InspectionId = inspection.Id,
                        Url = url,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // FR13: Record immutable audit entry
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = officerId,
            Action = "AmendInspection",
            EntityType = "Inspection",
            EntityId = inspection.Id,
            Timestamp = DateTime.UtcNow,
            Details = JsonSerializer.Serialize(new
            {
                PreviousGrade = oldGrade,
                NewGrade = request.ConfirmedGrade,
                PreviousNotes = oldNotes,
                NewNotes = request.Notes,
                Reason = request.ReasonForAmendment,
                AmendedBy = officer.FullName
            })
        };
        _context.AuditLogs.Add(audit);

        await _context.SaveChangesAsync();

        return new InspectionResponseDto
        {
            Id = inspection.Id,
            ListingId = inspection.ListingId,
            CropName = inspection.Listing?.Crop?.Name ?? "Unknown",
            Quantity = inspection.Listing?.Quantity ?? 0,
            Unit = inspection.Listing?.Unit ?? "kg",
            ClaimedGrade = inspection.Listing?.ClaimedGrade ?? string.Empty,
            ConfirmedGrade = inspection.ConfirmedGrade,
            Notes = inspection.Notes,
            InspectedAt = inspection.InspectedAt,
            OfficerId = officer.Id,
            OfficerName = officer.FullName,
            FarmerName = inspection.Listing?.Farmer?.FullName ?? "Unknown",
            RegionName = inspection.Listing?.Region?.Name ?? "Unknown",
            ListingStatus = inspection.Listing?.Status ?? "Unknown",
            HasDiscrepancy = inspection.Listing != null && !string.Equals(inspection.Listing.ClaimedGrade, inspection.ConfirmedGrade, StringComparison.OrdinalIgnoreCase),
            Photos = inspection.Photos.Select(p => new InspectionPhotoDto
            {
                Id = p.Id,
                Url = p.Url,
                UploadedAt = p.UploadedAt
            }).ToList()
        };
    }

    public async Task<List<GradeDiscrepancyDto>> GetDiscrepanciesAsync(bool? onlyUnresolved = true)
    {
        var query = _context.GradeDiscrepancyFlags
            .Include(f => f.Listing)
                .ThenInclude(l => l!.Crop)
            .Include(f => f.Listing)
                .ThenInclude(l => l!.Farmer)
            .Include(f => f.ResolvedByOfficer)
            .AsNoTracking();

        if (onlyUnresolved == true)
        {
            query = query.Where(f => !f.ResolvedAt.HasValue);
        }

        return await query
            .OrderByDescending(f => f.FlaggedAt)
            .Select(f => new GradeDiscrepancyDto
            {
                Id = f.Id,
                ListingId = f.ListingId,
                CropName = f.Listing != null && f.Listing.Crop != null ? f.Listing.Crop.Name : "Unknown",
                FarmerName = f.Listing != null && f.Listing.Farmer != null ? f.Listing.Farmer.FullName : "Unknown",
                Quantity = f.Listing != null ? f.Listing.Quantity : 0,
                Unit = f.Listing != null ? f.Listing.Unit : "kg",
                ClaimedGrade = f.ClaimedGrade,
                ConfirmedGrade = f.ConfirmedGrade,
                FlaggedAt = f.FlaggedAt,
                ResolvedAt = f.ResolvedAt,
                ResolutionNotes = f.ResolutionNotes,
                ResolvedByOfficerName = f.ResolvedByOfficer != null ? f.ResolvedByOfficer.FullName : null
            })
            .ToListAsync();
    }

    public async Task<GradeDiscrepancyDto> ResolveDiscrepancyAsync(Guid flagId, ResolveDiscrepancyRequest request, Guid officerId)
    {
        var flag = await _context.GradeDiscrepancyFlags
            .Include(f => f.Listing)
                .ThenInclude(l => l!.Crop)
            .Include(f => f.Listing)
                .ThenInclude(l => l!.Farmer)
            .FirstOrDefaultAsync(f => f.Id == flagId);

        if (flag == null)
        {
            throw new KeyNotFoundException($"Grade discrepancy flag with ID {flagId} was not found.");
        }

        var officer = await _context.Users.FindAsync(officerId);
        if (officer == null)
        {
            throw new KeyNotFoundException($"Officer with ID {officerId} was not found.");
        }

        flag.ResolvedAt = DateTime.UtcNow;
        flag.ResolutionNotes = request.ResolutionNotes;
        flag.ResolvedByOfficerId = officerId;
        flag.UpdatedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = officerId,
            Action = "ResolveGradeDiscrepancy",
            EntityType = "GradeDiscrepancyFlag",
            EntityId = flag.Id,
            Timestamp = DateTime.UtcNow,
            Details = JsonSerializer.Serialize(new
            {
                ListingId = flag.ListingId,
                ClaimedGrade = flag.ClaimedGrade,
                ConfirmedGrade = flag.ConfirmedGrade,
                ResolutionNotes = request.ResolutionNotes,
                ResolvedBy = officer.FullName
            })
        });

        // Notify farmer
        if (flag.Listing != null)
        {
            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = flag.Listing.FarmerId,
                Title = "Grade Discrepancy Resolved",
                Message = $"The grade discrepancy on your listing for {flag.Listing.Crop?.Name ?? "produce"} was resolved. Notes: {request.ResolutionNotes}",
                Type = "Info",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

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

    // FR5 Gate: A listing becomes Published ONLY after passing inspection AND officer approval
    public async Task<ListingSummaryDto> PublishListingWithGateCheckAsync(Guid listingId, Guid officerId)
    {
        var listing = await _context.Listings
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

        var officer = await _context.Users.FindAsync(officerId);
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

        // FR5 Rule 3: Unresolved discrepancies must be resolved or acknowledged
        var openDiscrepancy = listing.GradeDiscrepancyFlags.FirstOrDefault(f => !f.ResolvedAt.HasValue);
        if (openDiscrepancy != null)
        {
            throw new InvalidOperationException($"Quality Verification Gate Blocked: There is an active unresolved grade discrepancy (Claimed: {openDiscrepancy.ClaimedGrade} vs Confirmed: {openDiscrepancy.ConfirmedGrade}). Resolve the discrepancy before publishing.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            listing.Status = ListingStatus.Published;
            listing.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = officerId,
                Action = "PublishListingApproved",
                EntityType = "Listing",
                EntityId = listing.Id,
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    ListingId = listing.Id,
                    Crop = listing.Crop?.Name,
                    ConfirmedGrade = latestInspection.ConfirmedGrade,
                    ApprovedByOfficer = officer.FullName
                })
            });

            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = listing.FarmerId,
                Title = "Listing Published to Marketplace",
                Message = $"Your listing for {listing.Crop?.Name ?? "produce"} ({listing.Quantity}{listing.Unit}, {latestInspection.ConfirmedGrade}) has been verified and published! It is now visible to buyers.",
                Type = "Success",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ListingSummaryDto
            {
                Id = listing.Id,
                FarmerId = listing.FarmerId,
                FarmerName = listing.Farmer?.FullName ?? "Unknown",
                FarmerPhone = listing.Farmer?.Phone ?? string.Empty,
                CropId = listing.CropId,
                CropName = listing.Crop?.Name ?? "Unknown",
                Category = listing.Crop?.Category ?? "Unknown",
                RegionId = listing.RegionId,
                RegionName = listing.Region?.Name ?? "Unknown",
                Quantity = listing.Quantity,
                Unit = listing.Unit,
                ClaimedGrade = listing.ClaimedGrade,
                LatestConfirmedGrade = latestInspection.ConfirmedGrade,
                PickupWindowStart = listing.PickupWindowStart,
                PickupWindowEnd = listing.PickupWindowEnd,
                Status = listing.Status,
                MinPrice = listing.MinPrice,
                CreatedAt = listing.CreatedAt,
                InspectionCount = listing.Inspections.Count,
                HasUnresolvedDiscrepancy = false,
                ListingPhotos = listing.Photos.Select(p => p.Url).ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ListingSummaryDto>> GetPendingListingsForInspectionAsync()
    {
        var listings = await _context.Listings
            .Where(l => l.Status == ListingStatus.PendingApproval || l.Status == ListingStatus.Draft)
            .Include(l => l.Crop)
            .Include(l => l.Farmer)
            .Include(l => l.Region)
            .Include(l => l.Photos)
            .Include(l => l.Inspections)
            .Include(l => l.GradeDiscrepancyFlags)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return listings.Select(l =>
        {
            var latestInspection = l.Inspections.OrderByDescending(i => i.InspectedAt).FirstOrDefault();
            var hasUnresolvedDiscrepancy = l.GradeDiscrepancyFlags.Any(f => !f.ResolvedAt.HasValue);

            return new ListingSummaryDto
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
                LatestConfirmedGrade = latestInspection?.ConfirmedGrade,
                PickupWindowStart = l.PickupWindowStart,
                PickupWindowEnd = l.PickupWindowEnd,
                Status = l.Status,
                MinPrice = l.MinPrice,
                CreatedAt = l.CreatedAt,
                InspectionCount = l.Inspections.Count,
                HasUnresolvedDiscrepancy = hasUnresolvedDiscrepancy,
                ListingPhotos = l.Photos.Select(p => p.Url).ToList()
            };
        }).ToList();
    }

    public async Task<QualityDashboardStatsDto> GetDashboardStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var pendingInspections = await _context.Listings.CountAsync(l => l.Status == ListingStatus.PendingApproval && !l.Inspections.Any());
        var completedToday = await _context.Inspections.CountAsync(i => i.InspectedAt >= today);
        var totalInspections = await _context.Inspections.CountAsync();
        var activeDiscrepancies = await _context.GradeDiscrepancyFlags.CountAsync(f => !f.ResolvedAt.HasValue);
        var publishedListings = await _context.Listings.CountAsync(l => l.Status == ListingStatus.Published);
        
        var gradeACount = await _context.Inspections.CountAsync(i => i.ConfirmedGrade == QualityGrade.GradeA);
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
}
