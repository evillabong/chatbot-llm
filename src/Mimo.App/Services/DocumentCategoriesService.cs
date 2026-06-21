using Mimo.Api.Sdk;
using Mimo.Api.Sdk.Models;

namespace Mimo.App.Services;

/// <summary>
/// Operaciones sobre categorías documentales (endpoint /document-categories) por encima del cliente Kiota (#17).
/// </summary>
public sealed class DocumentCategoriesService(MimoApiClient api)
{
    public async Task<List<DocumentCategoryResponse>> ListAsync(CancellationToken ct = default)
    {
        var result = await api.DocumentCategories.GetAsync(cancellationToken: ct);
        return result ?? [];
    }

    public Task<DocumentCategoryResponse?> CreateAsync(string name, Guid? parentCategoryId = null, CancellationToken ct = default) =>
        api.DocumentCategories.PostAsync(new CreateDocumentCategoryRequest
        {
            Name = name,
            ParentCategoryId = parentCategoryId
        }, cancellationToken: ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        api.DocumentCategories.DeleteAsync(rc => rc.QueryParameters.Id = id, ct);
}
