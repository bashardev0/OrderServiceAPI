using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace OrderService.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public IOrderRepository Orders { get; }

        public UnitOfWork(AppDbContext db, IOrderRepository orders)
        {
            _db = db;
            Orders = orders;
        }

        // ---- EF Core ----
        public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
            _db.SaveChangesAsync(ct);

        public async Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default)
        {
            IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(ct);
            return tx;
        }

        public ValueTask DisposeAsync() => _db.DisposeAsync();

        // ---- Dapper support ----
        public IDbConnection Connection => _db.Database.GetDbConnection();

        public async Task OpenConnectionAsync(CancellationToken ct = default)
        {
            if (Connection.State != ConnectionState.Open)
            {
                if (Connection is System.Data.Common.DbConnection dbc)
                    await dbc.OpenAsync(ct);
                else
                    Connection.Open(); // fallback (sync)
            }
        }
    }
}
