using Microsoft.EntityFrameworkCore;
using Mimo.Core.Constants;
using Mimo.Core.Enums;
using Mimo.Core.Models;
using Mimo.Infrastructure.Data;
using Mimo.Infrastructure.Data.Repositories;

namespace Mimo.IntegrationTests.Ai;

/// <summary>
/// Pruebas de la agregación de uso de IA (resumen por tenant y periodo) sobre EF InMemory.
/// </summary>
public class AiUsageRepositoryTests
{
    private static GlobalDbContext NewDb() =>
        new(new DbContextOptionsBuilder<GlobalDbContext>()
            .UseInMemoryDatabase($"ai-usage-{Guid.NewGuid()}")
            .Options);

    private static AiUsageRecord Record(Guid tenantId, DateTime createdAt, int total, int prompt = 0, int completion = 0)
        => new()
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Provider = AiProviders.DeepSeek,
            Model = "m", Operation = AiOperation.Chat,
            PromptTokens = prompt, CompletionTokens = completion, TotalTokens = total,
            CreatedAt = createdAt
        };

    [Fact]
    public async Task GetSummaryAsync_AgrupaPorTenant_YSumaTokensYConteo()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var now     = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

        using var db = NewDb();
        db.AiUsageRecords.AddRange(
            Record(tenantA, now, total: 30, prompt: 10, completion: 20),
            Record(tenantA, now, total: 5,  prompt: 2,  completion: 3),
            Record(tenantB, now, total: 100, prompt: 40, completion: 60));
        await db.SaveChangesAsync();

        var repo    = new AiUsageRepository(db);
        var summary = await repo.GetSummaryAsync(now.AddDays(-1), now.AddDays(1), tenantId: null);

        Assert.Equal(2, summary.Count);

        var a = summary.Single(s => s.TenantId == tenantA);
        Assert.Equal(2, a.Requests);
        Assert.Equal(35, a.TotalTokens);
        Assert.Equal(12, a.PromptTokens);
        Assert.Equal(23, a.CompletionTokens);

        var b = summary.Single(s => s.TenantId == tenantB);
        Assert.Equal(1, b.Requests);
        Assert.Equal(100, b.TotalTokens);
    }

    [Fact]
    public async Task GetSummaryAsync_RespetaElRangoDeFechas()
    {
        var tenant = Guid.NewGuid();
        var now    = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

        using var db = NewDb();
        db.AiUsageRecords.AddRange(
            Record(tenant, now.AddMonths(-2), total: 999),  // fuera de rango
            Record(tenant, now,               total: 50));  // dentro
        await db.SaveChangesAsync();

        var repo    = new AiUsageRepository(db);
        var summary = await repo.GetSummaryAsync(now.AddDays(-1), now.AddDays(1), tenantId: null);

        Assert.Single(summary);
        Assert.Equal(50, summary[0].TotalTokens);
    }

    [Fact]
    public async Task GetSummaryAsync_FiltraPorTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var now     = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

        using var db = NewDb();
        db.AiUsageRecords.AddRange(
            Record(tenantA, now, total: 10),
            Record(tenantB, now, total: 20));
        await db.SaveChangesAsync();

        var repo    = new AiUsageRepository(db);
        var summary = await repo.GetSummaryAsync(now.AddDays(-1), now.AddDays(1), tenantId: tenantA);

        Assert.Single(summary);
        Assert.Equal(tenantA, summary[0].TenantId);
        Assert.Equal(10, summary[0].TotalTokens);
    }
}
