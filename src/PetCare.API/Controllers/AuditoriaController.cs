using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCare.API.Hateoas;
using PetCare.Application.Auditing;
using PetCare.Application.Common;

namespace PetCare.API.Controllers;

/// <summary>
/// Consulta a trilha de auditoria armazenada no MongoDB (NoSQL).
/// </summary>
[Authorize]
[ApiController]
[Route("api/auditoria")]
[Produces("application/json")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditLogRepository _repository;

    public AuditoriaController(IAuditLogRepository repository) => _repository = repository;

    /// <summary>Lista eventos de auditoria com paginação, ordenação e filtros.</summary>
    /// <remarks>
    /// Exemplo: `GET /api/auditoria?entidade=Pet&amp;acao=Criacao&amp;page=1&amp;pageSize=10&amp;sortBy=dataHora&amp;sortDir=desc`.
    /// Campos de ordenação: DataHora (padrão: mais recentes primeiro), Entidade, Acao, Usuario.
    /// </remarks>
    /// <response code="200">Página de eventos de auditoria.</response>
    /// <response code="400">Campo de ordenação inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AuditLogEntry>>> GetAll([FromQuery] AuditLogFilter filter)
    {
        var page = await _repository.GetPagedAsync(filter);
        return Ok(PagedResponse<AuditLogEntry>.From(page, Hateoas.Hateoas.PageLinks(Request, page)));
    }
}