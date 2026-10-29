using Microsoft.EntityFrameworkCore;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

public class ConsultaRepository : Repository<Consulta>, IConsultaRepository
{
    public ConsultaRepository(PetCareContext context) : base(context) { }

    public async Task<IEnumerable<Consulta>> GetByPetIdAsync(int petId)
        => await DbSet.AsNoTracking().Where(c => c.PetId == petId).ToListAsync();
}
