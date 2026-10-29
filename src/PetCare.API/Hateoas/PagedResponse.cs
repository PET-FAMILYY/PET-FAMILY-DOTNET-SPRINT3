using PetCare.Application.Common;

namespace PetCare.API.Hateoas;

/// <summary>Resposta paginada da API: dados da página + links de navegação (HATEOAS).</summary>
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<Link> Links)
{
    public static PagedResponse<T> From(PagedResult<T> page, IReadOnlyList<Link> links)
        => new(page.Items, page.Page, page.PageSize, page.TotalCount, page.TotalPages, links);
}