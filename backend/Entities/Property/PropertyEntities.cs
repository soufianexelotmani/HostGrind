using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Property;

public sealed class PropertyType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
}

public sealed class Amenity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
}

public sealed class Property : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid TypeId { get; set; }
    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public short MaxGuests { get; set; }
    public short Bedrooms { get; set; }
    public short Bathrooms { get; set; }
    public decimal BaseNightlyRate { get; set; }
    public TimeOnly CheckInTime { get; set; }
    public TimeOnly CheckOutTime { get; set; }
    public PropertyStatus Status { get; set; }
    public OperationalStatus OperationalStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class PropertyAmenity : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid AmenityId { get; set; }
}

public sealed class PropertyPhoto : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid FileId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PropertyInventoryItem : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = null!;
    public int Quantity { get; set; }
    public string? Condition { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class ManagementContract : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid OwnerId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Rate { get; set; }
    public Guid? FileId { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class CommissionRateHistory : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal Rate { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class SeasonalRate : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public short DaysOfWeek { get; set; }
    public decimal NightlyRate { get; set; }
    public short Priority { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class PropertyListing : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public BookingSource Source { get; set; }
    public string? Url { get; set; }
    public string? ExternalReference { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
