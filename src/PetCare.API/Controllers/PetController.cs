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
/// Gerencia o cadastro de pets vinculados a um tutor.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PetController : ControllerBase
{
    private readonly IPetRepository _petRepository;

    public PetController(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    /// <summary>Lista todos os pets cadastrados.</summary>
    /// <response code="200">Retorna a lista de pets.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PetResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PetResponse>>> GetAll()
    {
        var pets = await _petRepository.GetAllAsync();
        return Ok(pets.Select(ToResponse));
    }

    /// <summary>Busca um pet pelo Id.</summary>
    /// <param name="id">Identificador do pet.</param>
    /// <response code="200">Pet encontrado.</response>
    /// <response code="404">Pet não encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponse>> GetById(int id)
    {
        var pet = await _petRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Pet com Id {id} não foi encontrado.");

        return Ok(ToResponse(pet));
    }

    /// <summary>Cadastra um novo pet vinculado a um tutor existente.</summary>
    /// <param name="request">Dados do pet.</param>
    /// <response code="201">Pet criado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Tutor referenciado não foi encontrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponse>> Create(PetRequest request)
    {
        var pet = new Pet(request.Nome, request.Especie, request.Raca, request.DataNascimento, request.TutorId);
        await _petRepository.AddAsync(pet);

        var response = ToResponse(pet);
        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, response);
    }

    /// <summary>Atualiza os dados de um pet existente.</summary>
    /// <param name="id">Identificador do pet.</param>
    /// <param name="request">Novos dados do pet.</param>
    /// <response code="200">Pet atualizado com sucesso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Pet ou Tutor não encontrado.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponse>> Update(int id, PetRequest request)
    {
        var pet = await _petRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Pet com Id {id} não foi encontrado.");

        pet.Atualizar(request.Nome, request.Especie, request.Raca, request.DataNascimento);
        await _petRepository.UpdateAsync(pet);

        return Ok(ToResponse(pet));
    }

    /// <summary>Remove um pet.</summary>
    /// <param name="id">Identificador do pet.</param>
    /// <response code="204">Pet removido com sucesso.</response>
    /// <response code="404">Pet não encontrado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var pet = await _petRepository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Pet com Id {id} não foi encontrado.");

        await _petRepository.DeleteAsync(pet);
        return NoContent();
    }

    private static PetResponse ToResponse(Pet pet)
        => new(pet.Id, pet.Nome, pet.Especie, pet.Raca, pet.DataNascimento, pet.TutorId);
}
