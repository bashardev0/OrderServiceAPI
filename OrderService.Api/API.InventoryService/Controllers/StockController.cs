using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Business.Inventory;
using OrderService.Domain.Inventory;
using OrderService.Domain.Responses;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Api.API.InventoryService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/inventory/stock")]
    public sealed class StockController : ControllerBase
    {
        private readonly IInventoryService _svc;
        public StockController(IInventoryService svc) => _svc = svc;

        [HttpPost]
        public async Task<IActionResult> CreateStock([FromBody] StockCreateRequest req, CancellationToken ct)
        {
            var actor = User.Identity?.Name
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? "system";
            var resp = await _svc.CreateStockAsync(req, actor, ct);
            return ToResult(resp);
        }

        // GET all
        [HttpGet]
        public async Task<IActionResult> GetStocks(CancellationToken ct)
        {
            var resp = await _svc.GetStocksAsync(ct);
            return ToResult(resp);
        }

        // GET by id
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetStock(long id, CancellationToken ct)
        {
            var resp = await _svc.GetStockAsync(id, ct);
            return ToResult(resp);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateStock(long id, [FromBody] StockUpdateRequest req, CancellationToken ct)
        {
            var actor = User.Identity?.Name
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? "system";
            var resp = await _svc.UpdateStockAsync(id, req, actor, ct);
            return ToResult(resp);
        }

        [Authorize(Policy = "RequireManagerOrAdmin")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteStock(long id, CancellationToken ct)
        {
            var actor = User.Identity?.Name
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? "system";
            var resp = await _svc.DeleteStockAsync(id, actor, ct);
            return ToResult(resp);
        }

        private IActionResult ToResult<T>(BaseResponse<T> resp) =>
            resp.ErrorCode switch
            {
                0 => Ok(resp),
                400 => BadRequest(resp),
                401 => Unauthorized(resp),
                403 => Forbid(),
                404 => NotFound(resp),
                _ => StatusCode(500, resp),
            };
    }
}
