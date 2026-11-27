using HostGrid.Api.Data;
using HostGrid.Api.Entities.Document;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Integrations.Storage;

internal sealed class PrivateFileInventory(HostGridDbContext database, LocalFileStorage storage, TimeProvider clock)
{
    internal async Task WriteAsync(TextWriter output, CancellationToken token)
    {
        var root = storage.StorageRoot();
        var cutoff = clock.GetUtcNow().UtcDateTime.AddHours(-24);
        // This trusted terminal command compares the entire catalog with a private volume.
        var references = await database.Set<StoredFile>().IgnoreQueryFilters()
            .Select(file => new { file.Id, file.StorageKey }).ToListAsync(token);
        var referenceKeys = references.Select(file => file.StorageKey).ToHashSet(StringComparer.Ordinal);
        var present = new HashSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(root))
        {
            foreach (var entry in new DirectoryInfo(root).EnumerateFiles())
            {
                token.ThrowIfCancellationRequested();
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                var name = entry.Name;
                if (IsKey(name))
                {
                    present.Add(name);
                    if (!referenceKeys.Contains(name) && entry.LastWriteTimeUtc < cutoff)
                        await output.WriteLineAsync($"orphan candidate {name}");
                }
                else if (name.EndsWith(".staging", StringComparison.Ordinal) &&
                         IsKey(name.Split('.')[0]) && entry.LastWriteTimeUtc < cutoff)
                    await output.WriteLineAsync($"stale staging candidate {name}");
            }
        }
        foreach (var file in references.Where(file => !present.Contains(file.StorageKey)))
        {
            token.ThrowIfCancellationRequested();
            await output.WriteLineAsync($"metadata without local blob {file.Id}");
        }
        await output.WriteLineAsync("Private file inventory finished. No files or metadata were modified.");
    }

    private static bool IsKey(string key) => Guid.TryParseExact(key, "N", out var id) && key == id.ToString("N");
}
