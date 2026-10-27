using System.Threading.Tasks;

namespace PetCare.Application.Auditing;

public interface IAuditTrail
{
    Task RegistrarAsync(string entidade, int entidadeId, string acao, string? detalhes);
}