namespace HostGrid.Api.DTOs.Auth;

public sealed record LoginRequest(string Email, string Password);
public sealed record EmailRequest(string Email);
public sealed record PasswordLinkRequest(string Email, string Token, string Password);
public sealed record PasswordChangeRequest(string CurrentPassword, string Password);
public sealed record MfaChallengeRequest(string Challenge);
public sealed record MfaStartRequest(string Password);
public sealed record MfaVerifyRequest(string Challenge, string Code, bool Recovery = false);
public sealed record MfaResetRequest(string Password, string Code, bool Recovery = false);
public sealed record MembershipView(Guid OrganizationId, string Role, Guid? OwnerId);
public sealed record CurrentUserView(Guid Id, string Email, bool SuperAdmin, bool MfaEnabled,
    IReadOnlyList<MembershipView> Memberships);
public sealed record AuthResponse(string Status, string? AccessToken = null,
    CurrentUserView? User = null, string? Challenge = null, string[]? RecoveryCodes = null);
public sealed record EnrollmentView(string SetupKey, string AuthenticatorUri);
public sealed record InviteRequest(string Email, string Name, string Role);
public sealed record MembershipChangeRequest(bool Active, string Role);
public sealed record OrganizationRequest(string Name, string Reference);
public sealed record OrganizationView(Guid Id, string Name, string Reference, bool IsActive);
public sealed record AccountMembershipView(Guid UserId, string Email, string Role, bool MembershipActive, bool AccountActive);
public sealed record SecurityEventView(string Action, DateTime OccurredAt, Guid? ActorId,
    Guid? SubjectId, Guid? OrganizationId, string CorrelationId);
