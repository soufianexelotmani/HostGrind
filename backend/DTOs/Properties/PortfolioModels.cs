using HostGrid.Api.Enums;

namespace HostGrid.Api.DTOs.Properties;

public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int PageNumber, int PageSize);
public sealed record Option(Guid Id, string Code);
public sealed record OwnerWrite(string Name, string? Email, string? Phone, byte[]? Version);
public sealed record OwnerView(Guid Id, string Reference, string Name, string? Email, string? Phone, Guid? UserId,
    bool IsArchived, bool AccessEnabled, bool Activated, byte[] Version);
public sealed record OwnerPortfolio(OwnerView Owner, IReadOnlyList<PropertySummary> Properties, IReadOnlyList<ContractView> Contracts);
public sealed record PropertyWrite(Guid OwnerId, Guid TypeId, string Name, string? Description, string Address,
    string City, decimal? Latitude, decimal? Longitude, short MaxGuests, short Bedrooms, short Bathrooms,
    decimal BaseNightlyRate, TimeOnly CheckInTime, TimeOnly CheckOutTime, byte[]? Version);
public sealed record PropertySummary(Guid Id, string Reference, string Name, string City, Guid OwnerId,
    string OwnerName, Guid TypeId, string Type, PropertyStatus Status, OperationalStatus OperationalStatus,
    decimal BaseNightlyRate, Guid? CoverPhotoId);
public sealed record PropertyView(PropertySummary Summary, string? Description, string Address, decimal? Latitude,
    decimal? Longitude, short MaxGuests, short Bedrooms, short Bathrooms, TimeOnly CheckInTime,
    TimeOnly CheckOutTime, DateTime CreatedAt, DateTime UpdatedAt, byte[] Version, decimal DefaultCommission,
    IReadOnlyList<ContractView> Contracts, IReadOnlyList<CommissionView> CommissionRates, IReadOnlyList<InventoryView> Inventory,
    IReadOnlyList<Guid> Amenities, IReadOnlyList<SeasonView> SeasonalRates, IReadOnlyList<ListingView> Listings,
    IReadOnlyList<FileView> Photos, IReadOnlyList<FileView> Documents, IReadOnlyList<PortfolioAuditView> History);
public sealed record VersionWrite(byte[] Version);
public sealed record PropertyStatusWrite(PropertyStatus Status, byte[] Version);
public sealed record OwnerLinkWrite(Guid UserId, byte[] Version);
public sealed record ContractWrite(DateOnly StartDate, DateOnly? EndDate, decimal? Rate, Guid? DocumentId);
public sealed record ContractEndWrite(DateOnly EndDate, byte[] Version);
public sealed record ContractView(Guid Id, Guid PropertyId, DateOnly StartDate, DateOnly? EndDate, decimal Rate,
    string Status, string? DocumentName, Guid? DocumentId, byte[] Version);
public sealed record CommissionWrite(DateOnly EffectiveFrom, decimal Rate);
public sealed record CommissionView(Guid Id, DateOnly EffectiveFrom, DateOnly? EffectiveTo, decimal Rate);
public sealed record InventoryWrite(string Name, int Quantity, string? Condition, byte[]? Version);
public sealed record InventoryView(Guid Id, string Name, int Quantity, string? Condition, byte[] Version);
public sealed record AmenitiesWrite(Guid[] Ids, byte[] Version);
public sealed record SeasonWrite(DateOnly StartDate, DateOnly EndDate, short DaysOfWeek, decimal NightlyRate, short Priority, byte[]? Version);
public sealed record SeasonView(Guid Id, DateOnly StartDate, DateOnly EndDate, short DaysOfWeek, decimal NightlyRate, short Priority, byte[] Version);
public sealed record ListingWrite(string? Url, string? ExternalReference, byte[]? Version);
public sealed record ListingView(Guid Id, BookingSource Source, string? Url, string? ExternalReference, byte[] Version);
public sealed record FileView(Guid Id, string Name, string ContentType, long SizeBytes, DateTime UploadedAt,
    Guid UploaderId, FileScanStatus ScanStatus, int? SortOrder);
public sealed record PhotoOrderWrite(Guid[] Ids, byte[] Version);
public sealed record PortfolioAuditView(string Action, DateTime OccurredAt);
