using System.ComponentModel.DataAnnotations;

namespace PetCare.Application.DTOs;

public record RegisterRequest(
    [Required, StringLength(200)] string Nome,
    [Required, EmailAddress, StringLength(200)] string Email,
    [Required, StringLength(100, MinimumLength = 6)] string Senha);

public record RegisterResponse(int Id, string Nome, string Email);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Senha);

public record LoginResponse(string Token, DateTime ExpiraEm, string Nome, string Email, string Role);