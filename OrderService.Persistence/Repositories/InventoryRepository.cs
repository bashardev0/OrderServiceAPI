using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderService.Domain.Inventory;

namespace OrderService.Persistence.Repositories
{
    public sealed class InventoryRepository : IInventoryRepository
    {
        private readonly AppDbContext _db;
        public InventoryRepository(AppDbContext db) => _db = db;

        // ---------- JSON helpers (always produce valid JSON) ----------
        private static string JsonOk(object? data = null, string message = "OK") =>
            JsonSerializer.Serialize(new { errorCode = 0, message, data });

        private static string JsonFail(int code, string message, object? data = null) =>
            JsonSerializer.Serialize(new { errorCode = code, message, data });

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
            return conn;
        }

        // ---------------- Items ----------------
        public async Task<string> ItemCreateAsync(ItemCreateRequest req, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_item_create(@name,@price,@actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new
                {
                    name = req.Name,
                    price = req.UnitPrice,
                    actor
                });
            }
            catch (Exception ex)
            {
                return JsonFail(500, $"ItemCreate failed: {ex.Message}");
            }
        }

        public async Task<string> ItemUpdateAsync(long id, ItemUpdateRequest req, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_item_update(@id,@name,@price,@actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new
                {
                    id,
                    name = req.Name,
                    price = req.UnitPrice,
                    actor
                });
            }
            catch (Exception ex)
            {
                return JsonFail(500, $"ItemUpdate failed: {ex.Message}");
            }
        }

        public async Task<string> ItemDeleteAsync(long id, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_item_delete(@id,@actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new { id, actor });
            }
            catch (Exception ex)
            {
                return JsonFail(500, $"ItemDelete failed: {ex.Message}");
            }
        }

        public async Task<string> ItemGetAsync(long id, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_item_get(@id)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new { id });
            }
            catch (Exception ex)
            {
                return JsonFail(500, $"ItemGet failed: {ex.Message}");
            }
        }

        public async Task<string> ItemGetAllAsync(CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_item_get_all()";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql);
            }
            catch (Exception ex)
            {
                return JsonFail(500, $"ItemGetAll failed: {ex.Message}");
            }
        }

        // ---------------- Stock ----------------
        public async Task<string> StockCreateAsync(StockCreateRequest req, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_stock_create(@itemId, @location, @quantity, @actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new
                {
                    itemId = req.ItemId,
                    location = req.Location,
                    quantity = req.Quantity,
                    actor
                });
            }
            catch (Exception ex)
            {
                return $"{{\"errorCode\":500,\"message\":\"StockCreate failed: {ex.Message}\"}}";
            }
        }

        public async Task<string> StockUpdateAsync(long id, StockUpdateRequest req, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_stock_update(@id, @location, @quantity, @actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new
                {
                    id,
                    location = req.Location,
                    quantity = req.Quantity,
                    actor
                });
            }
            catch (Exception ex)
            {
                return $"{{\"errorCode\":500,\"message\":\"StockUpdate failed: {ex.Message}\"}}";
            }
        }

        public async Task<string> StockDeleteAsync(long id, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_stock_delete(@id, @actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new { id, actor });
            }
            catch (Exception ex)
            {
                return $"{{\"errorCode\":500,\"message\":\"StockDelete failed: {ex.Message}\"}}";
            }
        }


        public async Task<string> StockGetAsync(long id, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_stock_get(@id)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql, new { id });
            }
            catch (Exception ex)
            {
                return $"{{\"errorCode\":500,\"message\":\"StockGet failed: {ex.Message}\"}}";
            }
        }

        public async Task<string> StockGetAllAsync(CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_stock_get_all()";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql);
            }
            catch (Exception ex)
            {
                return $"{{\"errorCode\":500,\"message\":\"StockGetAll failed: {ex.Message}\"}}";
            }
        }
    }
}
