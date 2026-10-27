using System.Threading.Tasks;
using PetCare.Application.Common;
using PetCare.Application.DTOs;

namespace PetCare.Application.Services;

public interface IPetService
{
    Task<PagedResult<PetResponse>> ListarAsync(PetFilter filtro);
    Task<PetResponse> ObterAsync(int id);
    Task<PetResponse> CriarAsync(PetRequest request);
    Task<PetResponse> AtualizarAsync(int id, PetRequest request);
    Task RemoverAsync(int id);
}