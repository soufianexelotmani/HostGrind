using HostGrid.Api.Enums;
using HostGrid.Api.Entities.Organization;

namespace HostGrid.Api.Entities.Document;

public sealed class StoredFile : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string StorageKey { get; set; } = null!;
    public string OriginalName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string ContentHash { get; set; } = null!;
    public FileScanStatus ScanStatus { get; set; }
    public Guid UploaderId { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public sealed class PropertyDocument : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid FileId { get; set; }
    public PropertyDocumentType Type { get; set; }
}
