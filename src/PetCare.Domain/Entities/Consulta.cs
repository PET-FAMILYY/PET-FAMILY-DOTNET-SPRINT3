using PetCare.Domain.Common;
using PetCare.Domain.Exceptions;

namespace PetCare.Domain.Entities;

/// <summary>
/// Representa uma consulta veterinária realizada para um Pet.
/// </summary>
public class Consulta : BaseEntity
{
    public DateTime DataConsulta { get; private set; }
    public string? Observacoes { get; private set; }
    public int PetId { get; private set; }
    public Pet? Pet { get; private set; }

    protected Consulta() { } // EF Core

    public Consulta(DateTime dataConsulta, string? observacoes, int petId)
    {
        ValidarDataConsulta(dataConsulta);

        DataConsulta = dataConsulta;
        Observacoes = observacoes?.Trim();
        PetId = petId;
    }

    public void Atualizar(DateTime dataConsulta, string? observacoes)
    {
        ValidarDataConsulta(dataConsulta);

        DataConsulta = dataConsulta;
        Observacoes = observacoes?.Trim();
    }

    private static void ValidarDataConsulta(DateTime dataConsulta)
    {
        if (dataConsulta == default)
            throw new DomainException("A data da consulta é obrigatória.");
    }
}
