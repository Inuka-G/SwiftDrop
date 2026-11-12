using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Business.Lifecycle;

/// An uploaded proof photo, kept free of ASP.NET types so the business layer stays testable.
public sealed record ProofPhotoUpload(Stream Content, long Length);

/// A stored proof photo ready to stream back to the client.
public sealed record ProofPhoto(Stream Content, string ContentType);

/// Checks an uploaded photo by its first bytes, not by the client's file name or
/// content type, both of which are easy to fake.
internal static class ProofPhotoRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly (byte[] Signature, int Offset, string Extension, string ContentType)[] Formats =
    [
        ([0xFF, 0xD8, 0xFF], 0, "jpg", "image/jpeg"),
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], 0, "png", "image/png"),
        ("WEBP"u8.ToArray(), 8, "webp", "image/webp"),
    ];

    /// Copies the upload into memory and returns it with its detected format.
    public static async Task<(MemoryStream Content, string Extension)> ReadAsync(ProofPhotoUpload upload, CancellationToken ct)
    {
        if (upload.Length <= 0) throw new BadRequestException("The proof photo is empty.");
        if (upload.Length > MaxBytes) throw new BadRequestException("The proof photo must be 5 MB or smaller.");

        var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, ct);
        if (buffer.Length > MaxBytes) throw new BadRequestException("The proof photo must be 5 MB or smaller.");

        var extension = Detect(buffer.GetBuffer().AsSpan(0, (int)buffer.Length))
            ?? throw new BadRequestException("The proof photo must be a JPEG, PNG or WebP image.");
        buffer.Position = 0;
        return (buffer, extension);
    }

    public static string ContentTypeFor(string key) =>
        Formats.First(f => key.EndsWith("." + f.Extension, StringComparison.Ordinal)).ContentType;

    private static string? Detect(ReadOnlySpan<byte> bytes)
    {
        foreach (var (signature, offset, extension, _) in Formats)
        {
            if (bytes.Length >= offset + signature.Length && bytes.Slice(offset, signature.Length).SequenceEqual(signature))
                return extension;
        }
        return null;
    }
}
