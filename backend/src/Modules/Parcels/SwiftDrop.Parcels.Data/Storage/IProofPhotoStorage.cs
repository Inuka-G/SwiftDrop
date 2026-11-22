namespace SwiftDrop.Parcels.Data.Storage;

public interface IProofPhotoStorage
{
    Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct);
    Stream? OpenRead(string key);
    void Delete(string key);
}
