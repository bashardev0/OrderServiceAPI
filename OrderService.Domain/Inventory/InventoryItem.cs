using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Domain.Inventory
{
    public class InventoryItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public decimal UnitPrice { get; set; }

        // Audit
        public string CreatedBy { get; set; } = "system";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
