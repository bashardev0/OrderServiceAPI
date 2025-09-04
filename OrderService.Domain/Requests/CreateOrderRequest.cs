using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Domain.Requests;

public class CreateOrderRequest
{
    [Required]
    public long CustomerId { get; set; }

    [Required]
    public string CreatedBy { get; set; }

    [Required]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    [Required]
    public long ProductId { get; set; }

    [DefaultValue(0)]   // 👈 Swagger shows 0 by default
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Qty { get; set; } = 0;

    [DefaultValue(0.0)] // 👈 Swagger shows 0.0 by default
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be at least 0.01.")]
    public decimal UnitPrice { get; set; } = 0.0m;
}
