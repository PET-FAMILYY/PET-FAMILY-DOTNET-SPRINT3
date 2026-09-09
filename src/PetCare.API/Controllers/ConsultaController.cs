using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;

namespace PetCare.API.Controllers;

/// <summary>
/// Gerencia consultas veterinárias associadas a um pet.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConsultaController : ControllerBase
{
    private readonly IConsultaRepository _consultaRepository;

    public ConsultaController(IConsultaRepository consultaRepository)
    {
        _consultaRepository = consultaRepository;
    }

    /// <summary>Lista todas as consultas cadastradas.</summary>
    /// <response code="200">Retorna a lista de consultas.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ConsultaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConsultaResponse>>> GetAll()
    {
        var consultas = await _consultaRepository.GetAllAsync();
        return Ok(consultas.Select(ToResponse));
    }

    /// <summary>Busca uma consulta pelo Id.</summary>
    /// <param name="id">Identificador da consulta.</param>
    /// <response code="200">Consulta encontrada.</response>
    /// <response code="404">Consulta não encontrada.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultaResponse>> GetById(int id)
    {
        var consulta = await _consultaRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Consulta com Id {id} não foi encontrada.");

        return Ok(ToResponse(consulta));
    }

    /// <summary>Cadastra uma nova consulta vinculada a um pet existente.</summary>
    /// <param name="request">Dados da consulta.</param>
    /// <response code="201">Consulta criada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Pet referenciado não foi encontrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultaResponse>> Create(ConsultaRequest request)
    {
        var consulta = new Consulta(request.DataConsulta, request.Observacoes, request.PetId);
        await _consultaRepository.AddAsync(consulta);

        var response = ToResponse(consulta);
        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, response);
    }

    /// <summary>Atualiza os dados de uma consulta existente.</summary>
    /// <param name="id">Identificador da consulta.</param>
    /// <param name="request">Novos dados da consulta.</param>
    /// <response code="200">Consulta atualizada com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Consulta ou Pet não encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultaResponse>> Update(int id, ConsultaRequest request)
    {
        var consulta = await _consultaRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Consulta com Id {id} não foi encontrada.");

        consulta.Atualizar(request.DataConsulta, request.Observacoes);
        await _consultaRepository.UpdateAsync(consulta);

        return Ok(ToResponse(consulta));
    }

    /// <summary>Remove uma consulta.</summary>
    /// <param name="id">Identificador da consulta.</param>
    /// <response code="204">Consulta removida com sucesso.</response>
    /// <response code="404">Consulta não encontrada.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var consulta = await _consultaRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Consulta com Id {id} não foi encontrada.");

        await _consultaRepository.DeleteAsync(consulta);
        return NoContent();
    }

    private static ConsultaResponse ToResponse(Consulta consulta)
        => new(consulta.Id, consulta.DataConsulta, consulta.Observacoes, consulta.PetId);
}
