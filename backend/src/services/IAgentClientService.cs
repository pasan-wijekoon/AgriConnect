using System;
using System.Threading.Tasks;
using backend.src.dtos;

namespace backend.src.services;

public interface IAgentClientService
{
    Task<AgentQualityValidationDto> EvaluateListingQualityAsync(Guid listingId, Guid officerId);
    Task<AgentWorkflowResponseDto?> GetLatestWorkflowForListingAsync(Guid listingId);
}
