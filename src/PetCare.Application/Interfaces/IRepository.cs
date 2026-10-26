using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PetCare.Application.Common;
using PetCare.Domain.Common;

namespace PetCare.Application.Interfaces;

/// <summary>
/// Contrato genérico de persistência para entidades que herdam de BaseEntity.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(QueryParameters query, Expression<Func<T, bool>>? filter = null);

    Task<T?> GetByIdAsync(int id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsByIdAsync(int id);
}
