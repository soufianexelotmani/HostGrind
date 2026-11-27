using HostGrid.Api.Enums;

namespace HostGrid.Api.DTOs.Properties;

public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int PageNumber, int PageSize);
public sealed record Option(Guid Id, string Code);
public sealed record OwnerWrite(string Name, string? Email, string? Phone, byte[]? Version);
public sealed record OwnerView
{
    public Guid Id { get; init; }
    public required string Reference { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? UserId { get; init; }
    public bool IsArchived { get; init; }
    public bool AccessEnabled { get; init; }
    public bool Activated { get; init; }
    public required byte[] Version { get; init; }
}
public sealed record OwnerPortfolio(OwnerView Owner, IReadOnlyList<PropertySummary> Properties, IReadOnlyList<ContractView> Contracts);
public sealed record PropertyWrite(Guid OwnerId, Guid TypeId, string Name, string? Description, string Address,
    string City, decimal? Latitude, decimal? Longitude, short MaxGuests, short Bedrooms, short Bathrooms,
    decimal BaseNightlyRate, TimeOnly CheckInTime, TimeOnly CheckOutTime, byte[]? Version);
public sealed record PropertySummary
{
    public Guid Id { get; init; }
    public required string Reference { get; init; }
    public required string Name { get; init; }
    public required string City { get; init; }
    public Guid OwnerId { get; init; }
    public required string OwnerName { get; init; }
    public Guid TypeId { get; init; }
    public required string Type { get; init; }
    public PropertyStatus Status { get; init; }
    public OperationalStatus OperationalStatus { get; init; }
    public decimal BaseNightlyRate { get; init; }
    public Guid? CoverPhotoId { get; init; }
}
public sealed record PropertyView(PropertySummary Summary, string? Description, string Address, decimal? Latitude,
    decimal? Longitude, short MaxGuests, short Bedrooms, short Bathrooms, TimeOnly CheckInTime,
    TimeOnly CheckOutTime, DateTime CreatedAt, DateTime UpdatedAt, byte[] Version, decimal DefaultCommission,
    IReadOnlyList<ContractView> Contracts, IReadOnlyList<CommissionView> CommissionRates, IReadOnlyList<InventoryView> Inventory,
    IReadOnlyList<Guid> Amenities, IReadOnlyList<SeasonView> SeasonalRates, IReadOnlyList<ListingView> Listings,
    IReadOnlyList<FileView> Photos, IReadOnlyList<FileView> Documents, IReadOnlyList<PortfolioAuditView> History,
    DateOnly BusinessDate, decimal CurrentCommission, ContractView? ActiveContract);
public sealed record VersionWrite(byte[] Version);
public sealed record PropertyStatusWrite(PropertyStatus Status, byte[] Version);
public sealed record OwnerLinkWrite(Guid UserId, byte[] Version);
public sealed record ContractWrite(DateOnly StartDate, DateOnly? EndDate, decimal? Rate, Guid? DocumentId, byte[]? Version = null);
public sealed record ContractEndWrite(DateOnly EndDate, byte[] Version);
public sealed record ContractView(Guid Id, Guid PropertyId, DateOnly StartDate, DateOnly? EndDate, decimal Rate,
    string Status, string? DocumentName, Guid? DocumentId, byte[] Version, DateTime? ArchivedAt);
public sealed record CommissionWrite(DateOnly EffectiveFrom, decimal Rate);
public sealed record CommissionView(Guid Id, DateOnly EffectiveFrom, DateOnly? EffectiveTo, decimal Rate);
public sealed record EffectiveCommissionView(DateOnly Date, decimal Rate);
public sealed record InventoryWrite(string Name, int Quantity, string? Condition, byte[]? Version, string? Notes = null);
public sealed record InventoryView(Guid Id, string Name, int Quantity, string? Condition, byte[] Version, string? Notes);
public sealed record BaseRateWrite(decimal? NightlyRate, byte[] Version);
public sealed record NightlyQuote(DateOnly Night, decimal Amount, string Currency, string Source, Guid? RuleId);
public sealed record AmenitiesWrite(Guid[] Ids, byte[] Version);
public sealed record SeasonWrite(DateOnly StartDate, DateOnly EndDate, short DaysOfWeek, decimal NightlyRate, short Priority, byte[]? Version);
public sealed record SeasonView(Guid Id, DateOnly StartDate, DateOnly EndDate, short DaysOfWeek, decimal NightlyRate, short Priority, byte[] Version);
public sealed record ListingWrite(string? Url, string? ExternalReference, byte[]? Version);
public sealed record ListingView(Guid Id, BookingSource Source, string? Url, string? ExternalReference, byte[] Version);
public sealed record FileView(Guid Id, string Name, string ContentType, long SizeBytes, DateTime UploadedAt,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    Guid? UploaderId, FileScanStatus ScanStatus, int? SortOrder, bool Readable);
public sealed record PhotoOrderWrite(Guid[] Ids, byte[] Version);
public sealed record PortfolioAuditView(string Action, DateTime OccurredAt);
