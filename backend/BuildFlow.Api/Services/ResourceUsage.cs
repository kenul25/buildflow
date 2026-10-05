using BuildFlow.Api.DTOs;
using BuildFlow.Api.Models;

namespace BuildFlow.Api.Services;

public static class ResourceUsage
{
    public static decimal Count(ResourceRequestItem item) => item.ResourceCount ?? item.Quantity;
    public static decimal Hours(ResourceRequestItem item)
    {
        if (item.Kind == "Material" || item.ResourceCount == null) return 1;
        var factor = item.Unit.ToLowerInvariant() switch {
            "hours" => 1m, "shifts" or "days" or "mandays" => 8m, "weeks" => 40m,
            "workers" => 0m, _ => throw new ApiException(400, "invalid_unit", "Invalid resource usage unit.")
        };
        return factor == 0 ? 1 : item.Quantity * factor / item.ResourceCount.Value;
    }
    public static void Validate(IEnumerable<ResourceItemWriteDto> items)
    {
        foreach (var item in items.Where(x => x.Kind != "Material")) {
            if (item.ResourceCount == null) {
                if (decimal.Truncate(item.Quantity) != item.Quantity) throw new ApiException(400, "invalid_items", "Worker and equipment counts must be whole numbers.");
                continue;
            }
            var units = item.Kind == "Equipment" ? new[] { "shifts", "days", "hours", "weeks" } : new[] { "mandays", "workers", "hours", "shifts" };
            if (item.ResourceCount < 1 || item.ResourceCount > 10000 || !units.Contains(item.Unit.ToLowerInvariant())) throw new ApiException(400, "invalid_items", "Select a valid count and resource usage unit.");
            if (item.Unit.Equals("workers", StringComparison.OrdinalIgnoreCase) && item.Quantity != item.ResourceCount) throw new ApiException(400, "invalid_items", "Worker quantity must equal the worker count.");
        }
    }
}
