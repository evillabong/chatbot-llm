using Microsoft.AspNetCore.DataProtection;
using Mimo.Core.Interfaces;

namespace Mimo.Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="ISecretProtector"/> basada en ASP.NET Core Data Protection.
///
/// El cifrado y descifrado lo hacen procesos distintos (Mimo.Admin.Api cifra al crear el
/// conector; Mimo.Api descifra al invocar el LLM), por lo que ambas APIs deben compartir el
/// mismo anillo de llaves (mismo SetApplicationName y misma ubicación de persistencia).
/// Ver ADR 0010.
/// </summary>
public class DataProtectionSecretProtector : ISecretProtector
{
    // El "purpose" forma parte de la derivación de clave; cambiarlo invalida lo ya cifrado.
    private const string Purpose = "MIMO.AiConnector.ApiKey.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector(Purpose);

    public string Protect(string? plaintext)
        => string.IsNullOrEmpty(plaintext) ? string.Empty : _protector.Protect(plaintext);

    public string Unprotect(string? protectedValue)
        => string.IsNullOrEmpty(protectedValue) ? string.Empty : _protector.Unprotect(protectedValue);
}
