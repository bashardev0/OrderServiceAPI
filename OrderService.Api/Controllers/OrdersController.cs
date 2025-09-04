using Microsoft.AspNetCore.Mvc;
using OrderService.Business;
using OrderService.Domain.Entities;
using OrderService.Domain.Requests;
using OrderService.Domain.Responses;


namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrdersService _svc;
    public OrdersController(IOrdersService svc) => _svc = svc;

    [HttpPost]
    public Task<BaseResponse<object>> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
        => _svc.CreateAsync(req, ct);

    [HttpGet("{id:long}")]
    public Task<BaseResponse<OrderDto>> Get(long id, CancellationToken ct)
        => _svc.GetAsync(id, ct);

    [HttpPut("{id:long}")]
    public Task<BaseResponse<object>> Update(long id, [FromBody] UpdateOrderRequest req, CancellationToken ct)
        => _svc.UpdateAsync(id, req, ct);

    [HttpDelete("{id:long}")]
    public Task<BaseResponse<object>> Delete(long id, [FromQuery] string updatedBy = "system", CancellationToken ct = default)
        => _svc.DeleteAsync(id, updatedBy, ct);
}