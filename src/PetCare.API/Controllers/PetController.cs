using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.API.Hateoas;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Services;

namespace PetCare.API.Controllers;

/// <summary>
/// Gerencia o cadastro de pets vinculados a um tutor.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PetController : ControllerBase
{
    private readonly IPetService _service;

    public PetController(IPetService service) => _service = service;

    /// <summary>Lista pets com paginação, ordenação e filtros.</summary>
    /// <remarks>
    /// Exemplo: `GET /api/pet?especie=cachorro&amp;tutorId=1&amp;page=1&amp;pageSize=10&amp;sortBy=nome&amp;sortDir=desc`.
    /// Campos de ordenação: Id, Nome, Especie, Raca, DataNascimento, TutorId.
    /// </remarks>
    /// <response code="200">Página de pets com links de navegação (HATEOAS).</response>
    /// <response code="400">Campo de ordenação inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PetResponse>>> GetAll([FromQuery] PetFilter filter)
    {
        var page = (await _service.ListarAsync(filter)).Map(WithLinks);
        return Ok(PagedResponse<PetResponse>.From(page, Hateoas.Hateoas.PageLinks(Request, page)));
    }

    /// <summary>Busca um pet pelo Id.</summary>
    /// <param name="id">Identificador do pet.</param>
    /// <response code="200">Pet encontrado (com links HATEOAS).</response>
    /// <response code="404">Pet não encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetResponse>> GetById(int id)
        => Ok(WithLinks(await _service.ObterAsync(id)));

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
        var pet = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, WithLinks(pet));
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
        => Ok(WithLinks(await _service.AtualizarAsync(id, request)));

    /// <summary>Remove um pet.</summary>
    /// <param name="id">Identificador do pet.</param>
    /// <response code="204">Pet removido com sucesso.</response>
    /// <response code="404">Pet não encontrado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.RemoverAsync(id);
        return NoContent();
    }

    private PetResponse WithLinks(PetResponse pet) => pet with
    {
        Links = Hateoas.Hateoas.ForItem(
            self: Url.Action(nameof(GetById), new { id = pet.Id })!,
            collection: Url.Action(nameof(GetAll))!,
            new Link(Url.Action("GetById", "Tutor", new { id = pet.TutorId })!, "tutor", "GET"),
            new Link(Url.Action("GetAll", "Consulta", new { petId = pet.Id })!, "consultas", "GET"),
            new Link(Url.Action("GetAll", "Lembrete", new { petId = pet.Id })!, "lembretes", "GET"))
    };
}