using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using EVN_NHTSA.Models;
using EVN_NHTSA.Services;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

using Moq;

namespace EVN_NHTSA.Tests.Services
{
	public class CosmosDBServiceTests
	{
		[Fact]
		public async Task InsertAsync_ShouldSuccessfullyInsert_SingleValidEntity()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var testEntity = new CosmosEntity { Id = "valid-id" };

			var mockResponse = new Mock<ItemResponse<CosmosEntity>>();
			mockResponse.Setup(r => r.StatusCode).Returns(HttpStatusCode.Created);

			mockContainer
				.Setup(container => container.CreateItemAsync(
					It.IsAny<CosmosEntity>(),
					It.IsAny<PartitionKey>(),
					null,
					CancellationToken.None))
				.ReturnsAsync(mockResponse.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var (updatedRecords, errors) = await service.InsertAsync([testEntity]);

			// Assert
			Assert.Single(updatedRecords);
			Assert.Empty(errors);
			Assert.Equal("valid-id", updatedRecords[0].Id);
		}

		[Fact]
		public async Task InsertAsync_ShouldReturnError_WhenEntityNotCreated()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var testEntity = new CosmosEntity { Id = "conflicting-id" };

			var mockResponse = new Mock<ItemResponse<CosmosEntity>>();
			mockResponse.Setup(r => r.StatusCode).Returns(HttpStatusCode.Conflict);

			mockContainer
				.Setup(container => container.CreateItemAsync(
					It.IsAny<CosmosEntity>(),
					It.IsAny<PartitionKey>(),
					null,
					CancellationToken.None))
				.ReturnsAsync(mockResponse.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var (updatedRecords, errors) = await service.InsertAsync([testEntity]);

			// Assert
			Assert.Empty(updatedRecords);
			Assert.Single(errors);
		}

		[Fact]
		public async Task InsertAsync_ShouldHandleMultipleEntitiesWithMixedValidAndInvalidIds()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var testEntity1 = new CosmosEntity { Id = "conflicting-id" };
			var testEntity2 = new CosmosEntity { Id = "valid-id" };

			mockContainer
				.SetupSequence(container => container.CreateItemAsync(
					It.IsAny<CosmosEntity>(),
					It.IsAny<PartitionKey>(),
					null,
					CancellationToken.None))
				.ReturnsAsync(() =>
				{
					var response = new Mock<ItemResponse<CosmosEntity>>();
					response.Setup(r => r.StatusCode).Returns(HttpStatusCode.BadRequest);
					return response.Object;
				})
				.ReturnsAsync(() =>
				{
					var response = new Mock<ItemResponse<CosmosEntity>>();
					response.Setup(r => r.StatusCode).Returns(HttpStatusCode.Created);
					return response.Object;
				});

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var (updatedRecords, errors) = await service.InsertAsync([testEntity1, testEntity2]);

			// Assert
			Assert.Single(updatedRecords);
			Assert.Single(errors);
		}

		[Fact]
		public async Task InsertAsync_ShouldReturnErrorMessage_ForGeneralExceptionDuringInsertion()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var testEntity = new CosmosEntity { Id = "exception-id" };

			mockContainer
				.Setup(container => container.CreateItemAsync(
					It.IsAny<CosmosEntity>(),
					It.IsAny<PartitionKey>(),
					null,
					CancellationToken.None))
				.ThrowsAsync(new Exception("General exception occurred"));

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var (updatedRecords, errors) = await service.InsertAsync([testEntity]);

			// Assert
			Assert.Empty(updatedRecords);
			Assert.Single(errors);
			Assert.Contains("Error inserting vehicle exception-id: General exception occurred", errors);
		}

		[Fact]
		public async Task GetAsync_ShouldReturnVehicles_WhenIteratorHasResults()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var entity = new CosmosEntity { Id = "existing" };

			var mockFeedResponse = new Mock<FeedResponse<CosmosEntity>>();
			mockFeedResponse.Setup(response => response.GetEnumerator())
							.Returns(new List<CosmosEntity> { entity }.GetEnumerator());

			var mockIterator = new Mock<FeedIterator<CosmosEntity>>();
			mockIterator.SetupSequence(iterator => iterator.HasMoreResults)
						.Returns(true)
						.Returns(false);
			mockIterator.Setup(iterator => iterator.ReadNextAsync(It.IsAny<CancellationToken>()))
						.ReturnsAsync(mockFeedResponse.Object);

			mockContainer.Setup(container => container.GetItemQueryIterator<CosmosEntity>(
				It.IsAny<QueryDefinition>(),
				null,
				It.IsAny<QueryRequestOptions>()))
				.Returns(mockIterator.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var result = await service.GetAsync<CosmosEntity>("non-existent-id", CancellationToken.None);

			// Assert
			Assert.Equal(entity, result);
		}

		[Fact]
		public async Task GetAsync_ShouldReturnNull_WhenIteratorHasNoResults()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var entity = new CosmosEntity { Id = "existing" };

			var mockFeedResponse = new Mock<FeedResponse<CosmosEntity>>();
			mockFeedResponse.Setup(response => response.GetEnumerator())
							.Returns(new List<CosmosEntity>().GetEnumerator());

			var mockIterator = new Mock<FeedIterator<CosmosEntity>>();
			mockIterator.SetupSequence(iterator => iterator.HasMoreResults)
						.Returns(true)
						.Returns(false);
			mockIterator.Setup(iterator => iterator.ReadNextAsync(It.IsAny<CancellationToken>()))
						.ReturnsAsync(mockFeedResponse.Object);

			mockContainer.Setup(container => container.GetItemQueryIterator<CosmosEntity>(
				It.IsAny<QueryDefinition>(),
				null,
				It.IsAny<QueryRequestOptions>()))
				.Returns(mockIterator.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var result = await service.GetAsync<CosmosEntity>("non-existent-id", CancellationToken.None);

			// Assert
			Assert.Null(result);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("    ")]
		public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenEntityIdIsNull(string id)
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var testEntity = new CosmosEntity { Id = id };

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(testEntity));
		}

		[Fact]
		public async Task UpdateAsync_ShouldSuccessfullyUpdateEntity_WhenReplaceItemAsyncReturnsOK()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var testEntity = new CosmosEntity { Id = "valid-id" };

			var mockResponse = new Mock<ItemResponse<CosmosEntity>>();
			mockResponse.Setup(r => r.StatusCode).Returns(HttpStatusCode.OK);

			mockContainer
				.Setup(container => container.ReplaceItemAsync(
					It.IsAny<CosmosEntity>(),
					It.IsAny<string>(),
					It.IsAny<PartitionKey>(),
					It.IsAny<ItemRequestOptions>(),
					CancellationToken.None))
				.ReturnsAsync(mockResponse.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			await service.UpdateAsync(testEntity);

			// Assert
			mockContainer.Verify(container => container.ReplaceItemAsync(
				testEntity,
				testEntity.Id,
				new PartitionKey(testEntity.Id),
				It.IsAny<ItemRequestOptions>(),
				CancellationToken.None), Times.Once);
		}

		[Fact]
		public async Task UpdateAsync_ShouldThrowException_WhenReplaceItemAsyncReturnsNotOk()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var testEntity = new CosmosEntity { Id = "valid-id" };

			var mockResponse = new Mock<ItemResponse<CosmosEntity>>();
			mockResponse.Setup(r => r.StatusCode).Returns(HttpStatusCode.BadGateway);

			mockContainer
				.Setup(container => container.ReplaceItemAsync(
					It.IsAny<CosmosEntity>(),
					It.IsAny<string>(),
					It.IsAny<PartitionKey>(),
					It.IsAny<ItemRequestOptions>(),
					CancellationToken.None))
				.ReturnsAsync(mockResponse.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act & Assert
			await Assert.ThrowsAsync<Exception>(() => service.UpdateAsync(testEntity));
		}

		[Fact]
		public async Task GetAllAsync_ShouldReturnEmptyArray_WhenContainerHasNoItems()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var mockIterator = new Mock<FeedIterator<CosmosEntity>>();
			mockIterator.Setup(iterator => iterator.HasMoreResults).Returns(false);

			mockContainer.Setup(container => container.GetItemQueryIterator<CosmosEntity>(
				It.IsAny<QueryDefinition>(),
				null,
				It.IsAny<QueryRequestOptions>()))
				.Returns(mockIterator.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var result = await service.GetAllAsync<CosmosEntity>(CancellationToken.None);

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public async Task GetAllAsync_ShouldHandleCancellationTokenCancellation()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var mockIterator = new Mock<FeedIterator<CosmosEntity>>();
			mockIterator.Setup(iterator => iterator.HasMoreResults).Returns(true);

			var cts = new CancellationTokenSource();
			var ct = cts.Token;
			cts.Cancel();

			mockContainer.Setup(container => container.GetItemQueryIterator<CosmosEntity>(
				It.IsAny<QueryDefinition>(),
				null,
				It.IsAny<QueryRequestOptions>()))
				.Returns(mockIterator.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act & Assert
			await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetAllAsync<CosmosEntity>(ct));
		}

		[Fact]
		public async Task GetAllAsync_ShouldReturnAllItemsInCorrectOrder()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var entity1 = new CosmosEntity { Id = "1" };
			var entity2 = new CosmosEntity { Id = "2" };
			var entity3 = new CosmosEntity { Id = "3" };

			var mockFeedResponse = new Mock<FeedResponse<CosmosEntity>>();
			mockFeedResponse.Setup(response => response.GetEnumerator())
							.Returns(new List<CosmosEntity> { entity1, entity2, entity3 }.GetEnumerator());

			var mockIterator = new Mock<FeedIterator<CosmosEntity>>();
			mockIterator.SetupSequence(iterator => iterator.HasMoreResults)
						.Returns(true)
						.Returns(false);
			mockIterator.Setup(iterator => iterator.ReadNextAsync(It.IsAny<CancellationToken>()))
						.ReturnsAsync(mockFeedResponse.Object);

			mockContainer.Setup(container => container.GetItemQueryIterator<CosmosEntity>(
				It.IsAny<QueryDefinition>(),
				null,
				It.IsAny<QueryRequestOptions>()))
				.Returns(mockIterator.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var result = await service.GetAllAsync<CosmosEntity>(CancellationToken.None);

			// Assert
			Assert.Equal(3, result.Length);
			Assert.Equal("1", result[0].Id);
			Assert.Equal("2", result[1].Id);
			Assert.Equal("3", result[2].Id);
		}

		[Fact]
		public async Task GetAllByDealerIdAsync_ShouldReturnAllItems_WhenMultiplePagesOfResultsExist()
		{
			// Arrange
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.Setup(c => c["DatabaseName"]).Returns("TestDatabase");
			mockConfiguration.Setup(c => c["VehiclesContainer"]).Returns("TestContainer");

			var mockCosmosClient = new Mock<CosmosClient>();
			var mockDatabase = new Mock<Database>();
			var mockContainer = new Mock<Container>();

			mockCosmosClient.Setup(client => client.GetDatabase(It.IsAny<string>())).Returns(mockDatabase.Object);
			mockDatabase.Setup(db => db.GetContainer(It.IsAny<string>())).Returns(mockContainer.Object);

			var entity1 = new CosmosEntity { Id = "1" };
			var entity2 = new CosmosEntity { Id = "2" };
			var entity3 = new CosmosEntity { Id = "3" };

			var mockFeedResponse1 = new Mock<FeedResponse<CosmosEntity>>();
			mockFeedResponse1.Setup(response => response.GetEnumerator())
							 .Returns(new List<CosmosEntity> { entity1, entity2 }.GetEnumerator());

			var mockFeedResponse2 = new Mock<FeedResponse<CosmosEntity>>();
			mockFeedResponse2.Setup(response => response.GetEnumerator())
							 .Returns(new List<CosmosEntity> { entity3 }.GetEnumerator());

			var mockIterator = new Mock<FeedIterator<CosmosEntity>>();
			mockIterator.SetupSequence(iterator => iterator.HasMoreResults)
						.Returns(true)
						.Returns(true)
						.Returns(false);
			mockIterator.SetupSequence(iterator => iterator.ReadNextAsync(It.IsAny<CancellationToken>()))
						.ReturnsAsync(mockFeedResponse1.Object)
						.ReturnsAsync(mockFeedResponse2.Object);

			mockContainer.Setup(container => container.GetItemQueryIterator<CosmosEntity>(
				It.IsAny<QueryDefinition>(),
				null,
				It.IsAny<QueryRequestOptions>()))
				.Returns(mockIterator.Object);

			var service = new CosmosDBService(mockConfiguration.Object, mockCosmosClient.Object);

			// Act
			var result = await service.GetAllByDealerIdAsync<CosmosEntity>("dealer1", CancellationToken.None);

			// Assert
			Assert.Equal(3, result.Length);
			Assert.Contains(result, e => e.Id == "1");
			Assert.Contains(result, e => e.Id == "2");
			Assert.Contains(result, e => e.Id == "3");
		}
	}
}
