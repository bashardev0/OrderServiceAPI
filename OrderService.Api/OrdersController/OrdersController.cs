using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using OrderService.Business.Services;
using OrderService.Domain.Requests;
using OrderService.Domain.Responses;

namespace OrderService.Api.Controllers
{
    [Authorize] // all endpoints require a valid JWT
    [ApiController]
    [Route("api/[controller]")]
    public sealed class OrdersController : ControllerBase
    {
        private readonly IOrdersService _svc;

        public OrdersController(IOrdersService svc) => _svc = svc;

        /// <summary>Create a new order (any authenticated user).</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
        {
            var resp = await _svc.CreateAsync(req, ct);
            return ToActionResult(resp);
        }

        /// <summary>Get a single order by id (any authenticated user).</summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id, CancellationToken ct)
        {
            var resp = await _svc.GetAsync(id, ct);

            if (resp is null || resp.Data is null || resp.ErrorCode != 0)
                return NotFound(new { message = resp?.Message ?? "Not found", errorCode = resp?.ErrorCode ?? 404 });

            return Ok(resp);
        }

        /// <summary>Update an existing order (any authenticated user).</summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateOrderRequest req, CancellationToken ct)
        {
            var resp = await _svc.UpdateAsync(id, req, ct);
            return ToActionResult(resp);
        }

        /// <summary>Delete (soft-delete) an order. Only Admin or Manager can delete.</summary>
        [Authorize(Policy = "RequireManagerOrAdmin")]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            // take the actor from the JWT (preferred: unique_name/Name; fallback to sub; else "system")
            var updatedBy =
                User.Identity?.Name
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "system";

            var resp = await _svc.DeleteAsync(id, updatedBy, ct);
            return ToActionResult(resp);
        }

        // helper: map service response to HTTP responses
        private IActionResult ToActionResult<T>(BaseResponse<T> resp) =>
            resp.ErrorCode switch
            {
                0 => Ok(resp),
                400 => BadRequest(resp),
                401 => Unauthorized(resp),
                403 => Forbid(),
                404 => NotFound(resp),
                _ => StatusCode(500, resp)
            };
    }
}
