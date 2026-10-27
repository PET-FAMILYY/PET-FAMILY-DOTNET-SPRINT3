namespace PetCare.Application.Auditing;

/// <summary>Usuário autenticado da requisição atual (abstração para não acoplar a Application ao ASP.NET).</summary>
public interface ICurrentUser
{
    string Nome { get; }
}