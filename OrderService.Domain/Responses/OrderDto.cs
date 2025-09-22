using System.ComponentModel; // 👈 needed for [DefaultValue]

namespace OrderService.Domain.Responses
{
    public class OrderDto
    {
        [DefaultValue(0)]
        public long Id { get; set; }

        [DefaultValue(0)]
        public long CustomerId { get; set; }

        [DefaultValue("PENDING")] // 👈 sensible default status
        public string Status { get; set; } = "PENDING";

        [DefaultValue(0.0)]
        public decimal TotalAmount { get; set; } = 0.0m;

        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        [DefaultValue(0)]
        public long Id { get; set; }

        [DefaultValue(0)]
        public long ProductId { get; set; }

        [DefaultValue(0)]
        public int quantity { get; set; } = 0;

        [DefaultValue(0.0)]
        public decimal UnitPrice { get; set; } = 0.0m;

        [DefaultValue(0.0)]
        public decimal LineTotal { get; set; } = 0.0m;
    }
}
