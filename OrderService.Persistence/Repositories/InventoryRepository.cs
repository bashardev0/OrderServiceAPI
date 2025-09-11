using System;
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

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
            return conn;
        }

        // -------- Items --------
        public async Task<string> ItemCreateAsync(ItemCreateRequest req, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_item_create(@name,@price,@actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql,
                    new { name = req.Name, price = req.UnitPrice, actor });
            }
            catch (Exception ex)
            {
                return $"{{\"errorCode\":500,\"message\":\"ItemCreate failed: {ex.Message}\"}}";
            }
        }

        public async Task<string> ItemUpdateAsync(long id, ItemUpdateRequest req, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_item_update(@id,@name,@price,@actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql,
                    new { id, name = req.Name, price = req.UnitPrice, actor });
            }
            catch (Exception ex)
            {
                return $"{{\"errorCode\":500,\"message\":\"ItemUpdate failed: {ex.Message}\"}}";
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
                return $"{{\"errorCode\":500,\"message\":\"ItemDelete failed: {ex.Message}\"}}";
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
                return $"{{\"errorCode\":500,\"message\":\"ItemGet failed: {ex.Message}\"}}";
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
                return $"{{\"errorCode\":500,\"message\":\"ItemGetAll failed: {ex.Message}\"}}";
            }
        }

        // -------- Stock --------
        public async Task<string> StockCreateAsync(StockCreateRequest req, string actor, CancellationToken ct)
        {
            try
            {
                const string sql = @"SELECT inventory.sp_stock_create(@itemId,@location,@Product Name,@actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql,
                    new { itemId = req.ItemId, Location = req.Location, qty = req.Qty, actor });
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
                const string sql = @"SELECT inventory.sp_stock_update(@id,@location,@qty,@actor)";
                await using var conn = await OpenAsync(ct);
                return await conn.QueryFirstAsync<string>(sql,
                    new { id, Location = req.Location, qty = req.Qty, actor });
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
                const string sql = @"SELECT inventory.sp_stock_delete(@id,@actor)";
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
