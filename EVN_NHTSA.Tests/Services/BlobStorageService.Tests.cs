using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using EVN_NHTSA.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

namespace EVN_NHTSA.Tests.Services
{
	public class BlobStorageServiceTests
	{
		[Fact]
		public async Task RetrieveBlobAsync_ShouldReturnNullAndContainerNotFoundMessage_WhenContainerDoesNotExist()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<BlobStorageService>>();
			var configurationMock = new Mock<IConfiguration>();
			var blobServiceClientMock = new Mock<BlobServiceClient>();
			var containerClientMock = new Mock<BlobContainerClient>();

			var azureResponseMock = new Mock<Response<bool>>();
			azureResponseMock.Setup(r => r.Value).Returns(false);

			configurationMock.Setup(c => c["BlobContainerName"]).Returns("nonexistent-container");
			blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(containerClientMock.Object);
			containerClientMock.Setup(c => c.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);

			var service = new BlobStorageService(loggerMock.Object, configurationMock.Object, blobServiceClientMock.Object);

			// Act
			var (resultStream, resultMessage) = await service.RetrieveBlobAsync("nonexistent-container", "test-blob");

			// Assert
			Assert.Null(resultStream);
			Assert.Equal("Blob Container wasn't found.", resultMessage);
		}

		[Fact]
		public async Task RetrieveBlobAsync_ShouldReturnNullAndBlobNotFoundMessage_WhenBlobDoesNotExist()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<BlobStorageService>>();
			var configurationMock = new Mock<IConfiguration>();
			var blobServiceClientMock = new Mock<BlobServiceClient>();
			var containerClientMock = new Mock<BlobContainerClient>();
			var blobClientMock = new Mock<BlobClient>();

			var azureResponseMock = new Mock<Response<bool>>();
			azureResponseMock.SetupSequence(x => x.Value)
				.Returns(true)
				.Returns(false);

			configurationMock.Setup(c => c["BlobContainerName"]).Returns("existing-container");
			blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(containerClientMock.Object);
			containerClientMock.Setup(c => c.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);
			containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);
			blobClientMock.Setup(b => b.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);

			var service = new BlobStorageService(loggerMock.Object, configurationMock.Object, blobServiceClientMock.Object);

			// Act
			var (resultStream, resultMessage) = await service.RetrieveBlobAsync("existing-container", "nonexistent-blob");

			// Assert
			Assert.Null(resultStream);
			Assert.Equal("Blob doesn't exist.", resultMessage);
		}

		[Fact]
		public async Task RetrieveBlobAsync_ShouldReturnStreamAndEmptyMessage_WhenBlobExistsWithCsvExtension()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<BlobStorageService>>();
			var configurationMock = new Mock<IConfiguration>();
			var blobServiceClientMock = new Mock<BlobServiceClient>();
			var containerClientMock = new Mock<BlobContainerClient>();
			var blobClientMock = new Mock<BlobClient>();

			var blobStreamMock = new MemoryStream([1, 2, 3, 4, 5])
			{
				Position = 0
			};

			var azureResponseMock = new Mock<Response<bool>>();
			azureResponseMock.Setup(x => x.Value).Returns(true);

			configurationMock.Setup(c => c["BlobContainerName"]).Returns("existing-container");
			blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(containerClientMock.Object);
			containerClientMock.Setup(c => c.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);
			containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);
			blobClientMock.Setup(b => b.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);
			blobClientMock.Setup(b => b.OpenReadAsync(It.IsAny<BlobOpenReadOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(blobStreamMock);

			var service = new BlobStorageService(loggerMock.Object, configurationMock.Object, blobServiceClientMock.Object);

			// Act
			var (resultStream, resultMessage) = await service.RetrieveBlobAsync("existing-container", "test-blob.csv");

			// Assert
			Assert.NotNull(resultStream);
			Assert.Equal(string.Empty, resultMessage);
		}

		[Fact]
		public async Task UploadBlobAsync_ShouldReturnTrueAndEmptyMessage_WhenBlobIsSuccessfullyUploadedWithCsvExtension()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<BlobStorageService>>();
			var configurationMock = new Mock<IConfiguration>();
			var blobServiceClientMock = new Mock<BlobServiceClient>();
			var containerClientMock = new Mock<BlobContainerClient>();
			var blobClientMock = new Mock<BlobClient>();

			var azureResponseMock = new Mock<Response<bool>>();
			azureResponseMock.Setup(x => x.Value).Returns(true);

			configurationMock.Setup(c => c["BlobContainerName"]).Returns("existing-container");
			blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(containerClientMock.Object);
			containerClientMock.Setup(c => c.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);
			containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);
			blobClientMock.Setup(b => b.Upload(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));

			var service = new BlobStorageService(loggerMock.Object, configurationMock.Object, blobServiceClientMock.Object);

			// Act
			var (result, resultMessage) = await service.UploadBlobAsync("existing-container", "test-blob.csv", new byte[] { 1, 2, 3, 4, 5 });

			// Assert
			Assert.True(result);
			Assert.Equal(string.Empty, resultMessage);
		}

		[Fact]
		public async Task UploadBlobAsync_ShouldReturnFalseAndErrorMessage_WhenInputBytesIsEmptyArray()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<BlobStorageService>>();
			var configurationMock = new Mock<IConfiguration>();
			var blobServiceClientMock = new Mock<BlobServiceClient>();
			var containerClientMock = new Mock<BlobContainerClient>();

			var azureResponseMock = new Mock<Response<bool>>();
			azureResponseMock.Setup(x => x.Value).Returns(true);

			configurationMock.Setup(c => c["BlobContainerName"]).Returns("existing-container");
			blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(containerClientMock.Object);
			containerClientMock.Setup(c => c.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);

			var service = new BlobStorageService(loggerMock.Object, configurationMock.Object, blobServiceClientMock.Object);

			// Act
			var (result, resultMessage) = await service.UploadBlobAsync("existing-container", "test-blob.csv", new byte[] { });

			// Assert
			Assert.False(result);
			Assert.Equal("Failed to upload blob.", resultMessage);
		}
		[Fact]
		public async Task UploadBlobAsync_ShouldHandleExceptionAndReturnFalseWithErrorMessage_WhenBlobClientUploadThrowsException()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<BlobStorageService>>();
			var configurationMock = new Mock<IConfiguration>();
			var blobServiceClientMock = new Mock<BlobServiceClient>();
			var containerClientMock = new Mock<BlobContainerClient>();
			var blobClientMock = new Mock<BlobClient>();

			var azureResponseMock = new Mock<Response<bool>>();
			azureResponseMock.Setup(x => x.Value).Returns(true);

			configurationMock.Setup(c => c["BlobContainerName"]).Returns("existing-container");
			blobServiceClientMock.Setup(b => b.GetBlobContainerClient(It.IsAny<string>())).Returns(containerClientMock.Object);
			containerClientMock.Setup(c => c.Exists(It.IsAny<CancellationToken>())).Returns(azureResponseMock.Object);
			containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(blobClientMock.Object);

			blobClientMock.Setup(b => b.Upload(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
						  .Throws(new Exception("Upload failed"));

			var service = new BlobStorageService(loggerMock.Object, configurationMock.Object, blobServiceClientMock.Object);

			// Act
			var (result, resultMessage) = await service.UploadBlobAsync("existing-container", "test-blob.csv", [1, 2, 3, 4, 5]);

			// Assert
			Assert.False(result);
			Assert.Equal("Failed to upload blob.", resultMessage);
		}

		[Fact]
		public async Task UploadBlobAsync_ShouldReturnFalseAndErrorMessage_WhenBlobServiceClientIsNull()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<BlobStorageService>>();
			var configurationMock = new Mock<IConfiguration>();
			BlobServiceClient? blobServiceClient = null; // Simulate a null BlobServiceClient

			var service = new BlobStorageService(loggerMock.Object, configurationMock.Object, blobServiceClient);

			// Act & Assert
			await Assert.ThrowsAsync<NullReferenceException>(async () =>
			{
				var (result, resultMessage) = await service.UploadBlobAsync("existing-container", "test-blob.csv", new byte[] { 1, 2, 3, 4, 5 });
			});
		}
	}
}
