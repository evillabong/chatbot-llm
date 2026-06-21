using Microsoft.Extensions.Logging.Abstractions;
using Mimo.Core.Models;
using Mimo.Infrastructure.Sales;

namespace Mimo.UnitTests.Sales;

/// <summary>
/// Pruebas del proveedor de CRM simulado (#26): asigna un id externo determinista y conserva el
/// existente en re-sincronizaciones.
/// </summary>
public class SimulatedCrmSyncProviderTests
{
    private static SimulatedCrmSyncProvider NewProvider()
        => new(NullLogger<SimulatedCrmSyncProvider>.Instance);

    [Fact]
    public async Task Sync_AssignsDeterministicExternalId_WhenNone()
    {
        var opp = new Opportunity { Id = Guid.NewGuid() };
        var result = await NewProvider().SyncOpportunityAsync(opp);

        Assert.True(result.Success);
        Assert.StartsWith("SIM-", result.ExternalId);
        // Determinista: el mismo input produce el mismo id externo.
        var again = await NewProvider().SyncOpportunityAsync(opp);
        Assert.Equal(result.ExternalId, again.ExternalId);
    }

    [Fact]
    public async Task Sync_PreservesExistingExternalId()
    {
        var opp = new Opportunity { Id = Guid.NewGuid(), ExternalCrmId = "EXT-EXISTING" };
        var result = await NewProvider().SyncOpportunityAsync(opp);

        Assert.True(result.Success);
        Assert.Equal("EXT-EXISTING", result.ExternalId);
    }
}
