using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HostGrid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations/{organizationId:guid}/calendar")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CalendarController(CalendarManagement calendar) : ControllerBase
{
    [HttpGet]
    public Task<CalendarView> Get([FromQuery] CalendarFilter filter, CancellationToken token)
        => calendar.GetAsync(filter, token);

    [HttpGet("summary")]
    public Task<CalendarSummary> Summary([FromQuery] CalendarSummaryFilter filter, CancellationToken token)
        => calendar.SummaryAsync(filter, token);
}
