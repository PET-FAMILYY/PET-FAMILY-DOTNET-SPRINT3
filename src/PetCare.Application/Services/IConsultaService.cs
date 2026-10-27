using PetCare.Application.Common;
using PetCare.Application.DTOs;

namespace PetCare.Application.Services;

public interface IConsultaService
{
    Task<PagedResult<ConsultaResponse>> ListarAsync(ConsultaFilter filtro);
    Task<ConsultaResponse> ObterAsync(int id);
    Task<ConsultaResponse> CriarAsync(ConsultaRequest request);
    Task<ConsultaResponse> AtualizarAsync(int id, ConsultaRequest request);
    Task RemoverAsync(int id);
}