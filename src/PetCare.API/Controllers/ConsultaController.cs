using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.API.Hateoas;
using PetCare.Application.Common;
using PetCare.Application.DTOs;
using PetCare.Application.Services;

namespace PetCare.API.Controllers;

/// <summary>
/// Gerencia as consultas veterinárias de um pet.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConsultaController : ControllerBase
{
    private readonly IConsultaService _service;

    public ConsultaController(IConsultaService service) => _service = service;

    /// <summary>Lista consultas com paginação, ordenação e filtros.</summary>
    /// <remarks>
    /// Exemplo: `GET /api/consulta?petId=1&amp;de=2026-01-01&amp;ate=2026-12-31&amp;page=1&amp;pageSize=10&amp;sortBy=dataConsulta&amp;sortDir=desc`.
    /// Campos de ordenação: Id, DataConsulta, Observacoes, PetId.
    /// </remarks>
    /// <response code="200">Página de consultas com links de navegação (HATEOAS).</response>
    /// <response code="400">Campo de ordenação inválido.</response>
    [ProducesResponseType(typeof(PagedResponse<ConsultaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ConsultaResponse>>> GetAll([FromQuery] ConsultaFilter filter)
    {
        var page = (await _service.ListarAsync(filter)).Map(WithLinks);
        return Ok(PagedResponse<ConsultaResponse>.From(page, Hateoas.Hateoas.PageLinks(Request, page)));
    }

    /// <summary>Busca uma consulta pelo Id.</summary>
    /// <param name="id">Identificador da consulta.</param>
    /// <response code="200">Consulta encontrada (com links HATEOAS).</response>
    /// <response code="404">Consulta não encontrada.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultaResponse>> GetById(int id)
        => Ok(WithLinks(await _service.ObterAsync(id)));

    /// <summary>Cadastra uma nova consulta para um pet existente.</summary>
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
        var consulta = await _service.CriarAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, WithLinks(consulta));
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
        => Ok(WithLinks(await _service.AtualizarAsync(id, request)));

    /// <summary>Remove uma consulta.</summary>
    /// <param name="id">Identificador da consulta.</param>
    /// <response code="204">Consulta removida com sucesso.</response>
    /// <response code="404">Consulta não encontrada.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.RemoverAsync(id);
        return NoContent();
    }

    private ConsultaResponse WithLinks(ConsultaResponse consulta) => consulta with
    {
        Links = Hateoas.Hateoas.ForItem(
            self: Url.Action(nameof(GetById), new { id = consulta.Id })!,
            collection: Url.Action(nameof(GetAll))!,
            new Link(Url.Action("GetById", "Pet", new { id = consulta.PetId })!, "pet", "GET"))
    };
}