using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("order");

            // -------- Order --------
            modelBuilder.Entity<Order>(e =>
            {
                e.ToTable("order");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.CustomerId).HasColumnName("customer_id");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.TotalAmount)
                    .HasColumnName("total_amount")
                    .HasColumnType("numeric(18,2)");

                e.Property(x => x.CreatedBy).HasColumnName("created_by");
                e.Property(x => x.CreatedDate).HasColumnName("created_date");
                e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
                e.Property(x => x.UpdatedDate).HasColumnName("updated_date");
                e.Property(x => x.IsActive).HasColumnName("is_active");
                e.Property(x => x.IsDeleted).HasColumnName("is_deleted");

                e.HasMany(x => x.OrderItems)
                    .WithOne(i => i.Order)
                    .HasForeignKey(i => i.OrderId);
            });

            // -------- OrderItem --------
            modelBuilder.Entity<OrderItem>(e =>
            {
                e.ToTable("order_items");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.OrderId).HasColumnName("order_id");
                e.Property(x => x.ProductId).HasColumnName("product_id");
                e.Property(x => x.Qty).HasColumnName("qty");
                e.Property(x => x.UnitPrice)
                    .HasColumnName("unit_price")
                    .HasColumnType("numeric(18,2)");

                e.Property(x => x.LineTotal)
                    .HasColumnName("line_total")
                    .HasColumnType("numeric(18,2)")
                    .HasComputedColumnSql("(qty * unit_price)", stored: true);

                e.Property(x => x.CreatedBy).HasColumnName("created_by");
                e.Property(x => x.CreatedDate).HasColumnName("created_date");
                e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
                e.Property(x => x.UpdatedDate).HasColumnName("updated_date");
                e.Property(x => x.IsActive).HasColumnName("is_active");
                e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            });
            // ----- InventoryItem -----
            modelBuilder.Entity<OrderService.Domain.Inventory.InventoryItem>(e =>
            {
                e.ToTable("items", "inventory");
                e.HasKey(x => x.Id);

                e.Property(x => x.Name).HasColumnName("Location");
                e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,2)");

                e.Property(x => x.CreatedBy).HasColumnName("created_by");
                e.Property(x => x.CreatedDate).HasColumnName("created_date");
                e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
                e.Property(x => x.UpdatedDate).HasColumnName("updated_date");
                e.Property(x => x.IsActive).HasColumnName("is_active");
                e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            });

            // ----- InventoryStock -----
            modelBuilder.Entity<OrderService.Domain.Inventory.InventoryStock>(e =>
            {
                e.ToTable("stock", "inventory");
                e.HasKey(x => x.Id);

                e.Property(x => x.ItemId).HasColumnName("item_id");
                e.Property(x => x.Location).HasColumnName("Location");
                e.Property(x => x.Qty).HasColumnName("qty");

                e.Property(x => x.CreatedBy).HasColumnName("created_by");
                e.Property(x => x.CreatedDate).HasColumnName("created_date");
                e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
                e.Property(x => x.UpdatedDate).HasColumnName("updated_date");
                e.Property(x => x.IsActive).HasColumnName("is_active");
                e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            });

        }
    }
}
