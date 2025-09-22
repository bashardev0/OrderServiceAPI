using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;           // ← add
using OrderService.Persistence;

namespace OrderService.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AuthService> _logger;   // ← add

        public AuthService(IUnitOfWork uow, ILogger<AuthService> logger)   // ← inject logger
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<AuthUser?> ValidateAsync(string username, string password)
        {
            try
            {
                // quick guard: invalid input -> no DB hit
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("AUTH FAIL | Missing username or password");
                    return null;
                }

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

                if (string.IsNullOrEmpty(row.username))
                {
                    _logger.LogWarning("AUTH FAIL | Username={Username}", username);
                    return null;
                }

                _logger.LogInformation("SUCCESS | Login Username={Username} UserId={UserId}",
                    row.username, row.id);

                return new AuthUser(row.id, row.username, row.role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateAsync failed for {Username}", username);
                return null;
            }
        }
    }
}
