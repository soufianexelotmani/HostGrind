using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Options;
using HostGrid.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HostGrid.Api.Services;

public sealed class TokenService(SecurityOptions options, SymmetricSecurityKey key,
    AuthenticationRepository repository, UserManager<ApplicationUser> users,
    CurrentUserService currentUser, SecurityAuditService audit, TimeProvider clock)
{
    internal const string Cookie = "HostGrid.Refresh";
    internal static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    internal static string NewSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    internal static Guid Family(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue("sid"), out var id) ? id : Guid.Empty;

    internal async Task<SessionResult> IssueAsync(ApplicationUser user, CurrentUserView view, bool mfa,
        CancellationToken token, RefreshToken? previous = null, string[]? recoveryCodes = null)
    {
        var raw = NewSecret();
        var now = clock.GetUtcNow().UtcDateTime;
        var stamp = Hash(await users.GetSecurityStampAsync(user));
        var next = new RefreshToken
        {
            UserId = user.Id, FamilyId = previous?.FamilyId ?? Guid.NewGuid(),
            TokenHash = Hash(raw), ExpiresAt = now.Add(RefreshLifetime),
            SecurityStampHash = stamp, MfaVerified = mfa
        };
        repository.Sessions.Add(next);
        await repository.Database.SaveChangesAsync(token);
        if (previous is not null)
        {
            previous.RevokedAt = now;
            previous.ReplacedById = next.Id;
            await repository.Database.SaveChangesAsync(token);
        }

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()), new Claim("sid", next.FamilyId.ToString()),
            new Claim("sst", stamp), new Claim("jti", Guid.NewGuid().ToString())
        };
        var jwt = new JwtSecurityToken(options.Issuer, options.Audience, claims, now, now.AddMinutes(15),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new(new AuthResponse("authenticated", new JwtSecurityTokenHandler().WriteToken(jwt),
            view, RecoveryCodes: recoveryCodes), raw);
    }

    internal async Task<SessionResult?> RefreshAsync(string? raw, CancellationToken token)
    {
        var previous = await ResolveCookieAsync(raw, token);
        if (previous is null)
        {
            return null;
        }
        await using var transaction = await repository.LockAsync(previous.UserId.ToString("N"), token);
        await repository.Database.Entry(previous).ReloadAsync(token);
        var now = clock.GetUtcNow().UtcDateTime;
        if (previous.ReplacedById is not null)
        {
            await repository.RevokeAsync(previous.UserId, now, token, previous.FamilyId);
            await audit.RecordAsync("refresh_reused", token, previous.UserId);
            await transaction.CommitAsync(token);
            return null;
        }
        if (previous.RevokedAt is not null || previous.ExpiresAt <= now)
        {
            return null;
        }
        var user = await users.FindByIdAsync(previous.UserId.ToString());
        if (!CurrentUserService.IsEnabled(user) ||
            previous.SecurityStampHash != Hash(await users.GetSecurityStampAsync(user!)))
        {
            return null;
        }
        var view = await currentUser.DescribeAsync(user!, token);
        if (!CurrentUserService.HasAccess(view) || CurrentUserService.RequiresMfa(view) && !previous.MfaVerified)
        {
            return null;
        }
        var result = await IssueAsync(user!, view, previous.MfaVerified, token, previous);
        await transaction.CommitAsync(token);
        return result;
    }

    internal async Task LogoutAsync(string? raw, ClaimsPrincipal principal, CancellationToken token)
    {
        var session = await ResolveCookieAsync(raw, token);
        var userId = session?.UserId ?? CurrentUserService.UserId(principal);
        var family = session?.FamilyId ?? Family(principal);
        if (userId == Guid.Empty || family == Guid.Empty)
        {
            return;
        }
        await using var transaction = await repository.LockAsync(userId.ToString("N"), token);
        await repository.RevokeAsync(userId, clock.GetUtcNow().UtcDateTime, token, family);
        await audit.RecordAsync("session_revoked", token, userId);
        await transaction.CommitAsync(token);
    }

    internal async Task<CurrentUserView?> ValidateAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var id = CurrentUserService.UserId(principal);
        var family = Family(principal);
        if (id == Guid.Empty || family == Guid.Empty)
        {
            return null;
        }
        var now = clock.GetUtcNow().UtcDateTime;
        var session = await repository.Sessions.AsNoTracking().SingleOrDefaultAsync(row =>
            row.UserId == id && row.FamilyId == family && row.RevokedAt == null && row.ExpiresAt > now, token);
        var user = await users.FindByIdAsync(id.ToString());
        if (session is null || !CurrentUserService.IsEnabled(user))
        {
            return null;
        }
        var stamp = Hash(await users.GetSecurityStampAsync(user!));
        if (session.SecurityStampHash != stamp || principal.FindFirstValue("sst") != stamp)
        {
            return null;
        }
        var view = await currentUser.DescribeAsync(user!, token);
        return CurrentUserService.HasAccess(view) && (!CurrentUserService.RequiresMfa(view) || session.MfaVerified)
            ? view : null;
    }

    private Task<RefreshToken?> ResolveCookieAsync(string? raw, CancellationToken token)
    {
        if (raw is not { Length: 43 })
        {
            return Task.FromResult<RefreshToken?>(null);
        }
        var hash = Hash(raw);
        return repository.Sessions.SingleOrDefaultAsync(row => row.TokenHash == hash, token);
    }
}

internal sealed record SessionResult(AuthResponse Response, string? RefreshToken = null);
