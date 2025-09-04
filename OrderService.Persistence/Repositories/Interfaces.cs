using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using OrderService.Domain.Entities;

namespace OrderService.Persistence
{
    // Generic EF ops we already use
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
        Task AddAsync(T entity, CancellationToken ct = default);
        void Update(T entity);
    }

    // Order repo supports BOTH EF and Dapper SP calls
    public interface IOrderRepository : IGenericRepository<Order>
    {
        // Dapper/SP
        Task<string> CreateViaProcAsync(
            long customerId,
            IEnumerable<(long productId, int qty, decimal unitPrice)> items,
            string createdBy,
            CancellationToken ct = default);

        Task<string> GetViaProcAsync(long id, CancellationToken ct = default);

        Task<string> UpdateStatusViaProcAsync(
            long id,
            string status,
            string updatedBy,
            CancellationToken ct = default);

        Task<string> DeleteViaProcAsync(
            long id,
            string updatedBy,
            CancellationToken ct = default);

        // EF Core soft delete
        Task SoftDeleteOrderAsync(
            long orderId,
            string updatedBy,
            CancellationToken ct = default);
    }
}
