namespace OrderService.Domain.Requests;

public class UpdateOrderRequest
{
    public string? Status { get; set; }
    public decimal? TotalAmount { get; set; }
    public string UpdatedBy { get; set; }
}
