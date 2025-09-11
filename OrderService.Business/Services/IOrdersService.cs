using OrderService.Domain.Responses;
using OrderService.Domain.Requests;

namespace OrderService.Business.Services;

public interface IOrdersService
{
    Task<BaseResponse<object>> CreateAsync(CreateOrderRequest request, CancellationToken ct);
    Task<BaseResponse<OrderDto>> GetAsync(long id, CancellationToken ct);
    Task<BaseResponse<object>> UpdateAsync(long id, UpdateOrderRequest request, CancellationToken ct);
    Task<BaseResponse<object>> DeleteAsync(long id, string updatedBy, CancellationToken ct);
}
