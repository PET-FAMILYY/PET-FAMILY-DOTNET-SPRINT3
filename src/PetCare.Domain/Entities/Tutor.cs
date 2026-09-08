using System.Text.RegularExpressions;
using PetCare.Domain.Common;
using PetCare.Domain.Exceptions;

namespace PetCare.Domain.Entities;

/// <summary>
/// Representa o responsável (tutor) por um ou mais pets.
/// </summary>
public class Tutor : BaseEntity
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly List<Pet> _pets = new();

    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Telefone { get; private set; }

    public IReadOnlyCollection<Pet> Pets => _pets.AsReadOnly();

    protected Tutor() { } // EF Core

    public Tutor(string nome, string email, string? telefone)
    {
        ValidarNome(nome);
        ValidarEmail(email);

        Nome = nome.Trim();
        Email = email.Trim();
        Telefone = telefone?.Trim();
    }

    public void Atualizar(string nome, string email, string? telefone)
    {
        ValidarNome(nome);
        ValidarEmail(email);

        Nome = nome.Trim();
        Email = email.Trim();
        Telefone = telefone?.Trim();
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do tutor é obrigatório.");
    }

    private static void ValidarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do tutor é obrigatório.");

        if (!EmailRegex.IsMatch(email))
            throw new DomainException("O e-mail do tutor não possui um formato válido.");
    }
}
