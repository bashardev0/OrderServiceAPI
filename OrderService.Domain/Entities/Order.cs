using OrderService.Domain.Entities;

public class Order
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Status { get; set; } = "NEW";
    public decimal TotalAmount { get; set; }

    public string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public List<OrderItem> OrderItems { get; set; } = new();
}
