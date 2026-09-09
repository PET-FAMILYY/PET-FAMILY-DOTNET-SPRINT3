using System.Collections.Generic;
using System.Threading.Tasks;
using PetCare.Domain.Entities;

namespace PetCare.Application.Interfaces;

public interface ILembreteRepository : IRepository<Lembrete>
{
    Task<IEnumerable<Lembrete>> GetByPetIdAsync(int petId);
}
