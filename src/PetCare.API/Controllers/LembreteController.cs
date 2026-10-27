using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.API.Hateoas;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Services;

namespace PetCare.API.Controllers;

/// <summary>
/// Gerencia os lembretes (vacinas, retornos etc.) de um pet.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LembreteController : ControllerBase
{
    private readonly ILembreteService _service;

    public LembreteController(ILembreteService service) => _service = service;

    /// <summary>Lista lembretes com paginação, ordenação e filtros.</summary>
    /// <remarks>
    /// Exemplo: `GET /api/lembrete?petId=1&amp;titulo=vacina&amp;de=2026-01-01&amp;page=1&amp;pageSize=10&amp;sortBy=dataLembrete&amp;sortDir=asc`.
    /// Campos de ordenação: Id, Titulo, Descricao, DataLembrete, PetId.
    /// </remarks>
    /// <response code="200">Página de lembretes com links de navegação (HATEOAS).</response>
    /// <response code="400">Campo de ordenação inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LembreteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LembreteResponse>>> GetAll([FromQuery] LembreteFilter filter)
    {
        var result = await _service.ListarAsync(filter);
        return Ok(result with
        {
            Items = result.Items.Select(WithLinks).ToList(),
            Links = Hateoas.Hateoas.PageLinks(Request, result)
        });
    }

    /// <summary>Busca um lembrete pelo Id.</summary>
    /// <param name="id">Identificador do lembrete.</param>
    /// <response code="200">Lembrete encontrado (com links HATEOAS).</response>
    /// <response code="404">Lembrete não encontrado.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LembreteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LembreteResponse>> GetById(int id)
        => Ok(WithLinks(await _service.ObterAsync(id)));

    /// <summary>Cadastra um novo lembrete para um pet existente.</summary>
    /// <param name="request">Dados do lembrete.</param>
    /// <response code="201">Lembrete criado com sucesso.</response>
    /// <response code="400">Dados inválidos (ex.: data anterior à atual).</response>
    /// <response code="404">Pet referenciado não foi encontrado.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LembreteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LembreteResponse>> Create(LembreteRequest request)
    {
        var lembrete = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = lembrete.Id }, WithLinks(lembrete));
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
        => Ok(WithLinks(await _service.AtualizarAsync(id, request)));

    /// <summary>Remove um lembrete.</summary>
    /// <param name="id">Identificador do lembrete.</param>
    /// <response code="204">Lembrete removido com sucesso.</response>
    /// <response code="404">Lembrete não encontrado.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.RemoverAsync(id);
        return NoContent();
    }

    private LembreteResponse WithLinks(LembreteResponse lembrete) => lembrete with
    {
        Links = Hateoas.Hateoas.ForItem(
            self: Url.Action(nameof(GetById), new { id = lembrete.Id })!,
            collection: Url.Action(nameof(GetAll))!,
            new Link(Url.Action("GetById", "Pet", new { id = lembrete.PetId })!, "pet", "GET"))
    };
}