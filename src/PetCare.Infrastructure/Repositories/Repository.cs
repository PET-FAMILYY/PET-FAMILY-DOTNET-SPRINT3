using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetCare.Application.Common;
using PetCare.Application.Interfaces;
using PetCare.Domain.Common;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

/// <summary>
/// Implementação genérica de repositório, reutilizada por todos os repositórios específicos.
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly PetCareContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(PetCareContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
        => await DbSet.AsNoTracking().ToListAsync();
    
    public virtual async Task<PagedResult<T>> GetPagedAsync(QueryParameters query, Expression<Func<T, bool>>? filter = null)
    {
        IQueryable<T> source = DbSet.AsNoTracking();

        if (filter is not null)
            source = source.Where(filter);

        var total = await source.CountAsync();

        var items = await source
            .OrderByProperty(query.SortBy, query.Descending)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<T>(items, query.Page, query.PageSize, total);
    }

    public virtual async Task<T?> GetByIdAsync(int id)
        => await DbSet.FindAsync(id);

    public virtual async Task<T> AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task<bool> ExistsByIdAsync(int id)
        => (await DbSet.CountAsync(e => EF.Property<int>(e, "Id") == id)) > 0;
}
