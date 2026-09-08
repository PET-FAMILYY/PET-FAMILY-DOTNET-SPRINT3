namespace PetCare.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma regra de negócio de domínio é violada
/// (ex.: campo obrigatório ausente, valor fora do intervalo permitido).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
