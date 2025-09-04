namespace OrderService.Domain.Entities;

public class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public Order Order { get; set; }

    public long ProductId { get; set; }
    public int Qty { get; set; } = 0;
    public decimal UnitPrice { get; set; } = 0;
    public decimal LineTotal { get; set; }

    public string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}
