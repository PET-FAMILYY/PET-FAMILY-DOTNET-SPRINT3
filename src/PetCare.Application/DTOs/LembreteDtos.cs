namespace PetCare.Application.DTOs;

public record LembreteRequest(string Titulo, string? Descricao, DateTime DataLembrete, int PetId);

public record LembreteResponse(int Id, string Titulo, string? Descricao, DateTime DataLembrete, int PetId);
