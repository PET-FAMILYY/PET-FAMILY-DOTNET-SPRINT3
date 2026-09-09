using System.Threading.Tasks;
using PetCare.Domain.Entities;

namespace PetCare.Application.Interfaces;

public interface ITutorRepository : IRepository<Tutor>
{
    Task<Tutor?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email, int? ignoreId = null);
}
