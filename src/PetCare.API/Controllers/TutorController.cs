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
/// Gerencia o cadastro de tutores (responsáveis pelos pets).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TutorController : ControllerBase
{
    private readonly ITutorRepository _tutorRepository;

    public TutorController(ITutorRepository tutorRepository)
    {
        _tutorRepository = tutorRepository;
    }

    /// <summary>Lista todos os tutores cadastrados.</summary>
    /// <response code="200">Retorna a lista de tutores.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TutorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TutorResponse>>> GetAll()
    {
        var tutores = await _tutorRepository.GetAllAsync();
        return Ok(tutores.Select(ToResponse));
    }

    /// <summary>Busca um tutor pelo Id.</summary>
    /// <param name="id">Identificador do tutor.</param>
    /// <response code="200">Tutor encontrado.</response>
    /// <response code="404">Tutor não encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TutorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TutorResponse>> GetById(int id)
    {
        var tutor = await _tutorRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Tutor com Id {id} não foi encontrado.");

        return Ok(ToResponse(tutor));
    }

    /// <summary>Cadastra um novo tutor.</summary>
    /// <param name="request">Dados do tutor.</param>
    /// <response code="201">Tutor criado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="409">Já existe um tutor cadastrado com o mesmo e-mail.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TutorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TutorResponse>> Create(TutorRequest request)
    {
        if (await _tutorRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException($"Já existe um tutor cadastrado com o e-mail '{request.Email}'.");

        var tutor = new Tutor(request.Nome, request.Email, request.Telefone);
        await _tutorRepository.AddAsync(tutor);

        var response = ToResponse(tutor);
        return CreatedAtAction(nameof(GetById), new { id = tutor.Id }, response);
    }

    /// <summary>Atualiza os dados de um tutor existente.</summary>
    /// <param name="id">Identificador do tutor.</param>
    /// <param name="request">Novos dados do tutor.</param>
    /// <response code="200">Tutor atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Tutor não encontrado.</response>
    /// <response code="409">E-mail já utilizado por outro tutor.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TutorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TutorResponse>> Update(int id, TutorRequest request)
    {
        var tutor = await _tutorRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Tutor com Id {id} não foi encontrado.");

        if (await _tutorRepository.ExistsByEmailAsync(request.Email, ignoreId: id))
            throw new ConflictException($"Já existe um tutor cadastrado com o e-mail '{request.Email}'.");

        tutor.Atualizar(request.Nome, request.Email, request.Telefone);
        await _tutorRepository.UpdateAsync(tutor);

        return Ok(ToResponse(tutor));
    }

    /// <summary>Remove um tutor.</summary>
    /// <param name="id">Identificador do tutor.</param>
    /// <response code="204">Tutor removido com sucesso.</response>
    /// <response code="404">Tutor não encontrado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var tutor = await _tutorRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Tutor com Id {id} não foi encontrado.");

        await _tutorRepository.DeleteAsync(tutor);
        return NoContent();
    }

    private static TutorResponse ToResponse(Tutor tutor)
        => new(tutor.Id, tutor.Nome, tutor.Email, tutor.Telefone);
}
