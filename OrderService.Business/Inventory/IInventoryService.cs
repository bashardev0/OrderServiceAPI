using System.Threading;
using System.Threading.Tasks;
using OrderService.Domain.Inventory;
using OrderService.Domain.Responses;

namespace OrderService.Business.Inventory
{
    public interface IInventoryService
    {
        // Items
        Task<BaseResponse<object>> CreateItemAsync(ItemCreateRequest req, string actor, CancellationToken ct);
        Task<BaseResponse<object>> UpdateItemAsync(long id, ItemUpdateRequest req, string actor, CancellationToken ct);
        Task<BaseResponse<object>> DeleteItemAsync(long id, string actor, CancellationToken ct);
        Task<BaseResponse<object>> GetItemAsync(long id, CancellationToken ct);
        Task<BaseResponse<object>> GetItemsAsync(CancellationToken ct);

        // Stock
        Task<BaseResponse<object>> CreateStockAsync(StockCreateRequest req, string actor, CancellationToken ct);
        Task<BaseResponse<object>> UpdateStockAsync(long id, StockUpdateRequest req, string actor, CancellationToken ct);
        Task<BaseResponse<object>> DeleteStockAsync(long id, string actor, CancellationToken ct);
        Task<BaseResponse<object>> GetStockAsync(long id, CancellationToken ct);
        Task<BaseResponse<object>> GetStocksAsync(CancellationToken ct);
    }
}
