// InventoryService.cs
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Inventory;
using OrderService.Domain.Responses;
using OrderService.Persistence.Repositories;

namespace OrderService.Business.Inventory
{
    public sealed class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _repo;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IInventoryRepository repo, ILogger<InventoryService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        // -------- Items --------
        public async Task<BaseResponse<object>> CreateItemAsync(ItemCreateRequest req, string actor, CancellationToken ct)
            => Parse(await _repo.ItemCreateAsync(req, actor, ct));

        public async Task<BaseResponse<object>> UpdateItemAsync(long id, ItemUpdateRequest req, string actor, CancellationToken ct)
            => Parse(await _repo.ItemUpdateAsync(id, req, actor, ct));

        public async Task<BaseResponse<object>> DeleteItemAsync(long id, string actor, CancellationToken ct)
            => Parse(await _repo.ItemDeleteAsync(id, actor, ct));

        public async Task<BaseResponse<object>> GetItemAsync(long id, CancellationToken ct)
            => Parse(await _repo.ItemGetAsync(id, ct));

        public async Task<BaseResponse<object>> GetItemsAsync(CancellationToken ct)
            => Parse(await _repo.ItemGetAllAsync(ct));

       
        // -------- Stock --------
        public async Task<BaseResponse<object>> CreateStockAsync(StockCreateRequest req, string actor, CancellationToken ct)
            => Parse(await _repo.StockCreateAsync(req, actor, ct));

        public async Task<BaseResponse<object>> UpdateStockAsync(long id, StockUpdateRequest req, string actor, CancellationToken ct)
            => Parse(await _repo.StockUpdateAsync(id, req, actor, ct));

        public async Task<BaseResponse<object>> DeleteStockAsync(long id, string actor, CancellationToken ct)
            => Parse(await _repo.StockDeleteAsync(id, actor, ct));

        public async Task<BaseResponse<object>> GetStockAsync(long id, CancellationToken ct)
            => Parse(await _repo.StockGetAsync(id, ct));

        public async Task<BaseResponse<object>> GetStocksAsync(CancellationToken ct)
            => Parse(await _repo.StockGetAllAsync(ct));

        /// <summary>
        /// Parses repository JSON into BaseResponse&lt;object&gt;.
        /// Never throws. Returns 400 for invalid JSON to avoid 500s from the controller.
        /// </summary>
        private BaseResponse<object> Parse(string json)
        {
            // Empty/null -> treat as server-side problem from repo
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogError("Repository returned empty response.");
                return new BaseResponse<object>
                {
                    ErrorCode = 500,
                    Message = "Repository returned empty response.",
                    Data = null
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var resp = new BaseResponse<object>
                {
                    ErrorCode = root.TryGetProperty("errorCode", out var ec) && ec.TryGetInt32(out var eci) ? eci : 0,
                    Message = root.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : ""
                };

                if (root.TryGetProperty("data", out var d))
                {
                    resp.Data = JsonSerializer.Deserialize<object>(d.GetRawText());
                }

                return resp;
            }
            catch (JsonException ex)
            {
                var preview = json.Length > 300 ? json[..300] + "..." : json;
                _logger.LogError(ex, "Invalid JSON returned from repository. Preview: {Preview}", preview);

                // Return 400 so StockController.ToResult maps this to BadRequest (not 500)
                return new BaseResponse<object>
                {
                    ErrorCode = 400,
                    Message = $"Invalid JSON returned from repository: {ex.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                var preview = json.Length > 300 ? json[..300] + "..." : json;
                _logger.LogError(ex, "Unexpected error parsing repository response. Preview: {Preview}", preview);

                return new BaseResponse<object>
                {
                    ErrorCode = 500,
                    Message = "Unexpected error parsing repository response.",
                    Data = null
                };
            }
        }
    }
}
