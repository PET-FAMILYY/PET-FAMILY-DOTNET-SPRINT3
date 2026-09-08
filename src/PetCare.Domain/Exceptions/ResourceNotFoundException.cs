namespace PetCare.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um recurso referenciado (por ID ou FK) não existe.
/// Mapeada para HTTP 404 pelo GlobalExceptionHandler.
/// </summary>
public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}
