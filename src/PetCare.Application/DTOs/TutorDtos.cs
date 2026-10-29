using System.ComponentModel.DataAnnotations;
using PetCare.Application.Common;

namespace PetCare.Application.DTOs;

public record TutorRequest(
    [Required, StringLength(150)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [StringLength(20)] string? Telefone);

public record TutorResponse(int Id, string Nome, string Email, string? Telefone) : ResourceBase;