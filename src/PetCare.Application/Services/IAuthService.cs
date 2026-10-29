using PetCare.Application.DTOs;

namespace PetCare.Application.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegistrarAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
}