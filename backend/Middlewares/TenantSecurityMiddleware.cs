using HostGrid.Api.Data;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Options;
using HostGrid.Api.Security;
using HostGrid.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Middlewares;

internal sealed class TenantSecurityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, AuthenticatedTenantContext tenant,
        HostGridDbContext database, SecurityOptions options, SecurityAuditService audit)
    {
        var token = http.RequestAborted;
        var isApi = http.Request.Path.StartsWithSegments("/api");
        if (isApi && !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) &&
            !ValidBrowserRequest(http.Request, options))
        {
            http.Response.StatusCode = StatusCodes.Status403Forbidden;
            await audit.RecordAsync("request_origin_denied", token);
            return;
        }

        if (http.Items[typeof(CurrentUserView)] is CurrentUserView account &&
            http.Request.RouteValues.TryGetValue("organizationId", out var routeOrganization) &&
            Guid.TryParse(routeOrganization?.ToString(), out var organizationId))
        {
            var membership = account.Memberships.SingleOrDefault(row => row.OrganizationId == organizationId);
            if (membership is not null)
            {
                tenant.Authorize(organizationId, membership);
            }
            else if (account.SuperAdmin && await database.Set<Organization>().IgnoreQueryFilters()
                .AnyAsync(row => row.Id == organizationId && row.IsActive, token))
            {
                tenant.Authorize(organizationId, null);
                await audit.RecordAsync("admin_tenant_selected", token);
            }
            if (tenant.AuthorizedOrganizationId is null)
            {
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                await audit.RecordAsync("tenant_access_denied", token);
                return;
            }
        }

        await next(http);
        if (isApi && (http.Response.StatusCode is 401 or 403 ||
            http.Response.StatusCode == 404 && http.Request.RouteValues.ContainsKey("propertyId")))
        {
            await audit.RecordAsync("access_denied", token);
        }
    }

    private static bool ValidBrowserRequest(HttpRequest request, SecurityOptions options)
        => request.Headers["X-HostGrid-CSRF"] == "1" && request.Headers["Sec-Fetch-Site"] != "cross-site" &&
            (!request.Headers.TryGetValue("Origin", out var origin) || origin == options.PublicOrigin);
}
