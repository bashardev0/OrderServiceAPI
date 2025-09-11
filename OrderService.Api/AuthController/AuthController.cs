using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using OrderService.Business.Services;
using OrderService.Api.Auth;

namespace OrderService.Api.AuthController
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtTokenFactory _tokenFactory;

        public AuthController(IAuthService authService, JwtTokenFactory tokenFactory)
        {
            _authService = authService;
            _tokenFactory = tokenFactory;
        }

        /// <summary>Authenticate user and return a JWT token.</summary>
        [AllowAnonymous] // important so Swagger can call it without a token
        [HttpPost("login")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request is null)
                return BadRequest(new { message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username and password are required." });

            var user = await _authService.ValidateAsync(request.Username, request.Password);

            if (user is null)
                return Unauthorized(new { message = "Invalid credentials." });

            var token = _tokenFactory.CreateToken(user);

            return Ok(new
            {
                message = "Login successful",
                token,
                user = new { user.Id, user.Username, user.Role }
            });
        }
    }

    // Request DTO
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
