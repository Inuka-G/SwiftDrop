namespace SwiftDrop.Parcels.Data.Storage;

public sealed class LocalProofPhotoStorage : IProofPhotoStorage
{
    private readonly string rootPath;

    public LocalProofPhotoStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        this.rootPath = Path.GetFullPath(rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = fileExtension.Trim().TrimStart('.');
        if (extension.Length is 0 || extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("The file extension is invalid.", nameof(fileExtension));

        Directory.CreateDirectory(rootPath);

        var key = $"{Guid.NewGuid():N}.{extension}";
        var path = GetSafePath(key);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await content.CopyToAsync(output, ct);
        return key;
    }

    public Stream? OpenRead(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var path = GetSafePath(key);
        return File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    public void Delete(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var path = GetSafePath(key);
        if (File.Exists(path)) File.Delete(path);
    }

    private string GetSafePath(string key)
    {
        if (key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            key.Contains('/') ||
            key.Contains('\\'))
            throw new ArgumentException("The storage key is invalid.", nameof(key));

        return Path.Combine(rootPath, key);
    }
}
