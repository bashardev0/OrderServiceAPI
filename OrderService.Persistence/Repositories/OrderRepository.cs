using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace OrderService.Persistence
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OrderRepository> _logger; // ← added

        public OrderRepository(AppDbContext db, ILogger<OrderRepository> logger) // ← logger injected
        {
            _db = db;
            _logger = logger;
        }

        // -------------------------------------------------
        // EF Core methods
        // -------------------------------------------------
        public async Task AddAsync(Order order, CancellationToken ct) =>
            await _db.Orders.AddAsync(order, ct); // NOTE: if your DbSet is Orders (capital O), change back to _db.Orders

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
    IEnumerable<(long productId, int quantity, decimal unitPrice)> items,
    string createdBy,
    CancellationToken ct = default)
        {
            try
            {
                // Null/empty safe handling
                var itemsSeq = items ?? Enumerable.Empty<(long productId, int quantity, decimal unitPrice)>();
                var itemCount = itemsSeq.Count();
                if (itemCount == 0)
                    return "{\"errorCode\":400,\"message\":\"items collection is required\"}";

                // Build JSON payload
                var itemsJson = JsonSerializer.Serialize(
                    itemsSeq.Select(x => new
                    {
                        product_id = x.productId,
                        qty = x.quantity,
                        unit_price = x.unitPrice
                    })
                );

                await using var conn = await OpenConnectionAsync(ct);
                const string sql = "SELECT \"order\".sp_create_order(@p_customer_id, @p_items::jsonb, @p_created_by)";
                var res = await conn.QueryFirstOrDefaultAsync<string>(
                    new CommandDefinition(sql,
                        new { p_customer_id = customerId, p_items = itemsJson, p_created_by = createdBy },
                        cancellationToken: ct));

                return res ?? "{\"errorCode\":1,\"message\":\"null result\"}";
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    errorCode = 500,
                    message = $"CreateViaProc failed: {ex.Message}"
                });
            }
        }


        public async Task<string> GetViaProcAsync(long id, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("sp_get_order id={Id}", id);

                await using var conn = await OpenConnectionAsync(ct);
                const string sql = "SELECT \"order\".sp_get_order(@p_id)";
                var res = await conn.QueryFirstOrDefaultAsync<string>(
                    new CommandDefinition(sql, new { p_id = id }, cancellationToken: ct));

                return res ?? "{\"errorCode\":1,\"message\":\"null result\"}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetViaProcAsync failed id={Id}", id);
                return JsonSerializer.Serialize(new
                {
                    errorCode = 500,
                    message = $"GetViaProc failed: {ex.Message}"
                });
            }
        }

        public async Task<string> UpdateStatusViaProcAsync(
            long id, string status, string updatedBy, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("sp_update_order_status id={Id} status={Status}", id, status);

                await using var conn = await OpenConnectionAsync(ct);
                const string sql = "SELECT \"order\".sp_update_order_status(@p_id, @p_status, @p_updated_by)";
                var res = await conn.QueryFirstOrDefaultAsync<string>(
                    new CommandDefinition(sql,
                        new { p_id = id, p_status = status, p_updated_by = updatedBy },
                        cancellationToken: ct));

                return res ?? "{\"errorCode\":1,\"message\":\"null result\"}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateStatusViaProcAsync failed id={Id}", id);
                return JsonSerializer.Serialize(new
                {
                    errorCode = 500,
                    message = $"UpdateStatusViaProc failed: {ex.Message}"
                });
            }
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
            try
            {
                _logger.LogInformation("soft delete (proc) id={Id} by={User}", id, updatedBy);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteViaProcAsync failed id={Id}", id);
                return JsonSerializer.Serialize(new
                {
                    errorCode = 500,
                    message = $"DeleteViaProc failed: {ex.Message}"
                });
            }
        }
    }
}
