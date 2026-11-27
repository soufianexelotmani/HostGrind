using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HostGrid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations/{organizationId:guid}/calendar-blocks")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CalendarBlocksController(CalendarBlockManagement blocks) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public Task<CalendarBlockEvent> Get(Guid id, CancellationToken token) => blocks.GetAsync(id, token);

    [HttpPost]
    public async Task<IActionResult> Create(CalendarBlockCreate input, CancellationToken token)
        => StatusCode(201, new { id = await blocks.CreateAsync(input, token) });

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CalendarBlockWrite input, CancellationToken token)
        => Ok(new { id = await blocks.UpdateAsync(id, input, token) });

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CalendarBlockCancel input, CancellationToken token)
    {
        await blocks.CancelAsync(id, input, token);
        return NoContent();
    }
}
