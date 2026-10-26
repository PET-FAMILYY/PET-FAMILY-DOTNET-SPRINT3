namespace PetCare.Application.Common;

/// <summary>Link HATEOAS (href, relação e verbo HTTP).</summary>
public record Link(string Href, string Rel, string Method);

/// <summary>Base para qualquer recurso que carregue links HATEOAS.</summary>
public abstract record ResourceBase
{
    public IReadOnlyList<Link> Links { get; init; } = Array.Empty<Link>();
}

/// <summary>Página de resultados com metadados de paginação.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount) : ResourceBase
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector)
        => new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}