using System.Linq.Expressions;
using PetCare.Application.Auditing;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;

namespace PetCare.Application.Services;

public class ConsultaService : IConsultaService
{
    private readonly IConsultaRepository _consultas;
    private readonly IPetRepository _pets;
    private readonly IAuditTrail _audit;

    public ConsultaService(IConsultaRepository consultas, IPetRepository pets, IAuditTrail audit)
    {
        _consultas = consultas;
        _pets = pets;
        _audit = audit;
    }

    public async Task<PagedResult<ConsultaResponse>> ListarAsync(ConsultaFilter filtro)
    {
        var petId = filtro.PetId;
        var de = filtro.De;
        var ate = filtro.Ate;

        Expression<Func<Consulta, bool>> predicado = c =>
            (petId == null || c.PetId == petId) &&
            (de == null || c.DataConsulta >= de) &&
            (ate == null || c.DataConsulta <= ate);

        var pagina = await _consultas.GetPagedAsync(filtro, predicado);
        return pagina.Map(ToResponse);
    }

    public async Task<ConsultaResponse> ObterAsync(int id)
        => ToResponse(await BuscarAsync(id));

    public async Task<ConsultaResponse> CriarAsync(ConsultaRequest request)
    {
        var consulta = new Consulta(request.DataConsulta, request.Observacoes, request.PetId);

        if (!await _pets.ExistsByIdAsync(request.PetId))
            throw new ResourceNotFoundException($"Pet com Id {request.PetId} não foi encontrado.");

        await _consultas.AddAsync(consulta);
        await _audit.RegistrarAsync("Consulta", consulta.Id, "Criacao", $"Consulta do pet {consulta.PetId} agendada.");
        return ToResponse(consulta);
    }

    public async Task<ConsultaResponse> AtualizarAsync(int id, ConsultaRequest request)
    {
        var consulta = await BuscarAsync(id);
        consulta.Atualizar(request.DataConsulta, request.Observacoes);
        await _consultas.UpdateAsync(consulta);
        await _audit.RegistrarAsync("Consulta", consulta.Id, "Atualizacao", "Consulta atualizada.");
        return ToResponse(consulta);
    }

    public async Task RemoverAsync(int id)
    {
        var consulta = await BuscarAsync(id);
        await _consultas.DeleteAsync(consulta);
        await _audit.RegistrarAsync("Consulta", id, "Remocao", "Consulta removida.");
    }

    private async Task<Consulta> BuscarAsync(int id)
        => await _consultas.GetByIdAsync(id)
           ?? throw new ResourceNotFoundException($"Consulta com Id {id} não foi encontrada.");

    private static ConsultaResponse ToResponse(Consulta c)
        => new(c.Id, c.DataConsulta, c.Observacoes, c.PetId);
}