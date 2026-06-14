using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Mimo.Core.Constants;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;

namespace Mimo.Infrastructure.Data.Seeding;

/// <summary>
/// Siembra el conector de IA inicial a partir de la sección "DeepSeek" de appsettings,
/// migrando la configuración existente hacia la base de datos sin intervención manual.
///
/// Solo actúa si la tabla ai_connectors está vacía: una vez la configuración vive en BD,
/// la fuente de verdad es la base de datos, no appsettings.
/// </summary>
public static class AiConnectorSeeder
{
    public static async Task SeedDefaultAsync(
        GlobalDbContext db, IConfiguration configuration, CancellationToken ct = default)
    {
        if (await db.AiConnectors.AnyAsync(ct))
            return;

        var section = configuration.GetSection("DeepSeek");

        var connector = new AiConnector
        {
            Id          = Guid.NewGuid(),
            Provider    = AiProviders.DeepSeek,
            DisplayName = "DeepSeek",
            IsActive    = true,
            Settings    = new LlmConnectorSettings
            {
                ApiKey         = section["ApiKey"]         ?? string.Empty,
                BaseUrl        = section["BaseUrl"]        ?? "https://api.deepseek.com",
                ChatModel      = section["ChatModel"]      ?? "deepseek-chat",
                EmbeddingModel = section["EmbeddingModel"] ?? "deepseek-embedding"
            },
            CreatedAt = DateTime.UtcNow
        };

        db.AiConnectors.Add(connector);
        await db.SaveChangesAsync(ct);
    }
}
