using PetCare.Application.Common;
using PetCare.Application.DTOs;

namespace PetCare.Application.Services;

public interface ITutorService
{
    Task<PagedResult<TutorResponse>> ListarAsync(TutorFilter filtro);
    Task<TutorResponse> ObterAsync(int id);
    Task<TutorResponse> CriarAsync(TutorRequest request);
    Task<TutorResponse> AtualizarAsync(int id, TutorRequest request);
    Task RemoverAsync(int id);
}