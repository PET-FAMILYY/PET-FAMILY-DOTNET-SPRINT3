using Microsoft.EntityFrameworkCore;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Infrastructure.Persistence;

namespace PetCare.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly PetCareContext _context;

    public UsuarioRepository(PetCareContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> ObterPorEmailAsync(string email)
    {
        var emailNormalizado = email.Trim().ToLowerInvariant();
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado);
    }

    public async Task<bool> ExisteEmailAsync(string email)
    {
        var emailNormalizado = email.Trim().ToLowerInvariant();
        var quantidade = await _context.Usuarios
            .CountAsync(u => u.Email == emailNormalizado);

        return quantidade > 0;
    }
    
    public async Task AdicionarAsync(Usuario usuario)
    {
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();
    }
}