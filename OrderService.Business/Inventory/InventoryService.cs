using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrderService.Domain.Inventory;
using OrderService.Domain.Responses;
using OrderService.Persistence.Repositories;

namespace OrderService.Business.Inventory
{
    public sealed class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _repo;
        public InventoryService(IInventoryRepository repo) => _repo = repo;

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

        private static BaseResponse<object> Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var resp = new BaseResponse<object>();
            resp.ErrorCode = root.TryGetProperty("errorCode", out var ec) && ec.TryGetInt32(out var eci) ? eci : 1;
            resp.Message = root.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : "";

            if (root.TryGetProperty("data", out var d))
            {
                resp.Data = JsonSerializer.Deserialize<object>(d.GetRawText());
            }

            return resp;
        }
    }
}
