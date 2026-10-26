using System;

namespace PetCare.Application.Common;

/// <summary>Parâmetros comuns de paginação e ordenação para endpoints de listagem.</summary>
public class QueryParameters
{
    public const int MaxPageSize = 50;

    private int _page = 1;
    private int _pageSize = 10;

    /// <summary>Número da página (começa em 1).</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>Itens por página (1 a 50; padrão 10).</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    /// <summary>Nome da propriedade usada na ordenação (padrão: Id).</summary>
    public string? SortBy { get; set; }

    /// <summary>Direção da ordenação: asc (padrão) ou desc.</summary>
    public string SortDir { get; set; } = "asc";

    public bool Descending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}