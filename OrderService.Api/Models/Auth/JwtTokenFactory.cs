// JwtTokenFactory.cs
using Microsoft.IdentityModel.Tokens;
using OrderService.Api.Auth;
using OrderService.Business.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public sealed class JwtTokenFactory
{
    private readonly JwtOptions _opt;
    public JwtTokenFactory(JwtOptions opt) => _opt = opt;

    public string CreateToken(AuthUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),           // short "role" claim
            new Claim(ClaimTypes.Name, user.Username)        // optional, for logs
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
