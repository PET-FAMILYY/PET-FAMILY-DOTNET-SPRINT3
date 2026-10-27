using Microsoft.AspNetCore.WebUtilities;
using PetCare.Application.Common;

namespace PetCare.API.Hateoas;

public static class Hateoas
{
    /// <summary>Links padrão de um item: self, update, delete e a coleção, mais relações extras.</summary>
    public static IReadOnlyList<Link> ForItem(string self, string collection, params Link[] extras)
    {
        var links = new List<Link>
        {
            new(self, "self", "GET"),
            new(self, "update", "PUT"),
            new(self, "delete", "DELETE"),
            new(collection, "collection", "GET")
        };
        links.AddRange(extras);
        return links;
    }

    /// <summary>Links de navegação de uma página: self, first, prev, next e last (preservando os filtros da query).</summary>
    public static IReadOnlyList<Link> PageLinks<T>(HttpRequest request, PagedResult<T> page)
    {
        var ultima = Math.Max(1, page.TotalPages);

        string Montar(int numero)
        {
            var query = request.Query.ToDictionary(
                q => q.Key, q => (string?)q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            query["page"] = numero.ToString();
            query["pageSize"] = page.PageSize.ToString();
            return QueryHelpers.AddQueryString($"{request.PathBase}{request.Path}", query);
        }

        var links = new List<Link>
        {
            new(Montar(page.Page), "self", "GET"),
            new(Montar(1), "first", "GET")
        };
        if (page.Page > 1) links.Add(new Link(Montar(page.Page - 1), "prev", "GET"));
        if (page.Page < ultima) links.Add(new Link(Montar(page.Page + 1), "next", "GET"));
        links.Add(new Link(Montar(ultima), "last", "GET"));
        return links;
    }
}