using Microsoft.AspNetCore.Mvc;
using PetCare.Application.DTOs;
using PetCare.Application.Interfaces;
using PetCare.Domain.Entities;
using PetCare.Domain.Exceptions;

namespace PetCare.API.Controllers;

/// <summary>
/// Gerencia lembretes (vacinas, retornos, medicações) associados a um pet.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LembreteController : ControllerBase
{
    private readonly ILembreteRepository _lembreteRepository;

    public LembreteController(ILembreteRepository lembreteRepository)
    {
        _lembreteRepository = lembreteRepository;
    }

    /// <summary>Lista todos os lembretes cadastrados.</summary>
    /// <response code="200">Retorna a lista de lembretes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LembreteResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LembreteResponse>>> GetAll()
    {
        var lembretes = await _lembreteRepository.GetAllAsync();
        return Ok(lembretes.Select(ToResponse));
    }

    /// <summary>Busca um lembrete pelo Id.</summary>
    /// <param name="id">Identificador do lembrete.</param>
    /// <response code="200">Lembrete encontrado.</response>
    /// <response code="404">Lembrete não encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LembreteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LembreteResponse>> GetById(int id)
    {
        var lembrete = await _lembreteRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Lembrete com Id {id} não foi encontrado.");

        return Ok(ToResponse(lembrete));
    }

    /// <summary>Cadastra um novo lembrete vinculado a um pet existente.</summary>
    /// <param name="request">Dados do lembrete.</param>
    /// <response code="201">Lembrete criado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Pet referenciado não foi encontrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LembreteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LembreteResponse>> Create(LembreteRequest request)
    {
        var lembrete = new Lembrete(request.Titulo, request.Descricao, request.DataLembrete, request.PetId);
        await _lembreteRepository.AddAsync(lembrete);

        var response = ToResponse(lembrete);
        return CreatedAtAction(nameof(GetById), new { id = lembrete.Id }, response);
    }

    /// <summary>Atualiza os dados de um lembrete existente.</summary>
    /// <param name="id">Identificador do lembrete.</param>
    /// <param name="request">Novos dados do lembrete.</param>
    /// <response code="200">Lembrete atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Lembrete ou Pet não encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LembreteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LembreteResponse>> Update(int id, LembreteRequest request)
    {
        var lembrete = await _lembreteRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Lembrete com Id {id} não foi encontrado.");

        lembrete.Atualizar(request.Titulo, request.Descricao, request.DataLembrete);
        await _lembreteRepository.UpdateAsync(lembrete);

        return Ok(ToResponse(lembrete));
    }

    /// <summary>Remove um lembrete.</summary>
    /// <param name="id">Identificador do lembrete.</param>
    /// <response code="204">Lembrete removido com sucesso.</response>
    /// <response code="404">Lembrete não encontrado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var lembrete = await _lembreteRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Lembrete com Id {id} não foi encontrado.");

        await _lembreteRepository.DeleteAsync(lembrete);
        return NoContent();
    }

    private static LembreteResponse ToResponse(Lembrete lembrete)
        => new(lembrete.Id, lembrete.Titulo, lembrete.Descricao, lembrete.DataLembrete, lembrete.PetId);
}
