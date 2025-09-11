using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OrderService.Domain.Inventory
{
    public class InventoryStock
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        
        public string? Location { get; set; } = "Main";
        public int Qty { get; set; }

        // Audit
        public string CreatedBy { get; set; } = "system";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
