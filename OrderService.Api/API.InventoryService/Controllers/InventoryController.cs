using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Business.Inventory;
using OrderService.Domain.Inventory;
using OrderService.Domain.Responses;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Api.API.InventoryService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/inventory/items")]
    public sealed class InventoryController : ControllerBase
    {
        private readonly IInventoryService _svc;
        public InventoryController(IInventoryService svc) => _svc = svc;

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] ItemCreateRequest req, CancellationToken ct)
        {
            var actor = User.Identity?.Name
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? "system";
            var resp = await _svc.CreateItemAsync(req, actor, ct);
            return ToResult(resp);
        }

        // GET all
        [HttpGet]
        public async Task<IActionResult> GetItems(CancellationToken ct)
        {
            var resp = await _svc.GetItemsAsync(ct);
            return ToResult(resp);
        }

        // GET by id
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetItem(long id, CancellationToken ct)
        {
            var resp = await _svc.GetItemAsync(id, ct);
            return ToResult(resp);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateItem(long id, [FromBody] ItemUpdateRequest req, CancellationToken ct)
        {
            var actor = User.Identity?.Name
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? "system";
            var resp = await _svc.UpdateItemAsync(id, req, actor, ct);
            return ToResult(resp);
        }

        [Authorize(Policy = "RequireManagerOrAdmin")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteItem(long id, CancellationToken ct)
        {
            var actor = User.Identity?.Name
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? "system";
            var resp = await _svc.DeleteItemAsync(id, actor, ct);
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
