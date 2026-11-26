using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;

namespace HostGrid.Api.Services;

public sealed class SecurityAuditService(AuthenticationRepository repository, IHttpContextAccessor accessor,
    ITenantContext tenant, TimeProvider clock)
{
    internal Task RecordAsync(string action, CancellationToken token, Guid? subjectId = null)
    {
        var http = accessor.HttpContext;
        var actorId = http is null ? Guid.Empty : CurrentUserService.UserId(http.User);
        repository.Database.Add(new SecurityEvent
        {
            Action = action,
            ActorId = actorId == Guid.Empty ? null : actorId,
            SubjectId = subjectId,
            OrganizationId = tenant.AuthorizedOrganizationId,
            CorrelationId = http?.TraceIdentifier is { Length: <= 100 } trace ? trace : Guid.NewGuid().ToString("N"),
            OccurredAt = clock.GetUtcNow().UtcDateTime
        });
        return repository.Database.SaveChangesAsync(token);
    }
}
