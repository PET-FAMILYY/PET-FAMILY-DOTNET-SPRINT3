using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PetCare.Application.Auditing;

namespace PetCare.Infrastructure.Mongo;

public class AuditLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Entidade { get; set; } = string.Empty;
    public int EntidadeId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime DataHora { get; set; }

    public string? Detalhes { get; set; }

    public static AuditLogDocument From(AuditLogEntry e) => new()
    {
        Entidade = e.Entidade,
        EntidadeId = e.EntidadeId,
        Acao = e.Acao,
        Usuario = e.Usuario,
        DataHora = e.DataHora,
        Detalhes = e.Detalhes
    };

    public AuditLogEntry ToEntry()
        => new(Entidade, EntidadeId, Acao, Usuario, DataHora, Detalhes) { Id = Id };
}