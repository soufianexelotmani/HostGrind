using System.Security.Cryptography;
using HostGrid.Api.Entities.Document;
using HostGrid.Api.Enums;
using HostGrid.Api.Integrations.Storage;
using HostGrid.Api.Security;
using Microsoft.Extensions.Options;

namespace HostGrid.Api.Services;

public sealed class ReservationEvidence(IFileStorageService storage, IFileScanner scanner, IOptions<PropertyStorageOptions> options,
    IHostEnvironment environment, PortfolioAccess access, TimeProvider clock, ILogger<ReservationEvidence> logger)
{
    private readonly bool allowQuarantine = options.Value.CanReadQuarantined(environment);

    internal async Task<StoredFile> StageAsync(IFormFile? upload, CancellationToken token)
    {
        access.EnsureBookingManager();
        if (upload is null) throw new SecurityRequestException(400, "A private PDF is required.", "historical_evidence_required");
        if (upload.Length <= 0 || upload.Length > options.Value.DocumentBytes)
            throw new SecurityRequestException(413, "The evidence exceeds the upload limits or is empty.");
        var name = Path.GetFileName(upload.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Any(char.IsControl))
            throw new SecurityRequestException(400, "The filename is invalid.");
        await using var input = upload.OpenReadStream();
        var signature = new byte[Math.Min(upload.Length, 16)];
        await input.ReadExactlyAsync(signature, token);
        var type = PropertyFiles.Identify(name, signature, false);
        if (type is null) throw new SecurityRequestException(415, "Supporting evidence must be a PDF.");
        input.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
        input.Position = 0;
        var status = await scanner.ScanAsync(input, token);
        if (status != FileScanStatus.Clean && !(allowQuarantine && status == FileScanStatus.Quarantined))
            throw new SecurityRequestException(422, "The evidence has not passed the document scan policy.", "historical_evidence_unreadable");
        input.Position = 0;
        var id = Guid.NewGuid();
        var key = id.ToString("N");
        try { await storage.StoreAsync(key, input, token); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Reservation evidence storage failed ({FailureType}).", error.GetType().Name);
            throw new SecurityRequestException(503, "Private file storage is unavailable.");
        }
        return new StoredFile { Id = id, OrganizationId = access.OrganizationId, UploaderId = access.Actor,
            StorageKey = key, OriginalName = name, ContentType = type, ContentHash = hash, SizeBytes = upload.Length,
            ScanStatus = status, UploadedAt = clock.GetUtcNow().UtcDateTime };
    }

    internal async Task DeleteAsync(StoredFile file)
    {
        try { await storage.DeleteAsync(file.StorageKey, CancellationToken.None); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogError("Could not remove reservation evidence after rollback ({FailureType}).", error.GetType().Name);
        }
    }

    internal async Task<(Stream Content, string Name)> DownloadAsync(StoredFile? file, CancellationToken token)
    {
        if (file is null || file.ArchivedAt != null || file.ScanStatus != FileScanStatus.Clean &&
            !(allowQuarantine && file.ScanStatus == FileScanStatus.Quarantined))
            throw Missing();
        try { return (await storage.OpenAsync(file.StorageKey, token), file.OriginalName); }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { throw Missing(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Reservation evidence read failed ({FailureType}).", error.GetType().Name);
            throw new SecurityRequestException(503, "Private file storage is unavailable.");
        }
    }

    private static SecurityRequestException Missing() => new(404, "Supporting evidence not found.");
}
