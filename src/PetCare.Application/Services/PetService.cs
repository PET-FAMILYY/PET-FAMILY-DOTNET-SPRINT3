using System.Linq.Expressions;
using PetCare.Application.Auditing;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;

namespace PetCare.Application.Services;

public class PetService : IPetService
{
    private readonly IPetRepository _pets;
    private readonly IAuditTrail _audit;

    public PetService(IPetRepository pets, IAuditTrail audit)
    {
        _pets = pets;
        _audit = audit;
    }

    public async Task<PagedResult<PetResponse>> ListarAsync(PetFilter filtro)
    {
        var nome = Filtros.Normalizar(filtro.Nome);
        var especie = Filtros.Normalizar(filtro.Especie);
        var tutorId = filtro.TutorId;

        Expression<Func<Pet, bool>> predicado = p =>
            (nome == null || p.Nome.ToLower().Contains(nome)) &&
            (especie == null || p.Especie.ToLower().Contains(especie)) &&
            (tutorId == null || p.TutorId == tutorId);

        var pagina = await _pets.GetPagedAsync(filtro, predicado);
        return pagina.Map(ToResponse);
    }

    public async Task<PetResponse> ObterAsync(int id)
        => ToResponse(await BuscarAsync(id));

    public async Task<PetResponse> CriarAsync(PetRequest request)
    {
        var pet = new Pet(request.Nome, request.Especie, request.Raca, request.DataNascimento, request.TutorId);
        await _pets.AddAsync(pet);
        await _audit.RegistrarAsync("Pet", pet.Id, "Criacao", $"Pet '{pet.Nome}' cadastrado para o tutor {pet.TutorId}.");
        return ToResponse(pet);
    }

    public async Task<PetResponse> AtualizarAsync(int id, PetRequest request)
    {
        var pet = await BuscarAsync(id);
        pet.Atualizar(request.Nome, request.Especie, request.Raca, request.DataNascimento);
        await _pets.UpdateAsync(pet);
        await _audit.RegistrarAsync("Pet", pet.Id, "Atualizacao", $"Pet '{pet.Nome}' atualizado.");
        return ToResponse(pet);
    }

    public async Task RemoverAsync(int id)
    {
        var pet = await BuscarAsync(id);
        await _pets.DeleteAsync(pet);
        await _audit.RegistrarAsync("Pet", id, "Remocao", $"Pet '{pet.Nome}' removido.");
    }

    private async Task<Pet> BuscarAsync(int id)
        => await _pets.GetByIdAsync(id)
           ?? throw new ResourceNotFoundException($"Pet com Id {id} não foi encontrado.");

    private static PetResponse ToResponse(Pet p)
        => new(p.Id, p.Nome, p.Especie, p.Raca, p.DataNascimento, p.TutorId);
}