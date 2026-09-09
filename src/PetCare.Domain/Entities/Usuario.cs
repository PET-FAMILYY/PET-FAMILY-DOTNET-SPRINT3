using PetCare.Domain.Common;
using PetCare.Domain.Exceptions;

namespace PetCare.Domain.Entities;

/// <summary>
/// Representa um usuário autenticável da API.
/// </summary>
public class Usuario : BaseEntity
{
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "User";

    protected Usuario() { } // EF Core

    public Usuario(string nome, string email, string senhaHash, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new DomainException("A senha do usuário é obrigatória.");

        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
        Role = role;
    }
}