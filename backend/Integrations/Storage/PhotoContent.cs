using System.Buffers.Binary;
using SkiaSharp;

namespace HostGrid.Api.Integrations.Storage;

internal static class PhotoContent
{
    private const long MaxPixels = 24_000_000;
    private static readonly SemaphoreSlim DecodeSlots = new(2);

    internal static async Task<bool> IsValidAsync(Stream source, string mime, CancellationToken token)
    {
        if (!await HasCompleteContainerAsync(source, mime, token)) return false;
        source.Position = 0;
        await DecodeSlots.WaitAsync(token);
        try { return Decode(source, mime, token); }
        finally { DecodeSlots.Release(); }
    }

    private static async Task<bool> HasCompleteContainerAsync(Stream source, string mime, CancellationToken token)
    {
        var length = mime == "image/jpeg" ? 2 : mime == "image/png" ? 12 : 8;
        if (source.Length < length) return false;
        var bytes = new byte[length];
        source.Position = mime == "image/webp" ? 0 : source.Length - length;
        await source.ReadExactlyAsync(bytes, token);
        return mime switch
        {
            "image/jpeg" => bytes.AsSpan().SequenceEqual(new byte[] { 0xff, 0xd9 }),
            "image/png" => bytes.AsSpan().SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }),
            _ => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) + 8L == source.Length
        };
    }

    private static bool Decode(Stream source, string mime, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var input = new SKManagedStream(source, disposeManagedStream: false);
        using var codec = SKCodec.Create(input, out var result);
        if (codec is null || result != SKCodecResult.Success) return false;
        var expected = mime switch
        {
            "image/jpeg" => SKEncodedImageFormat.Jpeg,
            "image/png" => SKEncodedImageFormat.Png,
            _ => SKEncodedImageFormat.Webp
        };
        var info = codec.Info;
        if (codec.EncodedFormat != expected || info.Width <= 0 || info.Height <= 0 ||
            (long)info.Width * info.Height > MaxPixels || codec.FrameCount > 1) return false;
        using var bitmap = new SKBitmap();
        if (!bitmap.TryAllocPixels(info)) return false;
        var decoded = codec.GetPixels(info, bitmap.GetPixels(), bitmap.RowBytes, SKCodecOptions.Default);
        token.ThrowIfCancellationRequested();
        return decoded == SKCodecResult.Success;
    }
}
