using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderService.Persistence;
using OrderService.Domain.Requests;
using OrderService.Domain.Responses;
using OrderService.Domain.Entities;
using OrderService.Business.Services;
using Microsoft.Extensions.Logging;

namespace OrderService.Business
{
    public sealed class OrdersService : IOrdersService
    {
        private readonly IOrderRepository _orders;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<OrdersService> _logger;

        public OrdersService(IOrderRepository orders, IUnitOfWork uow, ILogger<OrdersService> logger)
        {
            _orders = orders;
            _uow = uow;
            _logger = logger;
        }

        public async Task<BaseResponse<object>> CreateAsync(CreateOrderRequest request, CancellationToken ct)
        {
            try
            {
                if (request.Items == null || !request.Items.Any())
                    return BaseResponse<object>.Fail("Order must contain at least one item", 400);

                var order = new Order
                {
                    CustomerId = request.CustomerId,
                    Status = "NEW",
                    TotalAmount = request.Items.Sum(i => i.quantity * i.UnitPrice),
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false,
                    OrderItems = request.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        quantity = i.quantity,
                        UnitPrice = i.UnitPrice,
                        CreatedBy = request.CreatedBy,
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true,
                        IsDeleted = false,
                    }).ToList()
                };

                _logger.LogInformation("Creating order for CustomerId={CustomerId} Items={Count}",
                    request.CustomerId, request.Items.Count);

                await _orders.AddAsync(order, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("SUCCESS | Order created OrderId={OrderId} CustomerId={CustomerId}",
                    order.Id, order.CustomerId);

                return BaseResponse<object>.Ok(new { orderId = order.Id }, "Order created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAsync failed CustomerId={CustomerId}", request.CustomerId);
                return BaseResponse<object>.Fail($"Unexpected error while creating order: {ex.Message}", 500);
            }
        }

        public async Task<BaseResponse<OrderDto>> GetAsync(long id, CancellationToken ct)
        {
            try
            {
                var order = await _orders.GetByIdAsync(id, ct);
                if (order is null || order.IsDeleted)
                    return BaseResponse<OrderDto>.Fail("Order not found", 404);

                var dto = new OrderDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    Items = order.OrderItems?.Select(x => new OrderItemDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        quantity = x.quantity,
                        UnitPrice = x.UnitPrice,
                        LineTotal = x.LineTotal
                    }).ToList() ?? new()
                };

                _logger.LogInformation("SUCCESS | Order retrieved OrderId={OrderId} CustomerId={CustomerId}",
                    order.Id, order.CustomerId);

                return BaseResponse<OrderDto>.Ok(dto, "Order retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAsync failed OrderId={OrderId}", id);
                return BaseResponse<OrderDto>.Fail($"Unexpected error while retrieving order: {ex.Message}", 500);
            }
        }

        public async Task<BaseResponse<object>> UpdateAsync(long id, UpdateOrderRequest request, CancellationToken ct)
        {
            try
            {
                var order = await _orders.GetByIdAsync(id, ct);
                if (order is null || order.IsDeleted)
                    return BaseResponse<object>.Fail("Order not found", 404);

                order.Status = request.Status ?? order.Status;
                if (request.TotalAmount.HasValue)
                    order.TotalAmount = request.TotalAmount.Value;

                order.UpdatedBy = request.UpdatedBy;
                order.UpdatedDate = DateTime.UtcNow;

                _logger.LogInformation("Updating order OrderId={OrderId} NewStatus={Status} NewTotal={Total}",
                    order.Id, order.Status, order.TotalAmount);

                _orders.Update(order);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("SUCCESS | Order updated OrderId={OrderId} Status={Status} Total={Total}",
                    order.Id, order.Status, order.TotalAmount);  // ← explicit success

                return BaseResponse<object>.Ok(new { orderId = order.Id }, "Order updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAsync failed OrderId={OrderId}", id);
                return BaseResponse<object>.Fail($"Unexpected error while updating order: {ex.Message}", 500);
            }
        }

        public async Task<BaseResponse<object>> DeleteAsync(long id, string updatedBy, CancellationToken ct)
        {
            try
            {
                var order = await _orders.GetByIdAsync(id, ct);
                if (order is null || order.IsDeleted)
                    return BaseResponse<object>.Fail("Order not found", 404);

                _logger.LogInformation("Soft deleting order OrderId={OrderId} By={User}", id, updatedBy);

                await _orders.SoftDeleteOrderAsync(id, updatedBy, ct);

                _logger.LogInformation("SUCCESS | Order deleted (soft) OrderId={OrderId} By={User}",
                    id, updatedBy);  // ← explicit success

                return BaseResponse<object>.Ok(new { orderId = id }, "Order deleted (soft)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteAsync failed OrderId={OrderId}", id);
                return BaseResponse<object>.Fail($"Unexpected error while deleting order: {ex.Message}", 500);
            }
        }
    }
}
