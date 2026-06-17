using Mimo.ApiClient.MimoApi;
using Mimo.ApiClient.MimoApi.Models;

namespace Mimo.App.Services;

/// <summary>
/// Operaciones sobre documentos de conocimiento (endpoint /documents) por encima del cliente Kiota.
/// </summary>
public sealed class DocumentsService(MimoApiClient api)
{
    public async Task<List<DocumentResponse>> ListAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var result = await api.Documents.GetAsync(rc =>
        {
            if (isActive is not null) rc.QueryParameters.IsActive = isActive;
        }, ct);
        return result ?? [];
    }

    public Task<DocumentResponse?> CreateAsync(CreateDocumentRequest request, CancellationToken ct = default) =>
        api.Documents.PostAsync(request, cancellationToken: ct);

    public Task<DocumentResponse?> UpdateAsync(Guid id, UpdateDocumentRequest request, CancellationToken ct = default) =>
        api.Documents.PutAsync(request, rc => rc.QueryParameters.Id = id, ct);

    public Task DeactivateAsync(Guid id, CancellationToken ct = default) =>
        api.Documents.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);

    public async Task ReindexAsync(Guid id, CancellationToken ct = default)
    {
        // El endpoint responde un objeto sin tipo declarado; Kiota lo expone como Stream.
        var stream = await api.Documents.Reindex.PostAsync(rc => rc.QueryParameters.Id = id, ct);
        if (stream is not null) await stream.DisposeAsync();
    }
}
