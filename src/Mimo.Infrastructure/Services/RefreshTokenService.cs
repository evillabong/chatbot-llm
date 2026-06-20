using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mimo.Core.Interfaces;
using Mimo.Core.Models;
using Mimo.Core.Models.Configuration;
using Mimo.Infrastructure.Data;

namespace Mimo.Infrastructure.Services;

/// <summary>
/// Emite, rota y revoca refresh tokens de funcionarios (#9) sobre el esquema del tenant. El token es
/// aleatorio de alta entropía; se guarda solo su hash (SHA-256, vía <see cref="IApiKeyService"/>).
/// </summary>
public sealed class RefreshTokenService(
    TenantDbContext db,
    IApiKeyService hasher,
    IOptions<JwtOptions> jwtOptions) : IRefreshTokenService
{
    private readonly int _expiryDays = Math.Max(1, jwtOptions.Value.RefreshTokenExpiryDays);

    public async Task<IssuedRefreshToken> IssueAsync(Guid agentId, CancellationToken ct = default)
    {
        var (plain, hash) = Generate();
        var expiresAt = DateTime.UtcNow.AddDays(_expiryDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            AgentId   = agentId,
            TokenHash = hash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return new IssuedRefreshToken(plain, expiresAt);
    }

    public async Task<RefreshRotation?> ValidateAndRotateAsync(string plainToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plainToken)) return null;

        var hash = hasher.Hash(plainToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTime.UtcNow)
            return null;

        // Rotación: revocar el actual y emitir uno nuevo para el mismo funcionario.
        existing.RevokedAt = DateTime.UtcNow;

        var (plain, newHash) = Generate();
        var expiresAt = DateTime.UtcNow.AddDays(_expiryDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id        = Guid.NewGuid(),
            AgentId   = existing.AgentId,
            TokenHash = newHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        return new RefreshRotation(existing.AgentId, plain, expiresAt);
    }

    public async Task RevokeAsync(string plainToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plainToken)) return;
        var hash = hasher.Hash(plainToken);
        await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    private (string Plain, string Hash) Generate()
    {
        var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (plain, hasher.Hash(plain));
    }
}
