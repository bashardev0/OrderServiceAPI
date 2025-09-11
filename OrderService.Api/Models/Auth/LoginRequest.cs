using System.ComponentModel.DataAnnotations;

namespace OrderService.Api.Models.Auth
{
    public class LoginRequest
    {
        [Required] public string Username { get; set; } = default!;
        [Required] public string Password { get; set; } = default!;
    }
}
