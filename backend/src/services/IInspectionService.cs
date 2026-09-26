using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using backend.src.dtos;

namespace backend.src.services;

public interface IInspectionService
{
    Task<InspectionResponseDto> RecordInspectionAsync(CreateInspectionRequest request, Guid officerId);
    Task<PagedResult<InspectionResponseDto>> GetInspectionsAsync(InspectionFilterParams filter);
    Task<InspectionResponseDto?> GetInspectionByIdAsync(Guid id);
    Task<List<InspectionResponseDto>> GetInspectionsByListingIdAsync(Guid listingId);
    Task<InspectionResponseDto> AmendInspectionAsync(Guid id, UpdateInspectionRequest request, Guid officerId);
    Task<List<GradeDiscrepancyDto>> GetDiscrepanciesAsync(bool? onlyUnresolved = true);
    Task<GradeDiscrepancyDto> ResolveDiscrepancyAsync(Guid flagId, ResolveDiscrepancyRequest request, Guid officerId);
    Task<ListingSummaryDto> PublishListingWithGateCheckAsync(Guid listingId, Guid officerId);
    Task<List<ListingSummaryDto>> GetPendingListingsForInspectionAsync();
    Task<QualityDashboardStatsDto> GetDashboardStatsAsync();
}
