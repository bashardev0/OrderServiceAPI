using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderService.Domain.Entities;

namespace OrderService.Persistence
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _db;

        public OrderRepository(AppDbContext db)
        {
            _db = db;
        }

        // -------------------------------------------------
        // EF Core methods
        // -------------------------------------------------
        public async Task AddAsync(Order order, CancellationToken ct) =>
            await _db.Orders.AddAsync(order, ct);

        public async Task<Order?> GetByIdAsync(long id, CancellationToken ct) =>
            await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems.Where(i => !i.IsDeleted))
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct);

        public void Update(Order order)
        {
            _db.Attach(order);
            var entry = _db.Entry(order);
            entry.Property(x => x.Status).IsModified = true;
            entry.Property(x => x.TotalAmount).IsModified = true;
            entry.Property(x => x.UpdatedBy).IsModified = true;
            entry.Property(x => x.UpdatedDate).IsModified = true;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken ct)
        {
            var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
            return conn;
        }

        // -------------------------------------------------
        // Dapper Stored Procedures
        // -------------------------------------------------
        public async Task<string> CreateViaProcAsync(
            long customerId,
            IEnumerable<(long productId, int qty, decimal unitPrice)> items,
            string createdBy,
            CancellationToken ct = default)
        {
            var itemsJson = JsonSerializer.Serialize(items.Select(x => new
            {
                product_id = x.productId,
                qty = x.qty,
                unit_price = x.unitPrice
            }));

            await using var conn = await OpenConnectionAsync(ct);
            const string sql = "SELECT \"order\".sp_create_order(@p_customer_id, @p_items::jsonb, @p_created_by)";
            var res = await conn.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(sql,
                    new { p_customer_id = customerId, p_items = itemsJson, p_created_by = createdBy },
                    cancellationToken: ct));

            return res ?? "{\"errorCode\":1,\"message\":\"null result\"}";
        }

        public async Task<string> GetViaProcAsync(long id, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct);
            const string sql = "SELECT \"order\".sp_get_order(@p_id)";
            var res = await conn.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(sql, new { p_id = id }, cancellationToken: ct));

            return res ?? "{\"errorCode\":1,\"message\":\"null result\"}";
        }

        public async Task<string> UpdateStatusViaProcAsync(
            long id, string status, string updatedBy, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct);
            const string sql = "SELECT \"order\".sp_update_order_status(@p_id, @p_status, @p_updated_by)";
            var res = await conn.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(sql,
                    new { p_id = id, p_status = status, p_updated_by = updatedBy },
                    cancellationToken: ct));

            return res ?? "{\"errorCode\":1,\"message\":\"null result\"}";
        }

        // -------------------------------------------------
        // Soft Delete (EF Core)
        // -------------------------------------------------
        public async Task SoftDeleteOrderAsync(long orderId, string updatedBy, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null) return;

            order.IsDeleted = true;
            order.IsActive = false;
            order.UpdatedBy = updatedBy;
            order.UpdatedDate = DateTime.UtcNow;

            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    item.IsDeleted = true;
                    item.IsActive = false;
                    item.UpdatedBy = updatedBy;
                    item.UpdatedDate = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        // -------------------------------------------------
        // Optional: direct Dapper-based soft delete proc
        // -------------------------------------------------
        public async Task<string> DeleteViaProcAsync(long id, string updatedBy, CancellationToken ct = default)
        {
            await using var conn = await OpenConnectionAsync(ct);

            const string sql = @"
                UPDATE ""order"".""order""
                SET is_deleted = TRUE,
                    is_active  = FALSE,
                    updated_by = @updatedBy,
                    updated_date = NOW()
                WHERE id = @id
                RETURNING json_build_object(
                    'errorCode', 0,
                    'message',   'Order deleted',
                    'orderId',   id
                )::text;";

            var res = await conn.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(sql, new { id, updatedBy }, cancellationToken: ct));

            return res ?? "{\"errorCode\":1,\"message\":\"Delete failed\"}";
        }
    }
}
