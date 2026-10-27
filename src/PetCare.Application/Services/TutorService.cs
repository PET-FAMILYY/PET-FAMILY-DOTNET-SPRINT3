using System.Linq.Expressions;
using PetCare.Application.Auditing;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;

namespace PetCare.Application.Services;

public class TutorService : ITutorService
{
    private readonly ITutorRepository _tutores;
    private readonly IAuditTrail _audit;

    public TutorService(ITutorRepository tutores, IAuditTrail audit)
    {
        _tutores = tutores;
        _audit = audit;
    }

    public async Task<PagedResult<TutorResponse>> ListarAsync(TutorFilter filtro)
    {
        var nome = Filtros.Normalizar(filtro.Nome);
        var email = Filtros.Normalizar(filtro.Email);

        Expression<Func<Tutor, bool>> predicado = t =>
            (nome == null || t.Nome.ToLower().Contains(nome)) &&
            (email == null || t.Email.ToLower().Contains(email));

        var pagina = await _tutores.GetPagedAsync(filtro, predicado);
        return pagina.Map(ToResponse);
    }

    public async Task<TutorResponse> ObterAsync(int id)
        => ToResponse(await BuscarAsync(id));

    public async Task<TutorResponse> CriarAsync(TutorRequest request)
    {
        if (await _tutores.ExistsByEmailAsync(request.Email))
            throw new ConflictException($"Já existe um tutor cadastrado com o e-mail '{request.Email}'.");

        var tutor = new Tutor(request.Nome, request.Email, request.Telefone);
        await _tutores.AddAsync(tutor);
        await _audit.RegistrarAsync("Tutor", tutor.Id, "Criacao", $"Tutor '{tutor.Nome}' cadastrado.");
        return ToResponse(tutor);
    }

    public async Task<TutorResponse> AtualizarAsync(int id, TutorRequest request)
    {
        var tutor = await BuscarAsync(id);

        if (await _tutores.ExistsByEmailAsync(request.Email, ignoreId: id))
            throw new ConflictException($"Já existe um tutor cadastrado com o e-mail '{request.Email}'.");

        tutor.Atualizar(request.Nome, request.Email, request.Telefone);
        await _tutores.UpdateAsync(tutor);
        await _audit.RegistrarAsync("Tutor", tutor.Id, "Atualizacao", $"Tutor '{tutor.Nome}' atualizado.");
        return ToResponse(tutor);
    }

    public async Task RemoverAsync(int id)
    {
        var tutor = await BuscarAsync(id);
        await _tutores.DeleteAsync(tutor);
        await _audit.RegistrarAsync("Tutor", id, "Remocao", $"Tutor '{tutor.Nome}' removido.");
    }

    private async Task<Tutor> BuscarAsync(int id)
        => await _tutores.GetByIdAsync(id)
           ?? throw new ResourceNotFoundException($"Tutor com Id {id} não foi encontrado.");

    private static TutorResponse ToResponse(Tutor t) => new(t.Id, t.Nome, t.Email, t.Telefone);
}