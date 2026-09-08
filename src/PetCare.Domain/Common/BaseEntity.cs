namespace PetCare.Domain.Common;

/// <summary>
/// Classe base para todas as entidades de domínio, fornecendo identidade única.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; protected set; }
}
