namespace PetCare.Application.Common;

internal static class Filtros
{
    /// <summary>Normaliza texto de filtro: null se vazio, senão trim + minúsculas.</summary>
    public static string? Normalizar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToLowerInvariant();
}