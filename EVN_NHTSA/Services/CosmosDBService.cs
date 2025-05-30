using System.Collections.Concurrent;
using System.Net;
using EVN_NHTSA.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace EVN_NHTSA.Services
{
    public class CosmosDBService(IConfiguration configuration,
                                 CosmosClient cosmosClient) : IDatabaseService
    {
        public async Task<(T[] updatedRecords, string[] errors)> InsertAsync<T>(IEnumerable<T> entities) where T : CosmosEntity
        {
            var database = cosmosClient.GetDatabase(configuration["DatabaseName"]);
            var container = database.GetContainer(configuration["VehiclesContainer"]);

            ConcurrentBag<T> valuesInserted = [];
            ConcurrentBag<string> errors = [];

            await Parallel.ForEachAsync(entities, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (entity, _) =>
            {
                if (!string.IsNullOrWhiteSpace(entity.Id))
                {
                    try
                    {
                        var response = await container.CreateItemAsync(
                            entity,
                            new PartitionKey(entity.Id),
                            cancellationToken: CancellationToken.None);

                        if (response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK)
                        {
                            valuesInserted.Add(entity);
                        }
                        else if (response.StatusCode is HttpStatusCode.Conflict)
                        {
                            errors.Add($"Entity with Id {entity.Id} already exists.");
						}
                        else
                        {
                            errors.Add($"Failed to insert vehicle {entity.Id}. Status code: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error inserting vehicle {entity.Id}: {ex.Message}");
                    }
                }
            }).ConfigureAwait(false);

			return (valuesInserted.ToArray(), errors.ToArray());
        }

        public async Task<T?> GetAsync<T>(string id, CancellationToken ct) where T : CosmosEntity
        {
            var database = cosmosClient.GetDatabase(configuration["DatabaseName"]);
            var container = database.GetContainer(configuration["VehiclesContainer"]);

            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            var iterator = container.GetItemQueryIterator<T>(query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(id) });

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(ct);
                var item = response.FirstOrDefault();
                if (item != null)
                {
                    return item;
                }
            }

            return null;
		}

		public async Task UpdateAsync<T>(T entity) where T : CosmosEntity
		{
			var database = cosmosClient.GetDatabase(configuration["DatabaseName"]);
            var container = database.GetContainer(configuration["VehiclesContainer"]);

            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                throw new ArgumentNullException("Entity must have a valid Id to update.", nameof(entity));
			}

            var response = await container.ReplaceItemAsync<T>(
                entity,
                entity.Id,
                new PartitionKey(entity.Id),
                cancellationToken: CancellationToken.None);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Failed to update entity with Id {entity.Id}. Status code: {response.StatusCode}");
			}
		}

		public async Task<T[]> GetAllAsync<T>(CancellationToken ct) where T : CosmosEntity
		{
			var database = cosmosClient.GetDatabase(configuration["DatabaseName"]);
			var container = database.GetContainer(configuration["VehiclesContainer"]);

			var query = new QueryDefinition("SELECT * FROM c ORDER BY c.id");
            var iterator = container.GetItemQueryIterator<T>(query);
            var results = new List<T>();

            while (iterator.HasMoreResults)
            {
                ct.ThrowIfCancellationRequested();
				var response = await iterator.ReadNextAsync(ct);
                results.AddRange(response);
			}

			return [.. results];
		}

		public async Task<T[]> GetAllByDealerIdAsync<T>(string dealerId, CancellationToken ct) where T : CosmosEntity
		{
			var database = cosmosClient.GetDatabase(configuration["DatabaseName"]);
			var container = database.GetContainer(configuration["VehiclesContainer"]);

			var query = new QueryDefinition("SELECT * FROM c WHERE c.DealerId = @dealerId")
				.WithParameter("@dealerId", dealerId);

            var iterator = container.GetItemQueryIterator<T>(query, requestOptions: new QueryRequestOptions { });

            var results = new List<T>();

			while (iterator.HasMoreResults)
			{
				var response = await iterator.ReadNextAsync(ct);
				results.AddRange(response);
			}

			return [.. results];
		}
	}
}