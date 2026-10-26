using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace PetCare.Infrastructure.Persistence;

public static class QueryableExtensions
{
    public static IQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string? propertyName, bool descending)
    {
        var property = ResolveProperty(typeof(T), propertyName ?? "Id");

        var parameter = Expression.Parameter(typeof(T), "e");
        var lambda = Expression.Lambda(Expression.Property(parameter, property), parameter);

        var method = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var call = Expression.Call(
            typeof(Queryable),
            method,
            new[] { typeof(T), property.PropertyType },
            source.Expression,
            Expression.Quote(lambda));

        return source.Provider.CreateQuery<T>(call);
    }

    private static PropertyInfo ResolveProperty(Type type, string name)
    {
        var permitidas = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsOrdenavel(p.PropertyType))
            .ToList();

        return permitidas.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
               ?? throw new ArgumentException(
                   $"Campo de ordenação '{name}' inválido. Campos permitidos: {string.Join(", ", permitidas.Select(p => p.Name))}.");
    }

    private static bool IsOrdenavel(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t == typeof(string) || t == typeof(DateTime) || t == typeof(decimal);
    }
}