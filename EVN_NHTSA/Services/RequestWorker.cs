using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using EVN_NHTSA.Models;
using EVN_NHTSA.Models.RequestModels;
using EVN_NHTSA.Models.ResponseModels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Polly;
using Polly.Retry;

namespace EVN_NHTSA.Services
{
	public class RequestWorker(
		ILogger<RequestWorker> logger,
		IConfiguration configuration,
		IBlobStorageService blobStorageService,
		ICsvService csvService,
		IDatabaseService databaseService,
		INHTSAService nhtsaService,
		JsonSerializerSettings jsonSerializerSettings) : IRequestWorker
	{
		private readonly AsyncRetryPolicy _networkRetryPolicy = Policy
			.Handle<HttpRequestException>()
			.Or<TimeoutException>()
			.Or<SocketException>()
			.Or<IOException>()
			.WaitAndRetryAsync(
				retryCount: configuration.GetValue<int>("Values:RetryAttempts"),
				sleepDurationProvider: retryAttempt =>
					TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) *
						configuration.GetValue<double>("Values:RetryDelay")),
				onRetry: (exception, timeSpan, retryCount, context) =>
				{
					logger.LogWarning(exception,
						"Retry {RetryCount} encountered an error. Waiting {TimeSpan} before next retry.",
						retryCount, timeSpan);
				}
			);

		private readonly string _blobConnectionString = configuration.GetValue<string>("BlobConnectionString") ?? throw new ArgumentNullException("BlobConnectionString must be set in configuration");
		private readonly string _vehiclesBlobName = configuration.GetValue<string>("BlobContainerName") ?? throw new ArgumentNullException("BlobContainerName must be set in configuration");

		public async Task ExecuteRequestAsync<TRequest>(TRequest request, ChannelWriter<string> channelWriter)
		{
			if (request is VehiclesImportRequest)
			{
				List<string> errors = [];
				int? insertedRecordsCount = null; 
				try
				{
					var insertedRecords = await _networkRetryPolicy.ExecuteAsync(async () =>
					{
						(var csvBlob, var blobDownloadErrors) = await blobStorageService.RetrieveBlobAsync(_blobConnectionString, _vehiclesBlobName);

						errors.Add(blobDownloadErrors);

						if (csvBlob is null || csvBlob.Length == 0 || !string.IsNullOrWhiteSpace(blobDownloadErrors))
						{
							await WriteErrorReponse("The location provided for the blob is invalid or the blob does not exist. Please provide a valid blob location.", HttpStatusCode.BadRequest);
							return null;
						}

						(var csvData, var csvReadErrors) = csvService.GetCsvDataFromStream<Vehicle>(csvBlob);

						errors.AddRange(csvReadErrors);

						csvData.ForEach(vehicle => vehicle.Id = vehicle.VIN);

						(var insertedRecords, var cosmosErrors) = await databaseService.InsertAsync(csvData);

						errors.AddRange(cosmosErrors);
						return insertedRecords;
					});

					if (insertedRecords is null)
					{
						await WriteErrorReponse("No records were inserted due to previous errors.", HttpStatusCode.InternalServerError);
						return;
					}

					var csvBytes = await csvService.WriteRecordsToStreamAsync(insertedRecords.ToList());
					(bool success, string blobUploadErrors) = await blobStorageService.UploadBlobAsync(_blobConnectionString
						, $"{_vehiclesBlobName}_{(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds}.csv",
						csvBytes);

					if (success)
					{
						insertedRecordsCount = insertedRecords.Length;
					}
					else
					{
						await WriteErrorReponse(JsonConvert.SerializeObject(errors, jsonSerializerSettings), HttpStatusCode.InternalServerError);
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "An error occurred while processing the request.");
					await WriteErrorReponse($"An error occurred while executing the request.", HttpStatusCode.InternalServerError);
				}
				
				await WriteSuccessResponse(new VehiclesImportResponse
				{
					ImportedVehiclesCount = insertedRecordsCount,
					Errors = [],
					StatusCode = HttpStatusCode.OK
				});
			}
			if (request is VehicleAugmentRequest augmentVehicleRequest)
			{
				try
				{
					using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
					var ct = cts.Token;

					await _networkRetryPolicy.ExecuteAsync(async () =>
					{
						var nhtsaTask = nhtsaService.SearchAsync(augmentVehicleRequest.VIN, ct);
						var dbTask = databaseService.GetAsync<Vehicle>(augmentVehicleRequest.VIN, ct);

						Vehicle? existingVehicle = null;
						NHTSADecodedResponse? nhtsaResponse = null;

						var completedTask = await Task.WhenAny(nhtsaTask, dbTask);

						if (completedTask == nhtsaTask)
						{
							var nhtsaVehicle = await nhtsaTask;
							if (nhtsaVehicle is not null)
							{
								await dbTask.WaitAsync(ct);
								if (dbTask.Result is not null)
								{
									existingVehicle = dbTask.Result;
								}
								else
								{
									await WriteErrorReponse("Provided VIN is not found in our system.", HttpStatusCode.BadRequest);
									return;
								}
							}
							else
							{
								await WriteErrorReponse("NHTSA did not find the vehicle.", HttpStatusCode.BadRequest);
								return;
							}
						}
						else
						{
							var vehicle = await dbTask;
							if (vehicle is not null)
							{
								existingVehicle = vehicle;

								await nhtsaTask.WaitAsync(ct);
								var decodedResponse = await nhtsaTask;

								if (decodedResponse is not null)
								{
									nhtsaResponse = decodedResponse;
								}
								else
								{
									await WriteErrorReponse("NHTSA did not find the vehicle.", HttpStatusCode.BadRequest);
									return;
								}
							}
							else
							{
								await WriteErrorReponse("Provided VIN is not found in our system.", HttpStatusCode.BadRequest);
								return;
							}
						}

						if (existingVehicle is not null && nhtsaResponse is not null)
						{
							var properties = new Dictionary<string, Action<string>>
							{
								{ "Make", value => existingVehicle.Make = value },
								{ "Model", value => existingVehicle.Model = value },
								{ "Model Year", value => existingVehicle.Year = value },
								{ "Trim", value => existingVehicle.Trim = value },
								{ "Vehicle Type", value => existingVehicle.VehicleType = value },
								{ "Fuel Type - Primary", value => existingVehicle.FuelTypePrimary = value }
							};

							foreach (var result in nhtsaResponse.Results)
							{
								if (result?.Variable is null)
								{
									continue;
								}

								if (properties.TryGetValue(result.Variable, out var updateAction) && result.Value is not null && result.Value.Length > 0)
								{
									updateAction(result.Value);
									existingVehicle.ModifiedDate = DateTime.UtcNow.Date;
								}
							}

							var (updatedRecords, cosmosErrors) = await databaseService.InsertAsync([existingVehicle]);
							if (updatedRecords is not null)
							{
								await WriteSuccessResponse(new VehiclesAugmentResponse
								{
									UpdatedVehicle = existingVehicle,
									Errors = [.. cosmosErrors],
									StatusCode = HttpStatusCode.OK
								});
								return;
							}
							else
							{
								await WriteErrorReponse(JsonConvert.SerializeObject(cosmosErrors, jsonSerializerSettings), HttpStatusCode.InternalServerError);
								return;
							}
						}
						else
						{
							await WriteErrorReponse("No vehicle data found to augment.", HttpStatusCode.BadRequest);
							return;
						}
					});
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "An error occurred while processing the request.");
					await WriteErrorReponse("An error occurred while executing the request.", HttpStatusCode.InternalServerError);
				}
			}
			if (request is VehiclesListRequest vehiclesListRequest)
			{
				var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
				var ct = cts.Token;

				try
				{
					await _networkRetryPolicy.ExecuteAsync(async () =>
					{
						var vehicles = 
							string.IsNullOrWhiteSpace(vehiclesListRequest.DealerId) ? await databaseService.GetAllAsync<Vehicle>(ct) : await databaseService.GetAllByDealerIdAsync<Vehicle>(vehiclesListRequest.DealerId, ct);

						var totalCount = vehicles.Length;

						var skip = (vehiclesListRequest.PageNumber - 1) * vehiclesListRequest.PageSize;

						if (skip >= totalCount)
						{
							await WriteErrorReponse("Page number exceeds the total number of records.", HttpStatusCode.BadRequest);
							return;
						}

						vehicles = [.. vehicles.Skip(skip).Take(vehiclesListRequest.PageSize)];

						await WriteSuccessResponse(new VehiclesListResponse
						{
							Vehicles = vehicles,
							TotalCount = totalCount,
							Page = vehiclesListRequest.PageNumber,
							StatusCode = HttpStatusCode.OK
						});
					});
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "An error occurred while processing the request.");
					await WriteErrorReponse("An error occurred while executing the request.", HttpStatusCode.InternalServerError);
				}
			}
			if (request is VehicleRequest vehicleRequest)
			{
				var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
				var ct = cts.Token;

				try
				{
					await _networkRetryPolicy.ExecuteAsync(async () =>
					{
						var vehicle = await databaseService.GetAsync<Vehicle>(vehicleRequest.VIN, ct);

						if(vehicle is not null)
						{
							await WriteSuccessResponse(new VehicleResponse
							{
								Vehicle = vehicle,
								StatusCode = HttpStatusCode.OK
							});
						}
						else
						{
							await WriteErrorReponse("Vehicle not found.", HttpStatusCode.BadRequest);
						}
					});
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "An error occurred while processing the request.");
					await WriteErrorReponse("An error occurred while executing the request.", HttpStatusCode.InternalServerError);
				}
			}

			logger.LogInformation("Unsupported request type: {RequestType}", typeof(TRequest).Name);
			await WriteErrorReponse("The received json does not match the expected format.", HttpStatusCode.BadRequest);

			async Task WriteErrorReponse(string error, HttpStatusCode httpStatusCode)
			{
				if (channelWriter is not null)
				{
					await channelWriter.WaitToWriteAsync();
					channelWriter?.TryWrite(JsonConvert.SerializeObject(new VehiclesImportResponse
					{
						Errors = [error],
						StatusCode = httpStatusCode
					}, jsonSerializerSettings));
					channelWriter?.Complete();
				}
				else
				{
					logger.LogCritical("Channel writer is not available.");
				}
			}

			async Task WriteSuccessResponse<T>(T response)
			{
				if (channelWriter is not null)
				{
					await channelWriter.WaitToWriteAsync();
					channelWriter?.TryWrite(JsonConvert.SerializeObject(response, jsonSerializerSettings));
					channelWriter?.Complete();
				}
				else
				{
					logger.LogCritical("Channel writer is not available.");
				}
			}
		}
	}
}