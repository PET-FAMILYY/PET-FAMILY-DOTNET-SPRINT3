namespace PetCare.Application.DTOs;

public record RegisterRequest(string Nome, string Email, string Senha);

public record LoginRequest(string Email, string Senha);

public record LoginResponse(string Token, DateTime ExpiraEm, string Nome, string Email, string Role);