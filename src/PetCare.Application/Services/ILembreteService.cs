using PetCare.Application.Common;
using PetCare.Application.DTOs;

namespace PetCare.Application.Services;

public interface ILembreteService
{
    Task<PagedResult<LembreteResponse>> ListarAsync(LembreteFilter filtro);
    Task<LembreteResponse> ObterAsync(int id);
    Task<LembreteResponse> CriarAsync(LembreteRequest request);
    Task<LembreteResponse> AtualizarAsync(int id, LembreteRequest request);
    Task RemoverAsync(int id);
}