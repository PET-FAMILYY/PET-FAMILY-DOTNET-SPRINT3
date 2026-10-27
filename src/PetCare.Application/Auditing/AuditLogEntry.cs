using System;

namespace PetCare.Application.Auditing;

/// <summary>Evento de auditoria armazenado no MongoDB.</summary>
public record AuditLogEntry(
    string Entidade,
    int EntidadeId,
    string Acao,
    string Usuario,
    DateTime DataHora,
    string? Detalhes)
{
    public string? Id { get; init; }
}