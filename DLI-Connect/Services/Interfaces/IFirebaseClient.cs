using System.Text.Json;

namespace DLI.Connect.Services.Interfaces;

public interface IFirebaseClient
{
    string? ApiKey { get; }
    string? ProjectId { get; }

    Task<JsonElement> PostAsync(string url, object body);
    Task<JsonElement> PostBytesAsync(string url, byte[] body, string contentType);
    Task<JsonElement> GetAsync(string url);
    Task<JsonElement> PatchAsync(string url, object body);
    Task DeleteAsync(string url);
}