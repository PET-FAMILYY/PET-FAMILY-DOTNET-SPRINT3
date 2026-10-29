using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

public class LembreteRepository : Repository<Lembrete>, ILembreteRepository
{
    public LembreteRepository(PetCareContext context) : base(context) { }

    public async Task<IEnumerable<Lembrete>> GetByPetIdAsync(int petId)
        => await DbSet.AsNoTracking().Where(l => l.PetId == petId).ToListAsync();
}
