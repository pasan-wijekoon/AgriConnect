using AgriConnect.Api.Dtos;

namespace AgriConnect.Api.Services;

public interface IAgentClientService
{
    Task<AgentQualityValidationDto> EvaluateListingQualityAsync(Guid listingId, Guid officerId);
    Task<AgentWorkflowResponseDto?> GetLatestWorkflowForListingAsync(Guid listingId);
}
