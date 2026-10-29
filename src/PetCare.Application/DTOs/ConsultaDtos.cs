using System.ComponentModel.DataAnnotations;
using PetCare.Application.Common;

namespace PetCare.Application.DTOs;

public record ConsultaRequest(
    DateTime DataConsulta,
    [StringLength(1000)] string? Observacoes,
    [Range(1, int.MaxValue, ErrorMessage = "PetId deve ser maior que zero.")] int PetId);

public record ConsultaResponse(int Id, DateTime DataConsulta, string? Observacoes, int PetId) : ResourceBase;