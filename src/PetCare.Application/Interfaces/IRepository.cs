using System.Collections.Generic;
using System.Threading.Tasks;
using PetCare.Domain.Common;

namespace PetCare.Application.Interfaces;

/// <summary>
/// Contrato genérico de persistência para entidades que herdam de BaseEntity.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsByIdAsync(int id);
}
