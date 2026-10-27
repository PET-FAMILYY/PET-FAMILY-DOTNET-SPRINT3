namespace PetCare.Application.Auditing;

public class AuditTrail : IAuditTrail
{
    private readonly IAuditLogRepository _repository;
    private readonly ICurrentUser _currentUser;

    public AuditTrail(IAuditLogRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public Task RegistrarAsync(string entidade, int entidadeId, string acao, string? detalhes)
        => _repository.AddAsync(new AuditLogEntry(
            entidade, entidadeId, acao, _currentUser.Nome, DateTime.UtcNow, detalhes));
}