using HostGrid.Api.Entities.System;
using HostGrid.Api.Repositories;

namespace HostGrid.Api.Services;

public sealed class PortfolioAudit(PortfolioRepository repository, PortfolioAccess access,
    TimeProvider clock, IHttpContextAccessor http)
{
    internal Task SaveAsync(string action, string type, Guid id, CancellationToken token)
    {
        Record(action, type, id);
        return repository.Database.SaveChangesAsync(token);
    }

    internal void Record(string action, string type, Guid id, string? oldValues = null, string? newValues = null)
    {
        repository.Database.Add(new AuditLog
        {
            OrganizationId = access.OrganizationId, ActorId = access.Actor, Action = action,
            ResourceType = type, ResourceId = id, OccurredAt = clock.GetUtcNow().UtcDateTime,
            CorrelationId = http.HttpContext!.TraceIdentifier, OldValuesJson = oldValues, NewValuesJson = newValues
        });
    }
}
