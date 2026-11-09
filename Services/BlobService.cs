using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services
{
    public interface IBlobService
    {
        /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
        Task<string> UploadImageAsync(IFormFile file, string folder);
    }

    public class BlobService : IBlobService
    {
        private static readonly Dictionary<string, string> AllowedTypes = new()
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        private readonly IConfiguration _config;
        private BlobContainerClient? _container;

        public BlobService(IConfiguration config) => _config = config;

        // Created lazily so the API (and the unit tests) start without Azure credentials.
        private async Task<BlobContainerClient> GetContainerAsync()
        {
            if (_container != null) return _container;

            var conn = _config["AzureBlob:ConnectionString"];
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");

            var name = _config["AzureBlob:ContainerName"] ?? "raceday-images";
            var client = new BlobContainerClient(conn, name);
            await client.CreateIfNotExistsAsync(PublicAccessType.Blob);
            return _container = client;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded.");
            if (file.Length > MaxBytes)
                throw new ArgumentException("Image must be 5 MB or smaller.");
            if (!AllowedTypes.TryGetValue(file.ContentType.ToLowerInvariant(), out var ext))
                throw new ArgumentException("Only JPG, PNG or WEBP images are allowed.");

            var container = await GetContainerAsync();
            var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{ext}");

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            return blob.Uri.ToString();
        }
    }
}