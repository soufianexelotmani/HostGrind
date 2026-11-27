using HostGrid.Api.Enums;
using Microsoft.Extensions.Options;

namespace HostGrid.Api.Integrations.Storage;

public sealed class PropertyStorageOptions
{
    internal const long MaxUploadBytes = 60_000_000;
    public string? Root { get; init; }
    public long PhotoBytes { get; init; } = 10 * 1024 * 1024;
    public long DocumentBytes { get; init; } = 20 * 1024 * 1024;
    public bool AllowUnscannedInDevelopment { get; init; } = true;

    internal bool CanReadQuarantined(IHostEnvironment environment)
        => environment.IsDevelopment() && AllowUnscannedInDevelopment;
}

public interface IFileStorageService
{
    Task StoreAsync(string key, Stream source, CancellationToken token);
    Task<Stream> OpenAsync(string key, CancellationToken token);
    Task DeleteAsync(string key, CancellationToken token);
}

public interface IFileScanner
{
    Task<FileScanStatus> ScanAsync(Stream source, CancellationToken token);
}

internal sealed class QuarantineFileScanner : IFileScanner
{
    public Task<FileScanStatus> ScanAsync(Stream source, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(FileScanStatus.Quarantined);
    }
}

internal sealed class LocalFileStorage(IOptions<PropertyStorageOptions> options, IWebHostEnvironment environment)
    : IFileStorageService
{
    public async Task StoreAsync(string key, Stream source, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var staging = path + "." + Guid.NewGuid().ToString("N") + ".staging";
        try
        {
            await using (var destination = new FileStream(staging, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 65536, FileOptions.Asynchronous))
                await source.CopyToAsync(destination, token);
            token.ThrowIfCancellationRequested();
            File.Move(staging, path);
        }
        finally { File.Delete(staging); }
    }

    public Task<Stream> OpenAsync(string key, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var path = Resolve(key);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Private storage does not permit linked blobs.");
        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan));
    }

    public Task DeleteAsync(string key, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        File.Delete(Resolve(key));
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        if (!Guid.TryParseExact(key, "N", out var id) || key != id.ToString("N"))
            throw new ArgumentException("Invalid private file identifier.", nameof(key));
        return Path.Combine(StorageRoot(), key);
    }

    internal string StorageRoot()
    {
        var configured = options.Value.Root;
        if (string.IsNullOrWhiteSpace(configured) && !environment.IsDevelopment())
            throw new InvalidOperationException("Private file storage requires a configured root.");
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "private-files") : configured);
        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        var relative = Path.GetRelativePath(webRoot, root);
        if (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar))
            throw new InvalidOperationException("Storage must be outside the web root.");
        for (var directory = new DirectoryInfo(root); directory is not null; directory = directory.Parent)
        {
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Private storage requires a path without linked directories.");
        }
        return root;
    }
}
