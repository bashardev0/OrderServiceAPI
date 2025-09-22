using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using OrderService.Business.Inventory;
using OrderService.Domain.Responses;

namespace OrderService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemSearchController : ControllerBase
    {
        private readonly IItemSearchService _service;

        public ItemSearchController(IItemSearchService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET api/itemsearch?query=twix
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
        {
            var response = await _service.SearchAsync(query, ct);
            return ToResult(response);
        }
        
        private IActionResult ToResult(BaseResponse<object> resp) =>
            resp.ErrorCode switch
            {
                0 => Ok(resp),
                400 => BadRequest(resp),
                404 => NotFound(resp),
                _ => StatusCode(500, resp)
            };
    }
}
