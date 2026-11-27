using System.Security.Cryptography;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Integrations.Storage;
using HostGrid.Api.Mappers;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HostGrid.Api.Services;

public sealed class PropertyFiles(PortfolioRepository repository, PortfolioAccess access, PortfolioAudit audit,
    AuthenticationRepository locks, IFileStorageService storage, IFileScanner scanner,
    IOptions<PropertyStorageOptions> options, IHostEnvironment environment, TimeProvider clock, ILogger<PropertyFiles> logger)
{
    private HostGrid.Api.Data.HostGridDbContext Database => repository.Database;

    internal async Task<IReadOnlyList<FileView>> ListAsync(Guid propertyId, bool photo, CancellationToken token)
    {
        var links = photo
            ? await (from link in Database.Set<PropertyPhoto>()
                     join file in Database.Set<StoredFile>() on link.FileId equals file.Id
                     where link.PropertyId == propertyId && file.ArchivedAt == null
                     orderby link.SortOrder, link.Id
                     select new { link.Id, File = file, Order = (int?)link.SortOrder }).AsNoTracking().ToListAsync(token)
            : await (from link in Database.Set<PropertyDocument>()
                     join file in Database.Set<StoredFile>() on link.FileId equals file.Id
                     where link.PropertyId == propertyId && file.ArchivedAt == null
                     orderby file.UploadedAt descending
                     select new { link.Id, File = file, Order = (int?)null }).AsNoTracking().ToListAsync(token);
        var canManage = access.CanManage;
        return links.Select(row => PortfolioMapper.File(row.Id, row.File, row.Order, CanRead(row.File)))
            .Where(row => canManage || row.Readable)
            .Select(row => canManage ? row : row with { UploaderId = null }).ToArray();
    }

    internal async Task<FileView> UploadAsync(Guid propertyId, IFormFile upload, bool photo, CancellationToken token)
    {
        var property = await access.PropertyAsync(propertyId, true, token);
        var max = photo ? options.Value.PhotoBytes : options.Value.DocumentBytes;
        if (upload.Length <= 0 || upload.Length > max)
            throw new SecurityRequestException(413, "The file exceeds the upload limits or is empty.");
        var displayName = Path.GetFileName(upload.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 255 || displayName.Any(char.IsControl))
            throw new SecurityRequestException(400, "The filename is invalid.");
        await using var input = upload.OpenReadStream();
        var magic = new byte[Math.Min(upload.Length, 16)];
        await input.ReadExactlyAsync(magic, token);
        var mime = Identify(displayName, magic, photo);
        if (mime is null) throw new SecurityRequestException(415, "Unsupported file type or signature.");
        input.Position = 0;
        if (photo && !await PhotoContent.IsValidAsync(input, mime, token))
            throw new SecurityRequestException(415, "The image could not be fully decoded or exceeds image limits.");
        input.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
        input.Position = 0;
        var scan = await scanner.ScanAsync(input, token);
        if (scan == FileScanStatus.Rejected) throw new SecurityRequestException(422, "The file was rejected.");
        input.Position = 0;
        var key = Guid.NewGuid().ToString("N");
        await using var transaction = await locks.LockAsync("property:" + propertyId, token);
        await RequireEditableAsync(property, token);
        var now = clock.GetUtcNow().UtcDateTime;
        var file = new StoredFile { Id = Guid.NewGuid(), OrganizationId = access.OrganizationId,
            ContentType = mime, ContentHash = hash, OriginalName = displayName, SizeBytes = upload.Length,
            ScanStatus = scan, StorageKey = key, UploadedAt = now, UploaderId = access.Actor };
        var id = Guid.NewGuid();
        int? order = null;
        try { await storage.StoreAsync(key, input, token); }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Private storage write failed ({FailureType}).", failure.GetType().Name);
            throw new SecurityRequestException(503, "Private file storage is unavailable.");
        }
        try
        {
            Database.Add(file);
            if (photo)
            {
                order = (await Database.Set<PropertyPhoto>().Where(row => row.PropertyId == propertyId)
                    .MaxAsync(row => (int?)row.SortOrder, token) ?? -1) + 1;
                Database.Add(new PropertyPhoto { Id = id, OrganizationId = access.OrganizationId,
                    PropertyId = propertyId, FileId = file.Id, SortOrder = order.Value });
            }
            else
            {
                Database.Add(new PropertyDocument { Id = id, OrganizationId = access.OrganizationId,
                    PropertyId = propertyId, FileId = file.Id, Type = PropertyDocumentType.Other });
            }
            Touch(property, now);
            await audit.SaveAsync(photo ? "photo_uploaded" : "document_uploaded", "Property", propertyId, token);
        }
        catch
        {
            try { await storage.DeleteAsync(key, CancellationToken.None); }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
            {
                logger.LogError("Private file cleanup failed after an unsuccessful upload ({FailureType}).", failure.GetType().Name);
            }
            throw;
        }
        // A failed commit can be ambiguous. Retain the private blob for reconciliation in that case.
        await transaction.CommitAsync(token);
        return PortfolioMapper.File(id, file, order, CanRead(file));
    }

    internal async Task<(Stream Content, string Mime, string Name)> DownloadAsync(Guid propertyId, Guid id, bool photo, CancellationToken token)
    {
        await access.PropertyAsync(propertyId, false, token);
        var file = await LookupAsync(propertyId, id, photo, token);
        if (!CanRead(file)) throw new SecurityRequestException(404, "File not found.");
        try { return (await storage.OpenAsync(file.StorageKey, token), file.ContentType, file.OriginalName); }
        catch (Exception failure) when (failure is FileNotFoundException or DirectoryNotFoundException)
        {
            logger.LogWarning("Private file metadata references missing storage content.");
            throw new SecurityRequestException(404, "File not found.");
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Private storage read failed ({FailureType}).", failure.GetType().Name);
            throw new SecurityRequestException(503, "Private file storage is unavailable.");
        }
    }

    internal async Task ArchiveAsync(Guid propertyId, Guid id, bool photo, CancellationToken token)
    {
        var property = await access.PropertyAsync(propertyId, true, token);
        await using var transaction = await locks.LockAsync("property:" + propertyId, token);
        await RequireEditableAsync(property, token);
        var file = await LookupAsync(propertyId, id, photo, token);
        if (await Database.Set<ManagementContract>().AnyAsync(row => row.FileId == file.Id, token))
            throw new SecurityRequestException(409, "A contract retains this document.");
        file.ArchivedAt = clock.GetUtcNow().UtcDateTime;
        Touch(property, file.ArchivedAt.Value);
        await audit.SaveAsync(photo ? "photo_archived" : "document_archived", "Property", propertyId, token);
        await transaction.CommitAsync(token);
    }

    internal async Task OrderAsync(Guid propertyId, PhotoOrderWrite input, CancellationToken token)
    {
        var property = await access.PropertyAsync(propertyId, true, token);
        await using var transaction = await locks.LockAsync("property:" + propertyId, token);
        await RequireEditableAsync(property, token);
        PortfolioVersion.Match(property.RowVersion, input.Version);
        var photos = await (from link in Database.Set<PropertyPhoto>()
            join file in Database.Set<StoredFile>() on link.FileId equals file.Id
            where link.PropertyId == propertyId && file.ArchivedAt == null select link).ToDictionaryAsync(row => row.Id, token);
        if (input.Ids.Length != photos.Count || !input.Ids.ToHashSet().SetEquals(photos.Keys))
            throw new SecurityRequestException(400, "Supply all active photo IDs exactly once.");
        for (var i = 0; i < input.Ids.Length; i++) photos[input.Ids[i]].SortOrder = i;
        Touch(property, clock.GetUtcNow().UtcDateTime);
        await audit.SaveAsync("photos_reordered", "Property", propertyId, token);
        await transaction.CommitAsync(token);
    }

    internal async Task<PropertyDocument> RequireDocumentAttachmentAsync(Guid propertyId, Guid id, CancellationToken token)
    {
        var document = await (from link in Database.Set<PropertyDocument>()
                              join file in Database.Set<StoredFile>() on link.FileId equals file.Id
                              where link.PropertyId == propertyId && link.Id == id && file.ArchivedAt == null &&
                                  (file.ScanStatus == FileScanStatus.Clean ||
                                   readQuarantined && file.ScanStatus == FileScanStatus.Quarantined)
                              select link).SingleOrDefaultAsync(token);
        return document ?? throw new SecurityRequestException(400, "Select an available document from this property.");
    }

    private async Task<StoredFile> LookupAsync(Guid propertyId, Guid id, bool photo, CancellationToken token)
    {
        var query = photo ? Database.Set<PropertyPhoto>().Where(row => row.PropertyId == propertyId && row.Id == id).Select(row => row.FileId)
            : Database.Set<PropertyDocument>().Where(row => row.PropertyId == propertyId && row.Id == id).Select(row => row.FileId);
        return await Database.Set<StoredFile>().SingleOrDefaultAsync(row => query.Contains(row.Id) && row.ArchivedAt == null, token)
            ?? throw new SecurityRequestException(404, "File not found.");
    }

    private async Task RequireEditableAsync(Property property, CancellationToken token)
    {
        await Database.Entry(property).ReloadAsync(token);
        if (property.Status == PropertyStatus.Archived)
            throw new SecurityRequestException(409, "This property is archived.");
    }

    private void Touch(Property property, DateTime now)
    {
        property.UpdatedAt = now;
        Database.Entry(property).Property(row => row.UpdatedAt).IsModified = true;
    }

    private readonly bool readQuarantined = options.Value.CanReadQuarantined(environment);

    private bool CanRead(StoredFile file) => file.ScanStatus == FileScanStatus.Clean ||
        readQuarantined && file.ScanStatus == FileScanStatus.Quarantined;

    internal static string? Identify(string name, ReadOnlySpan<byte> magic, bool photo)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        if (!photo)
            return ext == ".pdf" && magic.StartsWith("%PDF-"u8) ? "application/pdf" : null;
        if (ext is ".jpg" or ".jpeg" && magic.StartsWith(new byte[] { 255, 216, 255 })) return "image/jpeg";
        if (ext == ".png" && magic.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (ext == ".webp" && magic.Length >= 16 && magic.StartsWith("RIFF"u8) && magic[8..12].SequenceEqual("WEBP"u8)
            && (magic[12..16].SequenceEqual("VP8 "u8) || magic[12..16].SequenceEqual("VP8L"u8) ||
                magic[12..16].SequenceEqual("VP8X"u8))) return "image/webp";
        return null;
    }
}
