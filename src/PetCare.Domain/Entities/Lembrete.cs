using PetCare.Domain.Common;
using PetCare.Domain.Exceptions;

namespace PetCare.Domain.Entities;

/// <summary>
/// Representa um lembrete (vacina, retorno, medicação, etc.) associado a um Pet.
/// </summary>
public class Lembrete : BaseEntity
{
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public DateTime DataLembrete { get; private set; }
    public int PetId { get; private set; }
    public Pet? Pet { get; private set; }

    protected Lembrete() { } // EF Core

    public Lembrete(string titulo, string? descricao, DateTime dataLembrete, int petId)
    {
        ValidarTitulo(titulo);
        ValidarDataLembrete(dataLembrete);

        Titulo = titulo.Trim();
        Descricao = descricao?.Trim();
        DataLembrete = dataLembrete;
        PetId = petId;
    }

    public void Atualizar(string titulo, string? descricao, DateTime dataLembrete)
    {
        ValidarTitulo(titulo);
        ValidarDataLembrete(dataLembrete);

        Titulo = titulo.Trim();
        Descricao = descricao?.Trim();
        DataLembrete = dataLembrete;
    }

    private static void ValidarTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título do lembrete é obrigatório.");
    }

    private static void ValidarDataLembrete(DateTime dataLembrete)
    {
        if (dataLembrete.Date < DateTime.UtcNow.Date)
            throw new DomainException("A data do lembrete não pode ser anterior à data atual.");
    }
}
