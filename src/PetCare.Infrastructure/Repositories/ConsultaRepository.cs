using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

public class ConsultaRepository : Repository<Consulta>, IConsultaRepository
{
    private readonly IRepository<Pet> _petRepository;

    public ConsultaRepository(PetCareContext context, IRepository<Pet> petRepository)
        : base(context)
    {
        _petRepository = petRepository;
    }

    public async Task<IEnumerable<Consulta>> GetByPetIdAsync(int petId)
        => await DbSet.AsNoTracking().Where(c => c.PetId == petId).ToListAsync();

    public override async Task<Consulta> AddAsync(Consulta entity)
    {
        await ValidarPetExistenteAsync(entity.PetId);
        return await base.AddAsync(entity);
    }

    public override async Task UpdateAsync(Consulta entity)
    {
        await ValidarPetExistenteAsync(entity.PetId);
        await base.UpdateAsync(entity);
    }

    private async Task ValidarPetExistenteAsync(int petId)
    {
        var existe = await _petRepository.ExistsByIdAsync(petId);
        if (!existe)
            throw new ResourceNotFoundException($"Pet com Id {petId} não foi encontrado.");
    }
}
