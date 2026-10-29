using System.Linq.Expressions;
using PetCare.Application.Auditing;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;

namespace PetCare.Application.Services;

public class LembreteService : ILembreteService
{
    private readonly ILembreteRepository _lembretes;
    private readonly IPetRepository _pets;
    private readonly IAuditTrail _audit;

    public LembreteService(ILembreteRepository lembretes, IPetRepository pets, IAuditTrail audit)
    {
        _lembretes = lembretes;
        _pets = pets;
        _audit = audit;
    }

    public async Task<PagedResult<LembreteResponse>> ListarAsync(LembreteFilter filtro)
    {
        var petId = filtro.PetId;
        var titulo = Filtros.Normalizar(filtro.Titulo);
        var de = filtro.De;
        var ate = filtro.Ate;

        Expression<Func<Lembrete, bool>> predicado = l =>
            (petId == null || l.PetId == petId) &&
            (titulo == null || l.Titulo.ToLower().Contains(titulo)) &&
            (de == null || l.DataLembrete >= de) &&
            (ate == null || l.DataLembrete <= ate);

        var pagina = await _lembretes.GetPagedAsync(filtro, predicado);
        return pagina.Map(ToResponse);
    }

    public async Task<LembreteResponse> ObterAsync(int id)
        => ToResponse(await BuscarAsync(id));

    public async Task<LembreteResponse> CriarAsync(LembreteRequest request)
    {
        var lembrete = new Lembrete(request.Titulo, request.Descricao, request.DataLembrete, request.PetId);

        if (!await _pets.ExistsByIdAsync(request.PetId))
            throw new ResourceNotFoundException($"Pet com Id {request.PetId} não foi encontrado.");

        await _lembretes.AddAsync(lembrete);
        await _audit.RegistrarAsync("Lembrete", lembrete.Id, "Criacao", $"Lembrete '{lembrete.Titulo}' criado.");
        return ToResponse(lembrete);
    }

    public async Task<LembreteResponse> AtualizarAsync(int id, LembreteRequest request)
    {
        var lembrete = await BuscarAsync(id);
        lembrete.Atualizar(request.Titulo, request.Descricao, request.DataLembrete);
        await _lembretes.UpdateAsync(lembrete);
        await _audit.RegistrarAsync("Lembrete", lembrete.Id, "Atualizacao", $"Lembrete '{lembrete.Titulo}' atualizado.");
        return ToResponse(lembrete);
    }

    public async Task RemoverAsync(int id)
    {
        var lembrete = await BuscarAsync(id);
        await _lembretes.DeleteAsync(lembrete);
        await _audit.RegistrarAsync("Lembrete", id, "Remocao", $"Lembrete '{lembrete.Titulo}' removido.");
    }

    private async Task<Lembrete> BuscarAsync(int id)
        => await _lembretes.GetByIdAsync(id)
           ?? throw new ResourceNotFoundException($"Lembrete com Id {id} não foi encontrado.");

    private static LembreteResponse ToResponse(Lembrete l)
        => new(l.Id, l.Titulo, l.Descricao, l.DataLembrete, l.PetId);
}