namespace BuildFlow.Api.Models;

public sealed class Warehouse : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public ICollection<Material> Materials { get; set; } = new List<Material>();
}

public sealed class Material : BaseEntity
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReservedStock { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public decimal AvailableStock => CurrentStock - ReservedStock;

    public void ValidateStock()
    {
        if (CurrentStock < 0 || ReservedStock < 0 || ReservedStock > CurrentStock)
            throw new InvalidOperationException("Current stock and reserved stock must be non-negative, and reserved stock cannot exceed current stock.");
    }
}

public sealed class InventoryReservation : BaseEntity
{
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public decimal Quantity { get; set; }
    public Guid? ProjectId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}

public sealed class StockMovement : BaseEntity
{
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public string Type { get; set; } = "Receive";
    public decimal Quantity { get; set; }
    public decimal StockAfter { get; set; }
    public Guid? ActorId { get; set; }
    public string? Reference { get; set; }
}