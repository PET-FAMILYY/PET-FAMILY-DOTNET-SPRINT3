using PetCare.Domain.Entities;

namespace PetCare.Application.Interfaces;

public interface IConsultaRepository : IRepository<Consulta>
{
    Task<IEnumerable<Consulta>> GetByPetIdAsync(int petId);
}
