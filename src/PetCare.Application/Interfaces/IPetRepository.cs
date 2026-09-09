using System.Collections.Generic;
using System.Threading.Tasks;
using PetCare.Domain.Entities;

namespace PetCare.Application.Interfaces;

public interface IPetRepository : IRepository<Pet>
{
    Task<IEnumerable<Pet>> GetByTutorIdAsync(int tutorId);
}
