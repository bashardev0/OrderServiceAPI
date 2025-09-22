using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using OrderService.Business.Services;   
using OrderService.Api.Auth;           
using OrderService.Api.Models.Auth;    

namespace OrderService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // -> /api/Auth/*
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtTokenFactory _tokenFactory;

        public AuthController(IAuthService authService, JwtTokenFactory tokenFactory)
        {
            _authService = authService;
            _tokenFactory = tokenFactory;
        }

       
        [AllowAnonymous]                       // allow calling without a token
        [HttpPost("login")]                    // -> POST /api/Auth/login
        [Consumes("application/json")]         
        [Produces("application/json")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request is null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
}
