using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.API.Hateoas;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Services;

namespace PetCare.API.Controllers;

/// <summary>
/// Gerencia o cadastro de tutores (responsáveis pelos pets).
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TutorController : ControllerBase
{
    private readonly ITutorService _service;

    public TutorController(ITutorService service) => _service = service;

    /// <summary>Lista tutores com paginação, ordenação e filtros.</summary>
    /// <remarks>
    /// Exemplo: `GET /api/tutor?nome=joao&amp;page=1&amp;pageSize=5&amp;sortBy=email&amp;sortDir=asc`.
    /// Campos de ordenação: Id, Nome, Email, Telefone.
    /// </remarks>
    /// <response code="200">Página de tutores com links de navegação (HATEOAS).</response>
    /// <response code="400">Campo de ordenação inválido.</response>
    [ProducesResponseType(typeof(PagedResponse<TutorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<TutorResponse>>> GetAll([FromQuery] TutorFilter filter)
    {
        var page = (await _service.ListarAsync(filter)).Map(WithLinks);
        return Ok(PagedResponse<TutorResponse>.From(page, Hateoas.Hateoas.PageLinks(Request, page)));
    }

    /// <summary>Busca um tutor pelo Id.</summary>
    /// <param name="id">Identificador do tutor.</param>
    /// <response code="200">Tutor encontrado (com links HATEOAS).</response>
    /// <response code="404">Tutor não encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TutorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TutorResponse>> GetById(int id)
        => Ok(WithLinks(await _service.ObterAsync(id)));

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
        var tutor = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = tutor.Id }, WithLinks(tutor));
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
        => Ok(WithLinks(await _service.AtualizarAsync(id, request)));

    /// <summary>Remove um tutor.</summary>
    /// <param name="id">Identificador do tutor.</param>
    /// <response code="204">Tutor removido com sucesso.</response>
    /// <response code="404">Tutor não encontrado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.RemoverAsync(id);
        return NoContent();
    }

    private TutorResponse WithLinks(TutorResponse tutor) => tutor with
    {
        Links = Hateoas.Hateoas.ForItem(
            self: Url.Action(nameof(GetById), new { id = tutor.Id })!,
            collection: Url.Action(nameof(GetAll))!,
            new Link(Url.Action("GetAll", "Pet", new { tutorId = tutor.Id })!, "pets", "GET"))
    };
}