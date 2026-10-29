using System.ComponentModel.DataAnnotations;
using PetCare.Application.Common;

namespace PetCare.Application.DTOs;

public record PetRequest(
    [Required, StringLength(100)] string Nome,
    [Required, StringLength(50)] string Especie,
    [StringLength(50)] string? Raca,
    DateTime DataNascimento,
    [Range(1, int.MaxValue, ErrorMessage = "TutorId deve ser maior que zero.")] int TutorId);

public record PetResponse(int Id, string Nome, string Especie, string? Raca, DateTime DataNascimento, int TutorId) : ResourceBase;