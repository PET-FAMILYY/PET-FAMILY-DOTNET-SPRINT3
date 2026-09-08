namespace PetCare.Application.DTOs;

public record ConsultaRequest(DateTime DataConsulta, string? Observacoes, int PetId);

public record ConsultaResponse(int Id, DateTime DataConsulta, string? Observacoes, int PetId);
