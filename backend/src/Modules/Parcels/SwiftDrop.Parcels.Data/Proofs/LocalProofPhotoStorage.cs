namespace SwiftDrop.Parcels.Data.Storage;

public interface IProofPhotoStorage
{
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct);
    Stream? OpenRead(string key);
    void Delete(string key);
}

internal sealed class LocalProofPhotoStorage(string directory) : IProofPhotoStorage
{
    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct)
    {
        var key = $"{Guid.NewGuid():N}.{fileExtension}";
        var path = GetPath(key);
        Directory.CreateDirectory(directory);
        var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        try
        {
            await using (file)
                await content.CopyToAsync(file, ct);
        }
        catch
        {
            File.Delete(path);
            throw;
        }

        return key;
    }

    public Stream? OpenRead(string key)
    {
        try
        {
            return File.OpenRead(GetPath(key));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Delete(string key) => File.Delete(GetPath(key));

    private string GetPath(string key)
    {
        if (key.Length < 34 || !Guid.TryParseExact(key[..32], "N", out _)
            || key[32..] is not (".jpg" or ".png" or ".webp"))
            throw new ArgumentException("Invalid proof photo key.", nameof(key));

        return Path.Combine(directory, key);
    }
}
