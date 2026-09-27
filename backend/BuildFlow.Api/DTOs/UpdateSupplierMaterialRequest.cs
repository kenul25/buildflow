using System.ComponentModel.DataAnnotations;

namespace BuildFlow.Api.DTOs;

public class UpdateSupplierMaterialRequest
{
    [Range(0.001, double.MaxValue)]
    public decimal AvailableQuantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int LeadTimeDays { get; set; }
}