using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Enums;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HostGrid.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/organizations/{organizationId:guid}/properties")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PropertiesController(PropertyPortfolioService properties, PropertyConfigurationService configuration,
    PropertyFiles files, PortfolioAccess access, PropertyPricing pricing) : ControllerBase
{
    [HttpGet]
    public Task<Page<PropertySummary>> List(string? search, Guid? owner, Guid? type, PropertyStatus? status,
        string? sort, int page = 1, int size = 20, CancellationToken token = default)
        => properties.ListAsync(search, owner, type, status, sort, page, size, token);

    [HttpGet("{id:guid}")]
    public Task<PropertyView> Get(Guid id, CancellationToken token) => properties.GetAsync(id, token);

    [HttpGet("types")]
    public Task<List<Option>> Types(CancellationToken token) => properties.TypesAsync(token);

    [HttpGet("amenities")]
    public Task<List<Option>> AmenityCatalog(CancellationToken token) => properties.AmenitiesAsync(token);

    [HttpPost]
    public async Task<IActionResult> Create(PropertyWrite input, CancellationToken token)
        => StatusCode(201, new { id = await properties.CreateAsync(input, token) });

    [HttpPut("{id:guid}")]
    public Task Update(Guid id, PropertyWrite input, CancellationToken token) => properties.UpdateAsync(id, input, token);

    [HttpPut("{id:guid}/status")]
    public Task Status(Guid id, PropertyStatusWrite input, CancellationToken token) => properties.StatusAsync(id, input, token);

    [HttpPost("{id:guid}/contracts")]
    public Task Contract(Guid id, ContractWrite input, CancellationToken token) => configuration.ContractAsync(id, input, token);

    [HttpPut("{id:guid}/contracts/{contract:guid}/end")]
    public Task EndContract(Guid id, Guid contract, ContractEndWrite input, CancellationToken token)
        => configuration.CloseContractAsync(id, contract, input, token);

    [HttpPost("{id:guid}/commission-rates")]
    public Task Commission(Guid id, CommissionWrite input, CancellationToken token) => configuration.CommissionAsync(id, input, token);

    [HttpPut("{id:guid}/amenities")]
    public Task Amenities(Guid id, AmenitiesWrite input, CancellationToken token) => configuration.AmenitiesAsync(id, input, token);

    [HttpPost("{id:guid}/inventory")]
    public Task AddInventory(Guid id, InventoryWrite input, CancellationToken token) => configuration.InventoryAsync(id, null, input, token);

    [HttpPut("{id:guid}/inventory/{item:guid}")]
    public Task UpdateInventory(Guid id, Guid item, InventoryWrite input, CancellationToken token)
        => configuration.InventoryAsync(id, item, input, token);

    [HttpDelete("{id:guid}/inventory/{item:guid}")]
    public Task RemoveInventory(Guid id, Guid item, [FromBody] VersionWrite input, CancellationToken token)
        => configuration.RemoveInventoryAsync(id, item, input.Version, token);

    [HttpPost("{id:guid}/seasonal-rates")]
    public Task AddSeason(Guid id, SeasonWrite input, CancellationToken token) => configuration.SeasonAsync(id, null, input, token);

    [HttpPut("{id:guid}/seasonal-rates/{rate:guid}")]
    public Task UpdateSeason(Guid id, Guid rate, SeasonWrite input, CancellationToken token)
        => configuration.SeasonAsync(id, rate, input, token);

    [HttpGet("{id:guid}/nightly-rate")]
    public async Task<IActionResult> Price(Guid id, DateOnly night, CancellationToken token)
        => Ok(new { night, amount = await pricing.ResolveAsync(id, night, token), currency = "MAD" });

    [HttpPut("{id:guid}/listings/{source}")]
    public Task Listing(Guid id, BookingSource source, ListingWrite input, CancellationToken token)
        => configuration.ListingAsync(id, source, input, token);

    [HttpGet("{id:guid}/{group:regex(^(photos|documents)$)}")]
    public async Task<IReadOnlyList<FileView>> Files(Guid id, string group, CancellationToken token)
    {
        await access.PropertyAsync(id, false, token);
        return await files.ListAsync(id, group.Equals("photos", StringComparison.OrdinalIgnoreCase), token);
    }

    [HttpPost("{id:guid}/{group:regex(^(photos|documents)$)}")]
    [RequestSizeLimit(64000000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 64000000)]
    public async Task<IActionResult> Upload(Guid id, string group, IFormFile file, CancellationToken token)
        => StatusCode(201, await files.UploadAsync(id, file, group.Equals("photos", StringComparison.OrdinalIgnoreCase), token));

    [HttpGet("{id:guid}/{group:regex(^(photos|documents)$)}/{file:guid}")]
    public async Task<IActionResult> Download(Guid id, string group, Guid file, CancellationToken token)
    {
        var photo = group.Equals("photos", StringComparison.OrdinalIgnoreCase);
        var data = await files.DownloadAsync(id, file, photo, token);
        return File(data.Content, data.Mime, photo ? null : data.Name, enableRangeProcessing: true);
    }

    [HttpDelete("{id:guid}/{group:regex(^(photos|documents)$)}/{file:guid}")]
    public Task ArchiveFile(Guid id, string group, Guid file, CancellationToken token)
        => files.ArchiveAsync(id, file, group.Equals("photos", StringComparison.OrdinalIgnoreCase), token);

    [HttpPut("{id:guid}/photos/order")]
    public Task Order(Guid id, PhotoOrderWrite input, CancellationToken token) => files.OrderAsync(id, input, token);
}
