using System.Net;
using Mimo.Infrastructure.Chatbot;

namespace Mimo.UnitTests.Chatbot;

/// <summary>
/// Pruebas de los controles anti-SSRF del ejecutor de nodos ApiCall (#27 corte 3): allow-list de
/// hosts (deny por defecto) y bloqueo de IPs internas/metadata.
/// </summary>
public class ChatbotApiCallerTests
{
    [Theory]
    [InlineData("api.partner.com", new[] { "partner.com" }, true)]   // sufijo de dominio
    [InlineData("partner.com", new[] { "partner.com" }, true)]       // exacto
    [InlineData("evil.com", new[] { "partner.com" }, false)]         // no permitido
    [InlineData("notpartner.com", new[] { "partner.com" }, false)]   // no es sufijo real
    [InlineData("api.partner.com", new string[0], false)]            // allow-list vacía = deny all
    public void IsHostAllowed_RespectsAllowList(string host, string[] allowed, bool expected)
        => Assert.Equal(expected, ChatbotApiCaller.IsHostAllowed(host, allowed));

    [Theory]
    [InlineData("169.254.169.254")]   // metadata cloud (siempre bloqueado)
    [InlineData("127.0.0.1")]         // loopback
    [InlineData("10.0.0.5")]          // privada
    [InlineData("192.168.1.10")]      // privada
    [InlineData("172.16.5.5")]        // privada
    public void IsBlockedAddress_BlocksInternal_WhenLocalhostNotAllowed(string ip)
        => Assert.True(ChatbotApiCaller.IsBlockedAddress(IPAddress.Parse(ip), allowLocalhost: false));

    [Fact]
    public void IsBlockedAddress_AllowsLoopback_WhenLocalhostAllowed()
        => Assert.False(ChatbotApiCaller.IsBlockedAddress(IPAddress.Loopback, allowLocalhost: true));

    [Fact]
    public void IsBlockedAddress_AlwaysBlocksMetadata_EvenWhenLocalhostAllowed()
        => Assert.True(ChatbotApiCaller.IsBlockedAddress(IPAddress.Parse("169.254.169.254"), allowLocalhost: true));

    [Fact]
    public void IsBlockedAddress_AllowsPublicIp()
        => Assert.False(ChatbotApiCaller.IsBlockedAddress(IPAddress.Parse("8.8.8.8"), allowLocalhost: false));
}
