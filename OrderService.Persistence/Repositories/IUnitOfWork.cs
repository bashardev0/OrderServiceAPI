using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace OrderService.Persistence
{
    /// <summary>
    /// UoW that supports both EF Core (DbContext/transactions) and Dapper (IDbConnection).
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable
    {
        // Your existing repo(s)
        IOrderRepository Orders { get; }

        // EF Core unit-of-work methods
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default);

        // NEW: ADO.NET connection for Dapper
        IDbConnection Connection { get; }

        /// <summary>Ensures Connection is open (safe to call repeatedly).</summary>
        Task OpenConnectionAsync(CancellationToken ct = default);
    }
}
