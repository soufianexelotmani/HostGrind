using System.Security.Claims;
using System.Text.Json;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class AuthenticationService(UserManager<ApplicationUser> users, IUserStore<ApplicationUser> store,
    CurrentUserService current, AuthenticationRepository repository, TokenService tokens,
    SecurityAuditService audit, TimeProvider clock)
{
    private const string ReplayProvider = "HostGrid.Mfa";
    private const string ReplayToken = "RecentTotpProofs";
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal async Task<SessionResult?> LoginAsync(LoginRequest request, CancellationToken token)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            await audit.RecordAsync("login_failed", token);
            return null;
        }
        await using var transaction = await repository.LockAsync(user.Id.ToString("N"), token);
        await repository.Database.Entry(user).ReloadAsync(token);
        if (!await VerifyPasswordAsync(user, request.Password))
        {
            await audit.RecordAsync("login_failed", token, user.Id);
            await transaction.CommitAsync(token);
            return null;
        }
        var view = await current.DescribeAsync(user, token);
        if (!CurrentUserService.HasAccess(view))
        {
            await audit.RecordAsync("login_failed", token, user.Id);
            await transaction.CommitAsync(token);
            return null;
        }

        SessionResult result;
        if (CurrentUserService.RequiresMfa(view))
        {
            result = new(new AuthResponse(user.TwoFactorEnabled ? "mfa_required" : "mfa_enrollment_required",
                Challenge: await CreateChallengeAsync(user, token)));
        }
        else
        {
            RequireSuccess(await users.ResetAccessFailedCountAsync(user));
            result = await tokens.IssueAsync(user, view, false, token);
            await audit.RecordAsync("login_succeeded", token, user.Id);
        }
        await transaction.CommitAsync(token);
        return result;
    }

    internal async Task<AuthResponse> StartOptionalEnrollmentAsync(ClaimsPrincipal principal, string password, CancellationToken token)
    {
        var id = CurrentUserService.UserId(principal);
        await using var transaction = await repository.LockAsync(id.ToString("N"), token);
        var user = await FindAsync(id);
        await repository.Database.Entry(user).ReloadAsync(token);
        if (user.TwoFactorEnabled || !await VerifyPasswordAsync(user, password))
        {
            await audit.RecordAsync("mfa_enrollment_denied", token, id);
            await transaction.CommitAsync(token);
            throw SecurityRequestException.InvalidProof();
        }
        var challenge = await CreateChallengeAsync(user, token);
        await transaction.CommitAsync(token);
        return new("mfa_enrollment_required", Challenge: challenge);
    }

    internal async Task<EnrollmentView> EnrollmentAsync(string rawChallenge, CancellationToken token)
    {
        var challenge = await FindChallengeAsync(rawChallenge, token);
        await using var transaction = await repository.LockAsync(challenge.UserId.ToString("N"), token);
        var user = await ValidateChallengeAsync(challenge, token);
        if (user.TwoFactorEnabled)
        {
            throw SecurityRequestException.InvalidProof();
        }
        var key = await users.GetAuthenticatorKeyAsync(user);
        if (key is null)
        {
            RequireSuccess(await users.ResetAuthenticatorKeyAsync(user));
            key = await users.GetAuthenticatorKeyAsync(user) ?? throw new InvalidOperationException("Authenticator key missing.");
            challenge.SecurityStamp = await users.GetSecurityStampAsync(user);
            await repository.Database.SaveChangesAsync(token);
        }
        await transaction.CommitAsync(token);
        return new(key, $"otpauth://totp/HostGrid:{Uri.EscapeDataString(user.Email!)}?secret={key}&issuer=HostGrid");
    }

    internal async Task<SessionResult?> VerifyMfaAsync(MfaVerifyRequest request, CancellationToken token)
    {
        var challenge = await FindChallengeAsync(request.Challenge, token);
        await using var transaction = await repository.LockAsync(challenge.UserId.ToString("N"), token);
        var user = await ValidateChallengeAsync(challenge, token);
        if (!await VerifyMfaProofAsync(user, request.Code, request.Recovery))
        {
            RequireSuccess(await users.AccessFailedAsync(user));
            await audit.RecordAsync("login_failed", token, user.Id);
            await transaction.CommitAsync(token);
            return null;
        }

        string[]? recoveryCodes = null;
        if (!user.TwoFactorEnabled)
        {
            RequireSuccess(await users.SetTwoFactorEnabledAsync(user, true));
            recoveryCodes = Enumerable.Range(0, 10)
                .Select(_ => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12))).ToArray();
            await ((IUserTwoFactorRecoveryCodeStore<ApplicationUser>)store)
                .ReplaceCodesAsync(user, recoveryCodes.Select(TokenService.Hash), token);
            RequireSuccess(await users.UpdateSecurityStampAsync(user));
            await repository.RevokeAsync(user.Id, Now, token);
            await audit.RecordAsync("mfa_enrolled", token, user.Id);
        }
        RequireSuccess(await users.ResetAccessFailedCountAsync(user));
        repository.Challenges.Remove(challenge);
        var view = await current.DescribeAsync(user, token);
        if (!CurrentUserService.HasAccess(view))
        {
            throw SecurityRequestException.InvalidProof();
        }
        var result = await tokens.IssueAsync(user, view, true, token, recoveryCodes: recoveryCodes);
        await audit.RecordAsync("login_succeeded", token, user.Id);
        await transaction.CommitAsync(token);
        return result;
    }

    internal async Task ResetMfaAsync(ClaimsPrincipal principal, MfaResetRequest request, CancellationToken token)
    {
        var id = CurrentUserService.UserId(principal);
        await using var transaction = await repository.LockAsync(id.ToString("N"), token);
        var user = await FindAsync(id);
        await repository.Database.Entry(user).ReloadAsync(token);
        if (!user.TwoFactorEnabled || !await VerifyPasswordAsync(user, request.Password) ||
            !await VerifyMfaProofAsync(user, request.Code, request.Recovery))
        {
            if (user.TwoFactorEnabled && !await users.IsLockedOutAsync(user) &&
                await users.CheckPasswordAsync(user, request.Password))
            {
                RequireSuccess(await users.AccessFailedAsync(user));
            }
            await audit.RecordAsync("mfa_reset_denied", token, id);
            await transaction.CommitAsync(token);
            throw SecurityRequestException.InvalidProof();
        }
        RequireSuccess(await users.SetTwoFactorEnabledAsync(user, false));
        RequireSuccess(await users.ResetAuthenticatorKeyAsync(user));
        await ((IUserTwoFactorRecoveryCodeStore<ApplicationUser>)store).ReplaceCodesAsync(user, [], token);
        RequireSuccess(await users.UpdateSecurityStampAsync(user));
        await repository.RevokeAsync(id, Now, token);
        await audit.RecordAsync("mfa_reset", token, id);
        await transaction.CommitAsync(token);
    }

    private async Task<bool> VerifyPasswordAsync(ApplicationUser user, string password)
    {
        if (!CurrentUserService.IsEnabled(user) || await users.IsLockedOutAsync(user))
        {
            return false;
        }
        if (await users.CheckPasswordAsync(user, password))
        {
            return true;
        }
        RequireSuccess(await users.AccessFailedAsync(user));
        return false;
    }

    private async Task<bool> VerifyMfaProofAsync(ApplicationUser user, string code, bool recovery)
    {
        var normalized = code.Replace(" ", "").Replace("-", "");
        if (recovery)
        {
            return user.TwoFactorEnabled && (await users.RedeemTwoFactorRecoveryCodeAsync(user,
                TokenService.Hash(normalized.ToUpperInvariant()))).Succeeded;
        }
        if (normalized.Length != 6 || !normalized.All(char.IsAsciiDigit) ||
            !await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, normalized))
        {
            return false;
        }

        // Callers hold the account lock, so verifying and consuming a code is atomic.
        var key = await users.GetAuthenticatorKeyAsync(user);
        var proof = TokenService.Hash($"{key}:{normalized}");
        var stored = await users.GetAuthenticationTokenAsync(user, ReplayProvider, ReplayToken);
        var recent = stored is null ? [] : JsonSerializer.Deserialize<List<UsedTotp>>(stored) ?? [];
        var cutoff = Now.AddMinutes(-3);
        recent.RemoveAll(entry => entry.UsedAt < cutoff);
        if (recent.Any(entry => entry.Hash == proof))
        {
            return false;
        }
        recent.Add(new(proof, Now));
        RequireSuccess(await users.SetAuthenticationTokenAsync(user, ReplayProvider, ReplayToken,
            JsonSerializer.Serialize(recent)));
        return true;
    }

    private async Task<string> CreateChallengeAsync(ApplicationUser user, CancellationToken token)
    {
        await repository.Challenges.Where(row => row.UserId == user.Id).ExecuteDeleteAsync(token);
        var raw = TokenService.NewSecret();
        repository.Challenges.Add(new LoginChallenge
        {
            UserId = user.Id, TokenHash = TokenService.Hash(raw), ExpiresAt = Now.AddMinutes(5),
            SecurityStamp = await users.GetSecurityStampAsync(user)
        });
        await repository.Database.SaveChangesAsync(token);
        return raw;
    }

    private async Task<LoginChallenge> FindChallengeAsync(string raw, CancellationToken token)
    {
        var hash = TokenService.Hash(raw);
        return await repository.Challenges.SingleOrDefaultAsync(row => row.TokenHash == hash, token)
            ?? throw SecurityRequestException.InvalidProof();
    }

    private async Task<ApplicationUser> ValidateChallengeAsync(LoginChallenge challenge, CancellationToken token)
    {
        await repository.Database.Entry(challenge).ReloadAsync(token);
        var user = await FindAsync(challenge.UserId);
        if (repository.Database.Entry(challenge).State == EntityState.Detached || challenge.ExpiresAt <= Now ||
            !CurrentUserService.IsEnabled(user) || await users.IsLockedOutAsync(user) ||
            challenge.SecurityStamp != await users.GetSecurityStampAsync(user))
        {
            throw SecurityRequestException.InvalidProof();
        }
        return user;
    }

    private async Task<ApplicationUser> FindAsync(Guid id)
        => await users.FindByIdAsync(id.ToString()) ?? throw SecurityRequestException.InvalidProof();

    internal static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new SecurityRequestException(StatusCodes.Status400BadRequest,
                "The account operation could not be completed.");
        }
    }

    private sealed record UsedTotp(string Hash, DateTime UsedAt);
}
