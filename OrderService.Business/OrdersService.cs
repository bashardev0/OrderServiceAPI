using OrderService.Persistence;
using OrderService.Domain.Requests;
using OrderService.Domain.Responses;
using OrderService.Domain.Entities;
using OrderService.Business.Services;

namespace OrderService.Business
{
    public sealed class OrdersService : IOrdersService
    {
        private readonly IOrderRepository _orders;
        private readonly IUnitOfWork _uow;

        public OrdersService(IOrderRepository orders, IUnitOfWork uow)
        {
            _orders = orders;
            _uow = uow;
        }

        public async Task<BaseResponse<object>> CreateAsync(CreateOrderRequest request, CancellationToken ct)
        {
            if (request.Items == null || !request.Items.Any())
                return BaseResponse<object>.Fail("Order must contain at least one item", 400);

            var order = new Order
            {
                CustomerId = request.CustomerId,
                Status = "NEW",
                TotalAmount = request.Items.Sum(i => i.Qty * i.UnitPrice),
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
                OrderItems = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    CreatedBy = request.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false,
                }).ToList()
            };

            await _orders.AddAsync(order, ct);
            await _uow.SaveChangesAsync(ct);

            return BaseResponse<object>.Ok(new { orderId = order.Id }, "Order created");
        }

        public async Task<BaseResponse<OrderDto>> GetAsync(long id, CancellationToken ct)
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
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice,
                    LineTotal = x.LineTotal
                }).ToList() ?? new()
            };

            return BaseResponse<OrderDto>.Ok(dto, "Order retrieved");
        }

        public async Task<BaseResponse<object>> UpdateAsync(long id, UpdateOrderRequest request, CancellationToken ct)
        {
            var order = await _orders.GetByIdAsync(id, ct);
            if (order is null || order.IsDeleted)
                return BaseResponse<object>.Fail("Order not found", 404);

            order.Status = request.Status ?? order.Status;
            if (request.TotalAmount.HasValue)
                order.TotalAmount = request.TotalAmount.Value;

            order.UpdatedBy = request.UpdatedBy;
            order.UpdatedDate = DateTime.UtcNow;

            _orders.Update(order);
            await _uow.SaveChangesAsync(ct);

            return BaseResponse<object>.Ok(new { orderId = order.Id }, "Order updated");
        }

        // ✅ NEW Delete implementation uses repository’s SoftDelete
        public async Task<BaseResponse<object>> DeleteAsync(long id, string updatedBy, CancellationToken ct)
        {
            var order = await _orders.GetByIdAsync(id, ct);
            if (order is null || order.IsDeleted)
                return BaseResponse<object>.Fail("Order not found", 404);

            await _orders.SoftDeleteOrderAsync(id, updatedBy, ct);

            return BaseResponse<object>.Ok(new { orderId = id }, "Order deleted (soft)");
        }
    }
}
