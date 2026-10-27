using System.Threading.Tasks;
using PetCare.Application.Common;

namespace PetCare.Application.Auditing;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry);
    Task<PagedResult<AuditLogEntry>> GetPagedAsync(AuditLogFilter filter);
}