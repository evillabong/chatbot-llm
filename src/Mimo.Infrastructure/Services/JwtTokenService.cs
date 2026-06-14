using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mimo.Core.Authorization;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;

namespace Mimo.Infrastructure.Services;

/// <summary>
/// Emisión de tokens JWT firmados simétricamente (HMAC-SHA256) usando la
/// configuración de la sección "Jwt" (Issuer, Audience, Key, ExpiryMinutes).
/// </summary>
public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public AuthToken GenerateAgentToken(Agent agent, string tenantSlug, IEnumerable<string> roleNames)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, agent.Id.ToString()),
            new("agent_id", agent.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, agent.Email),
            new("name", agent.FullName),
            new("tenant_slug", tenantSlug),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roleNames.Select(role => new Claim("role", role)));

        return GenerateToken(claims);
    }

    public AuthToken GenerateSuperAdminToken(SuperAdmin superAdmin)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, superAdmin.Id.ToString()),
            new("super_admin_id", superAdmin.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, superAdmin.Email),
            new("name", superAdmin.FullName),
            new("role", MimoAuthorization.Roles.SuperAdmin),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateToken(claims);
    }

    private AuthToken GenerateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthToken(tokenString, expiresAt);
    }
}
