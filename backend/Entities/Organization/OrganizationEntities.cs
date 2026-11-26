using HostGrid.Api.Enums;

namespace HostGrid.Api.Entities.Organization;

public interface ITenantEntity
{
    Guid OrganizationId { get; set; }
}

public sealed class Organization
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class OrganizationMember : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrganizationRole Role { get; set; }
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class OrganizationSettings : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? BrandName { get; set; }
    public Guid? LogoFileId { get; set; }
    public string Language { get; set; } = null!;
    public decimal DefaultCommissionRate { get; set; } = 20m;
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class Owner : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsArchived { get; set; }
    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
