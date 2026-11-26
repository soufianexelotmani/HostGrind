using Microsoft.AspNetCore.Identity;
using HostGrid.Api.Enums;

namespace HostGrid.Api.Entities.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; }
    public DateTime? ActivatedAt { get; set; }
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public Guid FamilyId { get; set; }
    public bool MfaVerified { get; set; }
    public string? SecurityStampHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class LoginChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public string SecurityStamp { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public sealed class SecurityEvent
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Action { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
}

public sealed class UserPreferences
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Language { get; set; } = null!;
    public Theme Theme { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
