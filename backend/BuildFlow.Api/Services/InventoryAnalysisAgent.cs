using BuildFlow.Api.DTOs;

namespace BuildFlow.Api.Services;

public sealed class InventoryAnalysisAgent(IInventoryService inventoryService)
{
    public Task<MaterialAvailabilityReport> AnalyzeAsync(IEnumerable<MaterialRequirementDto> requirements, CancellationToken ct) =>
        inventoryService.CheckAvailabilityAsync(requirements, ct);
}