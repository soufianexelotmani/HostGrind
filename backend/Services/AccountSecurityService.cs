using System.Security.Claims;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Integrations.Email;
using HostGrid.Api.Options;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.AspNetCore.Identity;

namespace HostGrid.Api.Services;

public sealed class AccountSecurityService(UserManager<ApplicationUser> users, AuthenticationRepository repository,
    ISecurityEmail email, SecurityOptions options, SecurityAuditService audit, TimeProvider clock,
    ILogger<AccountSecurityService> logger)
{
    private const string Invitation = "HostGrid.Activation";
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    internal async Task SendInvitationAsync(ApplicationUser user, CancellationToken token)
    {
        var proof = await users.GenerateUserTokenAsync(user, users.Options.Tokens.PasswordResetTokenProvider, Invitation);
        await SendLinkAsync(user, proof, "activate", "Activate your HostGrid account", token);
    }

    internal async Task RequestResetAsync(string address, CancellationToken token)
    {
        var user = await users.FindByEmailAsync(address);
        if (CurrentUserService.IsEnabled(user))
        {
            try
            {
                var proof = await users.GeneratePasswordResetTokenAsync(user!);
                await SendLinkAsync(user!, proof, "reset-password", "Reset your HostGrid password", token);
                await audit.RecordAsync("password_reset_requested", token, user!.Id);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning("A password reset request could not be delivered.");
            }
        }
    }

    internal async Task CompleteLinkAsync(PasswordLinkRequest request, bool activate, CancellationToken token)
    {
        var user = await users.FindByEmailAsync(request.Email) ?? throw InvalidLink();
        await using var transaction = await repository.LockAsync(user.Id.ToString("N"), token);
        await repository.Database.Entry(user).ReloadAsync(token);
        if (activate)
        {
            if (user.IsActive || user.EmailConfirmed || user.ActivatedAt != null || await users.HasPasswordAsync(user) ||
                !await users.VerifyUserTokenAsync(user, users.Options.Tokens.PasswordResetTokenProvider, Invitation, request.Token))
            {
                throw InvalidLink();
            }
            user.EmailConfirmed = true;
            user.IsActive = true;
            user.ActivatedAt = Now;
            AuthenticationService.RequireSuccess(await users.AddPasswordAsync(user, request.Password));
        }
        else
        {
            if (!CurrentUserService.IsEnabled(user) || !await users.VerifyUserTokenAsync(user,
                users.Options.Tokens.PasswordResetTokenProvider, UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
                request.Token))
            {
                throw InvalidLink();
            }
            AuthenticationService.RequireSuccess(await users.ResetPasswordAsync(user, request.Token, request.Password));
        }
        AuthenticationService.RequireSuccess(await users.SetLockoutEndDateAsync(user, null));
        AuthenticationService.RequireSuccess(await users.ResetAccessFailedCountAsync(user));
        await repository.RevokeAsync(user.Id, Now, token);
        await audit.RecordAsync(activate ? "account_activated" : "password_reset", token, user.Id);
        await transaction.CommitAsync(token);
    }

    internal async Task ChangePasswordAsync(ClaimsPrincipal principal, PasswordChangeRequest request, CancellationToken token)
    {
        var id = CurrentUserService.UserId(principal);
        await using var transaction = await repository.LockAsync(id.ToString("N"), token);
        var user = await users.FindByIdAsync(id.ToString());
        if (user is not null)
        {
            await repository.Database.Entry(user).ReloadAsync(token);
        }
        if (!CurrentUserService.IsEnabled(user) || await users.IsLockedOutAsync(user!))
        {
            throw SecurityRequestException.InvalidProof();
        }
        var result = await users.ChangePasswordAsync(user!, request.CurrentPassword, request.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
            {
                AuthenticationService.RequireSuccess(await users.AccessFailedAsync(user!));
            }
            await audit.RecordAsync("password_change_denied", token, id);
            await transaction.CommitAsync(token);
            AuthenticationService.RequireSuccess(result);
        }
        await repository.RevokeAsync(id, Now, token);
        await audit.RecordAsync("password_changed", token, id);
        await transaction.CommitAsync(token);
    }

    private async Task SendLinkAsync(ApplicationUser user, string proof, string path, string subject, CancellationToken token)
    {
        var link = $"{options.PublicOrigin}/{path}#email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(proof)}";
        try
        {
            await email.SendAsync(user.Email!, subject, $"Open this link within 30 minutes.\n\n{link}", token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new SecurityRequestException(StatusCodes.Status503ServiceUnavailable, "Email delivery is unavailable.");
        }
    }

    private static SecurityRequestException InvalidLink()
        => new(StatusCodes.Status400BadRequest, "This account link is invalid or expired.");
}
