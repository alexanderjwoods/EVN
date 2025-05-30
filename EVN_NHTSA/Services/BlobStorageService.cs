using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EVN_NHTSA.Services
{
    /// <inheritdoc />
    public class BlobStorageService(ILogger<BlobStorageService> logger, IConfiguration configuration, BlobServiceClient blobServiceClient) : IBlobStorageService
    {
        /// <inheritdoc />
        public async Task<(Stream?, string)> RetrieveBlobAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = blobServiceClient.GetBlobContainerClient(configuration["BlobContainerName"]);
                if (!containerClient.Exists())
                { 
                    return (null, "Blob Container wasn't found.");
				}

                var blobClient = containerClient.GetBlobClient(blobName.EndsWith(".csv") ? blobName : $"{blobName}.csv");
                if (!blobClient.Exists())
                {
                    return (null, "Blob doesn't exist.");
                }
                var blobStream = await blobClient.OpenReadAsync(new BlobOpenReadOptions(false));
                return (blobStream, string.Empty);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                logger.LogError(ex, $"Blob not found. Container: {containerName}, Blob: {blobName}");
                throw;
            }
            catch (Azure.RequestFailedException ex)
            {
                logger.LogError(ex, $"Azure request failed when retrieving blob. Status: {ex.Status}, ErrorCode: {ex.ErrorCode}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Invalid operation when retrieving blob. This might be due to misconfiguration.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred when retrieving blob.");
                throw;
            }
        }

		/// <inheritdoc />
		public Task<(bool, string)> UploadBlobAsync(string containerName, string blobName, byte[] inputBytes)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(configuration["BlobContainerName"]);
            if (!containerClient.Exists())
            {
                return Task.FromResult((false, "Blob Container wasn't found."));
			}

            var blobClient = containerClient.GetBlobClient(blobName.EndsWith(".csv") ? blobName : $"{blobName}.csv");
            try
            {
                using var inputStream = new MemoryStream(inputBytes);
				blobClient.Upload(inputStream, overwrite: true);
				return Task.FromResult((true, string.Empty));
			}
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload blob. Container: {ContainerName}, Blob: {BlobName}", containerName, blobName);
				return Task.FromResult((false, $"Failed to upload blob."));
            }
		}
    }
}