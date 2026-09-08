namespace PetCare.Domain.Exceptions;

/// <summary>
/// Exceção lançada em conflitos de estado, como e-mail de Tutor duplicado.
/// Mapeada para HTTP 409 pelo GlobalExceptionHandler.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
