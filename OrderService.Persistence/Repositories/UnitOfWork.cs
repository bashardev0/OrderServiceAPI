using Microsoft.EntityFrameworkCore.Storage;

namespace OrderService.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    IOrderRepository Orders { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public IOrderRepository Orders { get; }

    public UnitOfWork(AppDbContext db, IOrderRepository orders)
    {
        _db = db;
        Orders = orders;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default)
    {
        IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(ct);
        return tx;
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
