using PetCare.Application.Common;

namespace PetCare.Application.DTOs;

public record TutorRequest(string Nome, string Email, string? Telefone);

public record TutorResponse(int Id, string Nome, string Email, string? Telefone) : ResourceBase;