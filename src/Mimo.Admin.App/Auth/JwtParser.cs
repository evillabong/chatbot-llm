using System.Security.Claims;
using System.Text.Json;

namespace Mimo.Admin.App.Auth;

/// <summary>
/// Extrae los claims de un JWT (payload) sin validar la firma. La validación criptográfica la
/// hace el backend; el cliente solo usa los claims para UI/autorización local.
/// </summary>
public static class JwtParser
{
    public static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return [];

        var json = Base64UrlDecode(parts[1]);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (dict is null) return [];

        var claims = new List<Claim>();
        foreach (var (key, value) in dict)
        {
            if (value.ValueKind == JsonValueKind.Array)
                claims.AddRange(value.EnumerateArray().Select(e => new Claim(key, e.ToString())));
            else
                claims.Add(new Claim(key, value.ToString()));
        }
        return claims;
    }

    public static DateTimeOffset? GetExpiration(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        return long.TryParse(exp, out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
    }

    private static string Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4) { case 2: output += "=="; break; case 3: output += "="; break; }
        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(output));
    }
}
