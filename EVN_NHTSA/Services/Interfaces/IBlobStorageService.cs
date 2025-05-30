namespace EVN_NHTSA.Services
{
    /// <summary>
    /// Provides methods for interacting with Azure Blob Storage, including operations
    /// such as uploading, downloading, and managing blobs within a specified container.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Retrieves a blob from the specified container in Azure Blob Storage.
        /// </summary>
        /// <param name="containerName">The name of the storage container</param>
        /// <param name="blobName">Name of the blob object</param>
        /// <returns>Tuple containing the  <see cref="Stream"/> of the retrieved blob object or an error message if the blob does not exist.</returns>
        /// <exception cref="Azure.RequestFailedException">Thrown when the blob is not found or other Azure request failures occur.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an invalid operation occurs, such as misconfiguration.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs while opening the blob stream.</exception>
        /// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
        Task<(Stream?, string)> RetrieveBlobAsync(string containerName, string blobName);

		/// <summary>
		/// Uploads a blob to the specified container in Azure Blob Storage.
		/// </summary>
		/// <param name="containerName">The name of the storage container</param>
		/// <param name="blobName">Name of the blob object</param>
		/// <param name="inputStream">The <see cref="Stream"/> of the blob data to be uploaded</param>
		/// <returns></returns>
		Task<(bool, string)> UploadBlobAsync(string containerName, string blobName, byte[] inputBytes);
    }
}