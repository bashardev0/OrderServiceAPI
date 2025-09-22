using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace OrderService.Persistence.Repositories
{
    public interface IItemSearchRepository
    {
        Task<string> SearchItemsAsync(string query, CancellationToken ct);
    }

    public sealed class ItemSearchRepository : IItemSearchRepository
    {
        private readonly AppDbContext _db;
        public ItemSearchRepository(AppDbContext db) => _db = db;

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
            return conn;
        }

        public async Task<string> SearchItemsAsync(string query, CancellationToken ct)
        {
            try
            {
                // Build a single JSON object in SQL so callers always get {errorCode,message,data}
                const string sql = @"
SELECT json_build_object(
  'errorCode', 0,
  'message',   'Search results',
  'data', COALESCE(
           json_agg(
             json_build_object(
               'id', i.id,
               'name', i.name,
               'unit_price', i.unit_price
             )
             ORDER BY i.id
           ),
           '[]'::json
         )
)::text
FROM inventory.items i
WHERE i.is_deleted = FALSE
  AND i.is_active  = TRUE
  AND i.name ILIKE @pattern;";

                await using var conn = await OpenAsync(ct);
                var result = await conn.QueryFirstOrDefaultAsync<string>(
                    sql, new { pattern = $"%{query}%" });

                // SQL always returns one row with the JSON string
                return result ?? "{\"errorCode\":0,\"message\":\"Search results\",\"data\":[]}";
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    errorCode = 500,
                    message = $"SearchItems failed: {ex.Message}"
                });
            }
        }
    }
}
