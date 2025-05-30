using System.Collections.Concurrent;
using System.Threading.Channels;
using EVN_NHTSA.Models.RequestModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace EVN_NHTSA.Services
{
    public class RequestWorkerFactory : IRequestWorkerFactory
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentBag<IRequestWorker> _workerPool = [];


        public RequestWorkerFactory(ILogger<RequestWorker> workerLogger,
                                    ILogger<IRequestWorkerFactory> logger,
									IConfiguration configuration,
                                    IBlobStorageService blobStorageService,
                                    ICsvService csvService,
                                    IDatabaseService databaseService,
                                    INHTSAService nhtsaService,
									JsonSerializerSettings jsonSerializerSettings)
        {
            ArgumentNullException.ThrowIfNull(workerLogger);
			ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(blobStorageService);
            ArgumentNullException.ThrowIfNull(csvService);
            ArgumentNullException.ThrowIfNull(databaseService);
            ArgumentNullException.ThrowIfNull(nhtsaService);
			ArgumentNullException.ThrowIfNull(jsonSerializerSettings);

			var maxWorkers = configuration.GetValue<int>("WorkerPoolSize");

			_semaphore = new SemaphoreSlim(maxWorkers, maxWorkers);

			for (int i = 0; i < maxWorkers; i++)
            {
                _workerPool.Add(new RequestWorker(workerLogger, configuration, blobStorageService, csvService, databaseService, nhtsaService, jsonSerializerSettings));
            }
        }

        /// <inheritdoc/>
        public async Task ProcessRequestAsync<TRequest>(TRequest request, ChannelWriter<string> channelWriter)
        {
            await _semaphore.WaitAsync();

            if (!_workerPool.TryTake(out var worker))
            {
                throw new InvalidOperationException("No available workers in the pool.");
            }

            _ = worker.ExecuteRequestAsync(request, channelWriter).ContinueWith(task =>
            {
                _workerPool.Add(worker);
                _semaphore.Release();
            });
        }
    }
}