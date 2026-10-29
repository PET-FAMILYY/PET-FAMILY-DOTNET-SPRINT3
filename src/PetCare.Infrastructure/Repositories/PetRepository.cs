using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

public class PetRepository : Repository<Pet>, IPetRepository
{
    public PetRepository(PetCareContext context) : base(context) { }

    public async Task<IEnumerable<Pet>> GetByTutorIdAsync(int tutorId)
        => await DbSet.AsNoTracking().Where(p => p.TutorId == tutorId).ToListAsync();
}
