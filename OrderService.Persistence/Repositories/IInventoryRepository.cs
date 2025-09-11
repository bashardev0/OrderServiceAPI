using System.Threading;
using System.Threading.Tasks;
using OrderService.Domain.Inventory;

namespace OrderService.Persistence.Repositories
{
    public interface IInventoryRepository
    {
        // Items
        Task<string> ItemCreateAsync(ItemCreateRequest req, string actor, CancellationToken ct);
        Task<string> ItemUpdateAsync(long id, ItemUpdateRequest req, string actor, CancellationToken ct);
        Task<string> ItemDeleteAsync(long id, string actor, CancellationToken ct);
        Task<string> ItemGetAsync(long id, CancellationToken ct);
        Task<string> ItemGetAllAsync(CancellationToken ct);

        // Stock
        Task<string> StockCreateAsync(StockCreateRequest req, string actor, CancellationToken ct);
        Task<string> StockUpdateAsync(long id, StockUpdateRequest req, string actor, CancellationToken ct);
        Task<string> StockDeleteAsync(long id, string actor, CancellationToken ct);
        Task<string> StockGetAsync(long id, CancellationToken ct);
        Task<string> StockGetAllAsync(CancellationToken ct);
    }
}
