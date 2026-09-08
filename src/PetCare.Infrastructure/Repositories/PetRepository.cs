using Microsoft.EntityFrameworkCore;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

public class PetRepository : Repository<Pet>, IPetRepository
{
    private readonly IRepository<Tutor> _tutorRepository;

    public PetRepository(PetCareContext context, IRepository<Tutor> tutorRepository)
        : base(context)
    {
        _tutorRepository = tutorRepository;
    }

    public async Task<IEnumerable<Pet>> GetByTutorIdAsync(int tutorId)
        => await DbSet.AsNoTracking().Where(p => p.TutorId == tutorId).ToListAsync();

    public override async Task<Pet> AddAsync(Pet entity)
    {
        await ValidarTutorExistenteAsync(entity.TutorId);
        return await base.AddAsync(entity);
    }

    public override async Task UpdateAsync(Pet entity)
    {
        await ValidarTutorExistenteAsync(entity.TutorId);
        await base.UpdateAsync(entity);
    }

    private async Task ValidarTutorExistenteAsync(int tutorId)
    {
        var existe = await _tutorRepository.ExistsByIdAsync(tutorId);
        if (!existe)
            throw new ResourceNotFoundException($"Tutor com Id {tutorId} não foi encontrado.");
    }
}
