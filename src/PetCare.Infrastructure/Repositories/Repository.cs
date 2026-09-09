using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
