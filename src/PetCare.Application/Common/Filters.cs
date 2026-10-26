namespace PetCare.Application.Common;

public class TutorFilter : QueryParameters
{
    /// <summary>Contém (sem diferenciar maiúsculas).</summary>
    public string? Nome { get; set; }
    public string? Email { get; set; }
}

public class PetFilter : QueryParameters
{
    public string? Nome { get; set; }
    public string? Especie { get; set; }
    public int? TutorId { get; set; }
}

public class ConsultaFilter : QueryParameters
{
    public int? PetId { get; set; }
    /// <summary>Data inicial (inclusive).</summary>
    public DateTime? De { get; set; }
    /// <summary>Data final (inclusive).</summary>
    public DateTime? Ate { get; set; }
}

public class LembreteFilter : QueryParameters
{
    public int? PetId { get; set; }
    public string? Titulo { get; set; }
    public DateTime? De { get; set; }
    public DateTime? Ate { get; set; }
}

public class AuditLogFilter : QueryParameters
{
    /// <summary>Tutor, Pet, Consulta ou Lembrete.</summary>
    public string? Entidade { get; set; }
    public int? EntidadeId { get; set; }
    /// <summary>Criacao, Atualizacao ou Remocao.</summary>
    public string? Acao { get; set; }
}