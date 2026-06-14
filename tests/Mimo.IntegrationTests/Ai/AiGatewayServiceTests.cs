using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mimo.Core.Constants;
using Mimo.Core.Exceptions;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;
using Mimo.IntegrationTests.Fakes;
using Mimo.Infrastructure.AI;
using Mimo.Infrastructure.Data;
using TenantModel = Mimo.Core.Models.Tenant;

namespace Mimo.IntegrationTests.Ai;

/// <summary>
/// Pruebas del gateway de IA sobre GlobalDbContext en memoria: verifican la resolución del
/// conector activo, el entitlement por plan, la cuota del periodo y el registro de uso (ADR 0005).
/// </summary>
public class AiGatewayServiceTests
{
    private const string PlanCode = "pro";

    private static GlobalDbContext NewDb() =>
        new(new DbContextOptionsBuilder<GlobalDbContext>()
            .UseInMemoryDatabase($"ai-gw-{Guid.NewGuid()}")
            .Options);

    private static AiGatewayService NewGateway(GlobalDbContext db, FakeLlmClientFactory? factory = null) =>
        new(db, factory ?? new FakeLlmClientFactory(), NullLogger<AiGatewayService>.Instance);

    private static Guid SeedTenant(GlobalDbContext db, string plan = PlanCode)
    {
        var tenant = new TenantModel
        {
            Id = Guid.NewGuid(), Name = "Municipio", Slug = "municipio", Plan = plan, IsActive = true
        };
        db.Tenants.Add(tenant);
        return tenant.Id;
    }

    private static void SeedActiveConnector(GlobalDbContext db, string provider = AiProviders.DeepSeek)
    {
        db.AiConnectors.Add(new AiConnector
        {
            Id = Guid.NewGuid(), Provider = provider, DisplayName = provider, IsActive = true,
            Settings = new LlmConnectorSettings { ChatModel = "m-chat", EmbeddingModel = "m-emb" }
        });
    }

    [Fact]
    public async Task ChatAsync_SinConectorActivo_Lanza()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db);
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => gateway.ChatAsync(tenantId, "sys", []));
    }

    [Fact]
    public async Task ChatAsync_PlanNoPermiteElProveedor_LanzaAccessDenied()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db);
        SeedActiveConnector(db, AiProviders.DeepSeek);
        db.AiPlanPolicies.Add(new AiPlanPolicy
        {
            Id = Guid.NewGuid(), PlanCode = PlanCode, AllowedProviders = ["openai"], IsActive = true
        });
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        await Assert.ThrowsAsync<AiAccessDeniedException>(
            () => gateway.ChatAsync(tenantId, "sys", []));
    }

    [Fact]
    public async Task ChatAsync_CuotaDeSolicitudesAlcanzada_LanzaQuotaExceeded()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db);
        SeedActiveConnector(db);
        db.AiPlanPolicies.Add(new AiPlanPolicy
        {
            Id = Guid.NewGuid(), PlanCode = PlanCode, MonthlyRequestQuota = 1, IsActive = true
        });
        db.AiUsageRecords.Add(new AiUsageRecord
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Provider = AiProviders.DeepSeek,
            Model = "m-chat", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        await Assert.ThrowsAsync<AiQuotaExceededException>(
            () => gateway.ChatAsync(tenantId, "sys", []));
    }

    [Fact]
    public async Task ChatAsync_CuotaDeTokensAlcanzada_LanzaQuotaExceeded()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db);
        SeedActiveConnector(db);
        db.AiPlanPolicies.Add(new AiPlanPolicy
        {
            Id = Guid.NewGuid(), PlanCode = PlanCode, MonthlyTokenQuota = 100, IsActive = true
        });
        db.AiUsageRecords.Add(new AiUsageRecord
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Provider = AiProviders.DeepSeek,
            Model = "m-chat", TotalTokens = 100, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        await Assert.ThrowsAsync<AiQuotaExceededException>(
            () => gateway.ChatAsync(tenantId, "sys", []));
    }

    [Fact]
    public async Task ChatAsync_SinPolitica_PermiteYRegistraUso()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db, plan: "plan-sin-politica");
        SeedActiveConnector(db);
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        var respuesta = await gateway.ChatAsync(tenantId, "sys", []);

        Assert.Equal("respuesta-fija", respuesta);
        var usage = await db.AiUsageRecords.SingleAsync();
        Assert.Equal(tenantId, usage.TenantId);
        Assert.Equal(FakeLlmClient.FixedUsage.TotalTokens, usage.TotalTokens);
    }

    [Fact]
    public async Task ChatAsync_ProveedorPermitidoYSinCuota_RegistraUsoConModeloDeChat()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db);
        SeedActiveConnector(db);
        db.AiPlanPolicies.Add(new AiPlanPolicy
        {
            Id = Guid.NewGuid(), PlanCode = PlanCode, AllowedProviders = [AiProviders.DeepSeek], IsActive = true
        });
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        await gateway.ChatAsync(tenantId, "sys", []);

        var usage = await db.AiUsageRecords.SingleAsync();
        Assert.Equal("m-chat", usage.Model);
        Assert.Equal(Mimo.Core.Enums.AiOperation.Chat, usage.Operation);
    }

    [Fact]
    public async Task GetEmbeddingAsync_RegistraUsoConModeloDeEmbedding()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db, plan: "plan-sin-politica");
        SeedActiveConnector(db);
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        var vector = await gateway.GetEmbeddingAsync(tenantId, "texto");

        Assert.NotEmpty(vector);
        var usage = await db.AiUsageRecords.SingleAsync();
        Assert.Equal("m-emb", usage.Model);
        Assert.Equal(Mimo.Core.Enums.AiOperation.Embedding, usage.Operation);
    }

    /// <summary>
    /// Verifica el fix del análisis #4: el match de plan es insensible a mayúsculas, de modo
    /// que un tenant con plan "Pro" SÍ aplica la política "pro" (antes caía en modo permisivo).
    /// </summary>
    [Fact]
    public async Task ChatAsync_MatchDePlanEsInsensibleAMayusculas_AplicaLaPolitica()
    {
        using var db = NewDb();
        var tenantId = SeedTenant(db, plan: "Pro"); // mayúscula
        SeedActiveConnector(db, AiProviders.DeepSeek);
        db.AiPlanPolicies.Add(new AiPlanPolicy
        {
            Id = Guid.NewGuid(), PlanCode = "pro", AllowedProviders = ["openai"], IsActive = true
        });
        await db.SaveChangesAsync();

        var gateway = NewGateway(db);

        // La política "pro" aplica pese a la diferencia de mayúsculas y deniega deepseek.
        await Assert.ThrowsAsync<AiAccessDeniedException>(
            () => gateway.ChatAsync(tenantId, "sys", []));
    }
}
