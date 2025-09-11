namespace OrderService.Business.Services
{
    public record AuthUser(long Id, string Username, string Role);

    public interface IAuthService
    {
        Task<AuthUser?> ValidateAsync(string username, string password);
    }
}
