using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Mimo.Core.Authorization;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;
using Mimo.Infrastructure.Services;

namespace Mimo.UnitTests.Security;

/// <summary>
/// Pruebas de emisión de tokens JWT. Verifica que los claims que consumen el resto del
/// sistema (agent_id, tenant_slug, role) estén presentes y que la expiración respete la config.
/// </summary>
public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiryMinutes = 60)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer        = "mimo-test",
            Audience      = "mimo-test-clients",
            Key           = "clave-de-prueba-con-mas-de-32-caracteres!!",
            ExpiryMinutes = expiryMinutes
        });
        return new JwtTokenService(options);
    }

    private static JwtSecurityToken Parse(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void GenerateAgentToken_IncluyeLosClaimsRequeridos()
    {
        var service = CreateService();
        var agent = new Agent
        {
            Id       = Guid.NewGuid(),
            Email    = "agente@tenant.com",
            FullName = "Agente Uno"
        };

        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var result = service.GenerateAgentToken(agent, "municipio", ["Administrador", "Soporte"], [roleId1, roleId2]);
        var jwt    = Parse(result.Token);

        Assert.Equal(agent.Id.ToString(), jwt.Claims.Single(c => c.Type == "agent_id").Value);
        Assert.Equal("municipio", jwt.Claims.Single(c => c.Type == "tenant_slug").Value);

        var roles = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        Assert.Contains("Administrador", roles);
        Assert.Contains("Soporte", roles);

        var roleIds = jwt.Claims.Where(c => c.Type == "role_id").Select(c => c.Value).ToList();
        Assert.Contains(roleId1.ToString(), roleIds);
        Assert.Contains(roleId2.ToString(), roleIds);
    }

    [Fact]
    public void GenerateSuperAdminToken_IncluyeRolSuperAdmin()
    {
        var service = CreateService();
        var superAdmin = new SuperAdmin
        {
            Id       = Guid.NewGuid(),
            Email    = "root@plataforma.com",
            FullName = "Root"
        };

        var result = service.GenerateSuperAdminToken(superAdmin);
        var jwt    = Parse(result.Token);

        Assert.Equal(
            MimoAuthorization.Roles.SuperAdmin,
            jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal(superAdmin.Id.ToString(), jwt.Claims.Single(c => c.Type == "super_admin_id").Value);
    }

    [Fact]
    public void GenerateAgentToken_RespetaLaExpiracionConfigurada()
    {
        var service = CreateService(expiryMinutes: 30);
        var agent   = new Agent { Id = Guid.NewGuid(), Email = "a@b.com", FullName = "A" };

        var result   = service.GenerateAgentToken(agent, "slug", [], []);
        var esperado = DateTime.UtcNow.AddMinutes(30);

        // Tolerancia amplia para evitar fragilidad por el tiempo de ejecución.
        Assert.True(Math.Abs((result.ExpiresAtUtc - esperado).TotalSeconds) < 60);
    }
}
