using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public sealed class WarehouseWriteDto
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Location { get; set; }
}

public sealed class MaterialWriteDto
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [Required, StringLength(80)] public string Category { get; set; } = "";
    [Required, StringLength(32)] public string Unit { get; set; } = "";
    [Range(typeof(decimal), "0", "999999999999999")] public decimal UnitPrice { get; set; }
    [Range(typeof(decimal), "0", "999999999999999")] public decimal CurrentStock { get; set; }
    [Range(typeof(decimal), "0", "999999999999999")] public decimal ReservedStock { get; set; }
    [Required] public Guid WarehouseId { get; set; }
}

public sealed class StockAdjustmentDto
{
    [Range(typeof(decimal), "0.001", "999999999999999")] public decimal Quantity { get; set; }
    [StringLength(200)] public string? Reference { get; set; }
}

public sealed class ReservationWriteDto
{
    [Range(typeof(decimal), "0.001", "999999999999999")] public decimal Quantity { get; set; }
    public Guid? ProjectId { get; set; }
    [Range(1, 10080)] public int DurationMinutes { get; set; } = 60;
}

public sealed record InventoryPageQuery(string? Search = null, Guid? WarehouseId = null, string? Sort = null, bool Desc = false, int Page = 1, int PageSize = 20);
public sealed record InventoryPageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record StockMovementDto(Guid Id, Guid MaterialId, string MaterialName, string Type, decimal Quantity, decimal StockAfter, Guid? ActorId, string? Reference, DateTimeOffset CreatedAt);
public sealed record InventoryReservationDetailsDto(Guid Id, Guid MaterialId, string MaterialName, decimal Quantity, Guid? ProjectId, string Status, DateTimeOffset ExpiresAt, DateTimeOffset? ReleasedAt, DateTimeOffset CreatedAt);

public sealed class MaterialRequirementDto
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = "";
    [Range(typeof(decimal), "0.001", "999999999999999")] public decimal RequiredQuantity { get; set; }
    [Required, StringLength(32)] public string Unit { get; set; } = "";
}

public sealed record WarehouseDto(Guid Id, string Name, string? Location);
public sealed record MaterialDto(Guid Id, string Name, string Category, string Unit, decimal UnitPrice, decimal CurrentStock, decimal ReservedStock, decimal AvailableStock, Guid WarehouseId, string WarehouseName);
public sealed record ReservationDto(Guid Id, Guid MaterialId, decimal Quantity, Guid? ProjectId, string Status, DateTimeOffset ExpiresAt);
public sealed record MaterialAvailabilityDto(string Name, string Unit, decimal Required, decimal Current, decimal Reserved, decimal Available, decimal Shortage, bool IsAvailable);
public sealed record MaterialAvailabilityReport(IReadOnlyList<MaterialAvailabilityDto> Items, bool IsFullyAvailable, DateTimeOffset GeneratedAt);
public sealed record LowStockAlertDto(Guid MaterialId, string MaterialName, string Unit, Guid WarehouseId, decimal AvailableStock, decimal Threshold, string Severity, string Message);