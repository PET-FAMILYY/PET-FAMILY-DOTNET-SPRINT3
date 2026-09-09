using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

public class TutorRepository : Repository<Tutor>, ITutorRepository
{
    public TutorRepository(PetCareContext context) : base(context) { }

    public async Task<Tutor?> GetByEmailAsync(string email)
        => await DbSet.FirstOrDefaultAsync(t => t.Email == email);

    public async Task<bool> ExistsByEmailAsync(string email, int? ignoreId = null)
    {
        var query = DbSet.Where(t => t.Email == email);

        if (ignoreId.HasValue)
            query = query.Where(t => t.Id != ignoreId.Value);

        return await query.CountAsync() > 0;
    }
}
