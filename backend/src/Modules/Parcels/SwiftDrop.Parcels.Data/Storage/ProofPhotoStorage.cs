using System.Text.RegularExpressions;

namespace SwiftDrop.Parcels.Data.Storage;

/// Where proof-of-delivery photos are kept. Swappable (for example for blob storage)
/// without touching the business logic.
public interface IProofPhotoStorage
{
    /// Stores the photo under a generated key and returns that key.
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct);
    Stream? OpenRead(string key);
    void Delete(string key);
}

/// Keeps photos as files in one folder, set by Parcels:ProofStoragePath.
/// The file name is always a generated GUID, never the uploaded name, so a
/// client can't choose where a file is written or read from.
internal sealed partial class LocalProofPhotoStorage(string rootPath) : IProofPhotoStorage
{
    [GeneratedRegex(@"^[0-9a-f]{32}\.(jpg|png|webp)$")]
    private static partial Regex KeyPattern();

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct)
    {
        Directory.CreateDirectory(rootPath);
        var key = $"{Guid.NewGuid():N}.{fileExtension}";
        await using var file = new FileStream(PathFor(key), FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Stream? OpenRead(string key)
    {
        var path = PathFor(key);
        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
    }

    public void Delete(string key)
    {
        var path = PathFor(key);
        if (File.Exists(path)) File.Delete(path);
    }

    private string PathFor(string key) =>
        KeyPattern().IsMatch(key)
            ? Path.Combine(rootPath, key)
            : throw new ArgumentException("Invalid proof photo key.", nameof(key));
}
