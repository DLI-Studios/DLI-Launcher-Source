using System;
using System.Threading.Tasks;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Firebase;

public interface IFirebaseStorage
{
    Task<string> UploadAvatarAsync(byte[] imageBytes, string uid);
    Task DeleteAvatarAsync(string uid);
}

public class FirebaseStorage : IFirebaseStorage
{
    private readonly IFirebaseClient _client;

    public FirebaseStorage(IFirebaseClient client)
    {
        _client = client;
    }

    private static string StorageUrl(string path) =>
        $"https://firebasestorage.googleapis.com/v0/b/{FirebaseConfig.StorageBucket}/o/{path}";

    public async Task<string> UploadAvatarAsync(byte[] imageBytes, string uid)
    {
        var fileName = $"avatars/{uid}.jpg";
        var encodedName = Uri.EscapeDataString(fileName);
        var url = $"{StorageUrl("")}?uploadType=media&name={encodedName}";

        await _client.PostBytesAsync(url, imageBytes, "image/jpeg");

        // Public download URL via the Firebase Storage download endpoint.
        return $"https://firebasestorage.googleapis.com/v0/b/{FirebaseConfig.StorageBucket}/o/{encodedName}?alt=media";
    }

    public async Task DeleteAvatarAsync(string uid)
    {
        var fileName = $"avatars/{uid}.jpg";
        var encodedName = Uri.EscapeDataString(fileName);
        await _client.DeleteAsync(StorageUrl(encodedName));
    }
}
