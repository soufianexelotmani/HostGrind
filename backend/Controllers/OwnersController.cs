using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Security;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HostGrid.Api.Controllers;

[ApiController]
[Authorize(Policy = SecurityPolicies.TenantManager)]
[Route("api/v1/organizations/{organizationId:guid}/owners")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OwnersController(OwnerPortfolioService owners) : ControllerBase
{
    [HttpGet]
    public Task<Page<OwnerView>> List(string? search, bool? archived, string? sort, int page = 1, int size = 20,
        CancellationToken token = default) => owners.ListAsync(search, archived, sort, page, size, token);

    [HttpGet("{id:guid}")]
    public Task<OwnerPortfolio> Get(Guid id, CancellationToken token) => owners.GetAsync(id, token);

    [HttpPost]
    public async Task<IActionResult> Create(OwnerWrite input, CancellationToken token)
        => StatusCode(201, new { id = await owners.CreateAsync(input, token) });

    [HttpPut("{id:guid}")]
    public Task Update(Guid id, OwnerWrite input, CancellationToken token) => owners.UpdateAsync(id, input, token);

    [HttpPost("{id:guid}/invitation")]
    [EnableRateLimiting("authentication")]
    public Task Invite(Guid id, CancellationToken token) => owners.InviteAsync(id, token);

    [HttpPost("{id:guid}/link")]
    public Task Link(Guid id, OwnerLinkWrite input, CancellationToken token) => owners.LinkAsync(id, input, token);

    [HttpPost("{id:guid}/disable-access")]
    public Task Disable(Guid id, CancellationToken token) => owners.DisableAsync(id, token);

    [HttpPost("{id:guid}/archive")]
    public Task Archive(Guid id, VersionWrite input, CancellationToken token) => owners.ArchiveAsync(id, input.Version, token);
}
