using System.ComponentModel.DataAnnotations;
using PetCare.Application.Common;

namespace PetCare.Application.DTOs;

public record LembreteRequest(
    [Required, StringLength(150)] string Titulo,
    [StringLength(500)] string? Descricao,
    DateTime DataLembrete,
    [Range(1, int.MaxValue, ErrorMessage = "PetId deve ser maior que zero.")] int PetId);

public record LembreteResponse(int Id, string Titulo, string? Descricao, DateTime DataLembrete, int PetId) : ResourceBase;