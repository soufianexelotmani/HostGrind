using System.Text.Json;
using FluentValidation;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Services;
using HostGrid.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HostGrid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations/{organizationId:guid}/reservations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReservationsController(ReservationManagement reservations) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public Task<Page<ReservationSummary>> List([FromQuery] ReservationFilter filter, CancellationToken token)
        => reservations.ListAsync(filter, token);

    [HttpGet("{id:guid}")]
    public Task<ReservationView> Get(Guid id, CancellationToken token) => reservations.GetAsync(id, token);

    [HttpPost]
    public async Task<IActionResult> Create(ReservationWrite input, CancellationToken token)
        => StatusCode(201, new { id = await reservations.CreateAsync(input, token) });

    [HttpPut("{id:guid}")]
    public Task Update(Guid id, ReservationWrite input, CancellationToken token) => reservations.UpdateAsync(id, input, token);

    [HttpPost("pricing-preview")]
    public Task<IReadOnlyList<AcceptedNight>> Preview(ReservationPreview input, CancellationToken token)
        => reservations.PreviewAsync(input, token);

    [HttpGet("{id:guid}/history")]
    public Task<Page<PriceRevisionView>> History(Guid id, CancellationToken token, int page = 1, int size = 10)
        => reservations.HistoryAsync(id, page, size, token);

    [HttpPost("historical")]
    [ServiceFilter(typeof(PropertyUploadLimit))]
    public async Task<IActionResult> Historical([FromForm] string request, IFormFile? file,
        [FromServices] IValidator<ReservationWrite> validator, CancellationToken token)
    {
        HistoricalReservation? input;
        try { input = JsonSerializer.Deserialize<HistoricalReservation>(request, Json); }
        catch (JsonException) { return Problem("Invalid historical reservation request.", statusCode: 400); }
        if (input?.Booking is null) return Problem("The historical reservation data is required.", statusCode: 400);
        var validation = await validator.ValidateAsync(input.Booking, token);
        if (!validation.IsValid) return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        return StatusCode(201, new { id = await reservations.HistoricalAsync(input, file, token) });
    }

    [HttpGet("{id:guid}/evidence")]
    public async Task<IActionResult> Evidence(Guid id, CancellationToken token)
    {
        var file = await reservations.EvidenceAsync(id, token);
        return File(file.Content, "application/pdf", file.Name, enableRangeProcessing: true);
    }
}
