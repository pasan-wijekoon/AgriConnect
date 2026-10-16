using AgriConnect.Api.Dtos;
using AgriConnect.Api.Models;

namespace AgriConnect.Api.Services;

public interface IInspectionService
{
    Task<InspectionResponseDto> RecordInspectionAsync(CreateInspectionRequest request, Guid officerId);
    Task<InspectionPagedResult<InspectionResponseDto>> GetInspectionsAsync(InspectionFilterParams filter);
    Task<InspectionResponseDto?> GetInspectionByIdAsync(Guid id);
    Task<List<InspectionResponseDto>> GetInspectionsByListingIdAsync(Guid listingId);
    Task<InspectionResponseDto> AmendInspectionAsync(Guid id, UpdateInspectionRequest request, Guid officerId);
    Task<List<GradeDiscrepancyDto>> GetDiscrepanciesAsync(bool? onlyUnresolved = true);
    Task<GradeDiscrepancyDto> ResolveDiscrepancyAsync(Guid flagId, ResolveDiscrepancyRequest request, Guid officerId);
    Task<ListingSummaryDto> PublishListingWithGateCheckAsync(Guid listingId, Guid officerId);
    Task<List<ListingSummaryDto>> GetPendingListingsForInspectionAsync();
    Task<List<ListingSummaryDto>> GetFarmerListingsAsync(Guid farmerId);
    Task<QualityDashboardStatsDto> GetDashboardStatsAsync();

    // For authorization checks: returns listing with FarmerId for ownership validation
    Task<Listing?> GetListingByIdAsync(Guid listingId);
}
