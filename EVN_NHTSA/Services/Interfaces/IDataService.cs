using EVN_NHTSA.Models;

namespace EVN_NHTSA.Services
{
    public interface IDatabaseService
    {
        Task<(T[] updatedRecords, string[] errors)> InsertAsync<T>(IEnumerable<T> entities) where T : CosmosEntity;

        Task UpdateAsync<T>(T entity) where T : CosmosEntity;

		Task<T?> GetAsync<T>(string id, CancellationToken ct) where T : CosmosEntity;
        Task<T[]> GetAllAsync<T>(CancellationToken ct) where T : CosmosEntity;
        Task<T[]> GetAllByDealerIdAsync<T>(string dealerId, CancellationToken ct) where T : CosmosEntity;
	}
}