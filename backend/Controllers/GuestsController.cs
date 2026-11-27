using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HostGrid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations/{organizationId:guid}/guests")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GuestsController(GuestManagement guests) : ControllerBase
{
    [HttpGet]
    public Task<Page<GuestView>> List(string? search, bool? hasContact, string? sort,
        CancellationToken token, int page = 1, int size = 20)
        => guests.ListAsync(search, hasContact, sort, page, size, token);

    [HttpGet("{id:guid}")]
    public Task<GuestView> Get(Guid id, CancellationToken token) => guests.GetAsync(id, token);

    [HttpPost]
    public async Task<IActionResult> Create(GuestWrite input, CancellationToken token)
        => StatusCode(201, new { id = await guests.SaveAsync(null, input, token) });

    [HttpPut("{id:guid}")]
    public async Task Update(Guid id, GuestWrite input, CancellationToken token) => await guests.SaveAsync(id, input, token);

    [HttpPost("suggestions")]
    public Task<List<GuestView>> Suggestions(GuestLookup input, CancellationToken token)
        => guests.SuggestionsAsync(input, token);

    [HttpGet("{id:guid}/reservations")]
    public Task<Page<GuestReservation>> History(Guid id, CancellationToken token, int page = 1, int size = 20)
        => guests.HistoryAsync(id, page, size, token);
}
