using System.Data;
using Npgsql;

namespace OrderService.Persistence;

public interface IConnectionFactory { IDbConnection Create(); }

public class NpgsqlConnectionFactory : IConnectionFactory
{
    private readonly string _connStr;
    public NpgsqlConnectionFactory(string connStr) => _connStr = connStr;
    public IDbConnection Create() => new NpgsqlConnection(_connStr);
}
