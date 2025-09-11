using System.Threading.Tasks;
using Dapper;
using OrderService.Persistence;

namespace OrderService.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;

        public AuthService(IUnitOfWork uow) => _uow = uow;

        public async Task<AuthUser?> ValidateAsync(string username, string password)
        {
            const string sql = @"
SELECT id, username, role
FROM ""order"".login
WHERE username = @username
  AND is_deleted = FALSE
  AND is_active  = TRUE
  AND password_hash = crypt(@password, password_hash)
LIMIT 1;";

            await _uow.OpenConnectionAsync();

            var row = await _uow.Connection
                .QuerySingleOrDefaultAsync<(long id, string username, string role)>(
                    sql, new { username, password });

            if (row.username is null) return null;

            return new AuthUser(row.id, row.username, row.role);
        }
    }
}
