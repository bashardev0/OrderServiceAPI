using System;

namespace OrderService.Domain.Entities
{
    public class OrderItem
    {
        public long Id { get; set; }
        public long OrderId { get; set; }

        // Navigation back to Order
        public Order Order { get; set; }

        // Product + pricing
        public long ProductId { get; set; }
        public int Qty { get; set; } = 0;
        public decimal UnitPrice { get; set; } = 0;

        // Computed column in DB (qty * unit_price)
        public decimal LineTotal { get; set; }

        // Audit fields
        public string CreatedBy { get; set; } = "system";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Soft delete flags
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
