using System;

namespace OrderService.Domain.Inventory
{
    // -------- Items --------
    public sealed class ItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        public string CreatedBy { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    public sealed class ItemCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }

    public sealed class ItemUpdateRequest
    {
        public string? Name { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    // -------- Stock --------
    public sealed class StockDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string? Location { get; set; }
        public int quantity { get; set; }

        public string CreatedBy { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    public sealed class StockCreateRequest
    {
        public long ItemId { get; set; }
        public string Location { get; set; } = "";
        public int Quantity { get; set; }
    }

    public sealed class StockUpdateRequest
    {
        public string Location { get; set; } = "";
        public int Quantity { get; set; }
    }
}
