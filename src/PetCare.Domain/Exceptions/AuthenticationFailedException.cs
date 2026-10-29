namespace PetCare.Domain.Exceptions;

/// <summary>
/// Lançada quando as credenciais informadas não são válidas (e-mail inexistente ou senha incorreta).
/// Mapeada para HTTP 401 pelo GlobalExceptionHandler.
/// </summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }

}