using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Security;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HostGrid.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountsController(AccountManagementService accounts,
    IAuthorizationService authorization) : ControllerBase
{
    private bool IsPlatformAdmin => User.IsInRole(CurrentUserService.SuperAdmin);

    [HttpGet("admin/organizations")]
    [Authorize(Policy = SecurityPolicies.GlobalAdmin)]
    public async Task<IActionResult> Organizations([FromQuery] int page = 1, CancellationToken token = default)
        => Ok(await accounts.ListOrganizationsAsync(page, token));

    [HttpPost("admin/organizations")]
    [Authorize(Policy = SecurityPolicies.GlobalAdmin)]
    public async Task<IActionResult> CreateOrganization(OrganizationRequest request, CancellationToken token)
        => StatusCode(201, new { id = await accounts.CreateOrganizationAsync(request, token) });

    [HttpGet("admin/security-events")]
    [Authorize(Policy = SecurityPolicies.GlobalAdmin)]
    public async Task<IActionResult> Audit([FromQuery] int page = 1, CancellationToken token = default)
        => Ok(await accounts.ListAuditAsync(page, token));

    [HttpGet("organizations/{organizationId:guid}/accounts")]
    [Authorize(Policy = SecurityPolicies.TenantManager)]
    public async Task<IActionResult> Memberships([FromQuery] int page = 1, CancellationToken token = default)
        => Ok(await accounts.ListMembershipsAsync(page, IsPlatformAdmin, token));

    [HttpPost("admin/accounts/{userId:guid}/disable")]
    [Authorize(Policy = SecurityPolicies.GlobalAdmin)]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Disable(Guid userId, CancellationToken token)
    {
        await accounts.DisableAsync(userId, CurrentUserService.UserId(User), token);
        return NoContent();
    }

    [HttpPost("organizations/{organizationId:guid}/accounts/invitations")]
    [Authorize(Policy = SecurityPolicies.TenantManager)]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Invite(InviteRequest request, CancellationToken token)
        => StatusCode(201, new { id = await accounts.InviteAsync(request, IsPlatformAdmin, token) });

    [HttpPut("organizations/{organizationId:guid}/accounts/{userId:guid}/membership")]
    [Authorize(Policy = SecurityPolicies.TenantManager)]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Membership(Guid userId, MembershipChangeRequest request, CancellationToken token)
    {
        await accounts.ChangeMembershipAsync(userId, request, IsPlatformAdmin, token);
        return NoContent();
    }

    [HttpPost("admin/organizations/{organizationId:guid}/managers/{userId:guid}")]
    [Authorize(Policy = SecurityPolicies.GlobalAdmin)]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> AssignManager(Guid userId, CancellationToken token)
    {
        await accounts.AssignManagerAsync(userId, token);
        return NoContent();
    }

    [HttpGet("organizations/{organizationId:guid}/properties/{propertyId:guid}/access")]
    public async Task<IActionResult> PropertyAccess(Guid propertyId, CancellationToken token)
    {
        var property = await accounts.FindPropertyAsync(propertyId, token);
        return property is null || !(await authorization.AuthorizeAsync(User, property, new PropertyAccess(false))).Succeeded
            ? NotFound() : NoContent();
    }
}
