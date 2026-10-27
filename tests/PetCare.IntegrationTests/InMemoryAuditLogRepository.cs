using System.Collections.Concurrent;
using PetCare.Application.Auditing;
using PetCare.Application.Common;

namespace PetCare.IntegrationTests;

/// <summary>Substituto em memória do MongoAuditLogRepository, usado só nos testes.</summary>
public class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly ConcurrentQueue<AuditLogEntry> _entries = new();

    public Task AddAsync(AuditLogEntry entry)
    {
        _entries.Enqueue(entry);
        return Task.CompletedTask;
    }

    public Task<PagedResult<AuditLogEntry>> GetPagedAsync(AuditLogFilter filter)
    {
        IEnumerable<AuditLogEntry> query = _entries;

        if (!string.IsNullOrWhiteSpace(filter.Entidade))
            query = query.Where(e => e.Entidade.Equals(filter.Entidade, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Acao))
            query = query.Where(e => e.Acao.Equals(filter.Acao, StringComparison.OrdinalIgnoreCase));
        if (filter.EntidadeId.HasValue)
            query = query.Where(e => e.EntidadeId == filter.EntidadeId.Value);

        var lista = query.OrderByDescending(e => e.DataHora).ToList();
        var itens = lista.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

        return Task.FromResult(new PagedResult<AuditLogEntry>(itens, filter.Page, filter.PageSize, lista.Count));
    }
}