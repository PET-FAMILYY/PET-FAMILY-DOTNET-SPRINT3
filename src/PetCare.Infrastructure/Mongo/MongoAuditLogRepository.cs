using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PetCare.Application.Auditing;
using PetCare.Application.Common;

namespace PetCare.Infrastructure.Mongo;

public class MongoAuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditLogDocument> _collection;
    private readonly ILogger<MongoAuditLogRepository> _logger;

    public MongoAuditLogRepository(
        IMongoDatabase database,
        MongoSettings settings,
        ILogger<MongoAuditLogRepository> logger)
    {
        _collection = database.GetCollection<AuditLogDocument>(settings.AuditCollection);
        _logger = logger;
    }

    /// <summary>
    /// Best-effort: uma falha no MongoDB não pode derrubar a operação de negócio já concluída no Oracle.
    /// </summary>
    public async Task AddAsync(AuditLogEntry entry)
    {
        try
        {
            await _collection.InsertOneAsync(AuditLogDocument.From(entry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao gravar auditoria no MongoDB ({Entidade} {EntidadeId} {Acao}).",
                entry.Entidade, entry.EntidadeId, entry.Acao);
        }
    }

    public async Task<PagedResult<AuditLogEntry>> GetPagedAsync(AuditLogFilter filter)
    {
        var builder = Builders<AuditLogDocument>.Filter;
        var criterios = builder.Empty;

        if (!string.IsNullOrWhiteSpace(filter.Entidade))
            criterios &= builder.Regex(d => d.Entidade,
                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(filter.Entidade.Trim())}$", "i"));

        if (!string.IsNullOrWhiteSpace(filter.Acao))
            criterios &= builder.Regex(d => d.Acao,
                new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(filter.Acao.Trim())}$", "i"));

        if (filter.EntidadeId.HasValue)
            criterios &= builder.Eq(d => d.EntidadeId, filter.EntidadeId.Value);

        var total = await _collection.CountDocumentsAsync(criterios);

        // Ordenação restrita a campos conhecidos (padrão: mais recentes primeiro)
        var sortBuilder = Builders<AuditLogDocument>.Sort;
        var ordenar = (filter.SortBy?.ToLowerInvariant()) switch
        {
            null or "" => sortBuilder.Descending(d => d.DataHora),   // padrão: mais recentes primeiro
            "datahora" => Direcao(filter, d => d.DataHora),
            "entidade" => Direcao(filter, d => d.Entidade),
            "acao" => Direcao(filter, d => d.Acao),
            "usuario" => Direcao(filter, d => d.Usuario),
            _ => throw new ArgumentException(
                $"Campo de ordenação '{filter.SortBy}' inválido. Campos permitidos: DataHora, Entidade, Acao, Usuario.")
        };

        var documentos = await _collection
            .Find(criterios)
            .Sort(ordenar)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Limit(filter.PageSize)
            .ToListAsync();

        return new PagedResult<AuditLogEntry>(
            documentos.Select(d => d.ToEntry()).ToList(),
            filter.Page, filter.PageSize, (int)total);
    }

    private static SortDefinition<AuditLogDocument> Direcao(
        AuditLogFilter filter, System.Linq.Expressions.Expression<Func<AuditLogDocument, object>> campo)
        => filter.Descending
            ? Builders<AuditLogDocument>.Sort.Descending(campo)
            : Builders<AuditLogDocument>.Sort.Ascending(campo);
}