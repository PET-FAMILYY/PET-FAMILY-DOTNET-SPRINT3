using PetCare.Domain.Common;
using PetCare.Domain.Exceptions;

namespace PetCare.Domain.Entities;

/// <summary>
/// Representa um animal de estimação vinculado a um Tutor.
/// </summary>
public class Pet : BaseEntity
{
    private readonly List<Consulta> _consultas = new();
    private readonly List<Lembrete> _lembretes = new();

    public string Nome { get; private set; } = string.Empty;
    public string Especie { get; private set; } = string.Empty;
    public string? Raca { get; private set; }
    public DateTime DataNascimento { get; private set; }
    public int TutorId { get; private set; }
    public Tutor? Tutor { get; private set; }

    public IReadOnlyCollection<Consulta> Consultas => _consultas.AsReadOnly();
    public IReadOnlyCollection<Lembrete> Lembretes => _lembretes.AsReadOnly();

    protected Pet() { } // EF Core

    public Pet(string nome, string especie, string? raca, DateTime dataNascimento, int tutorId)
    {
        ValidarNome(nome);
        ValidarEspecie(especie);
        ValidarDataNascimento(dataNascimento);

        Nome = nome.Trim();
        Especie = especie.Trim();
        Raca = raca?.Trim();
        DataNascimento = dataNascimento;
        TutorId = tutorId;
    }

    public void Atualizar(string nome, string especie, string? raca, DateTime dataNascimento)
    {
        ValidarNome(nome);
        ValidarEspecie(especie);
        ValidarDataNascimento(dataNascimento);

        Nome = nome.Trim();
        Especie = especie.Trim();
        Raca = raca?.Trim();
        DataNascimento = dataNascimento;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do pet é obrigatório.");
    }

    private static void ValidarEspecie(string especie)
    {
        if (string.IsNullOrWhiteSpace(especie))
            throw new DomainException("A espécie do pet é obrigatória.");
    }

    private static void ValidarDataNascimento(DateTime dataNascimento)
    {
        if (dataNascimento.Date > DateTime.UtcNow.Date)
            throw new DomainException("A data de nascimento do pet não pode ser futura.");
    }
}
