using System.Net;

using EVN_NHTSA.Models.RequestModels;
using EVN_NHTSA.Models.ResponseModels;
using EVN_NHTSA.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

namespace EVN_NHTSA.Functions
{
	public class VehiclesFunction(ILogger<VehiclesFunction> logger, IRequestWorkerFactory requestWorkerFactory, JsonSerializerSettings jsonSerializerSettings) : FunctionBase<VehiclesFunction>(logger, requestWorkerFactory)
	{
		[Function(nameof(VehicleImportData))]
		[OpenApiOperation(Summary = "Import vehicle data", Description = "Imports vehicle data from a provided source.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(VehiclesImportResponse), Description = "The response containing the result of the import operation.", Example = typeof(VehiclesImportResponseExample))]
		[OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "An error occurred while processing the request.")]
		public async Task<HttpResponseData> VehicleImportData([HttpTrigger(AuthorizationLevel.Function, "post", Route = "vehicles/import")] HttpRequestData httpRequestData)
		{
			logger.LogInformation("C# HTTP trigger function processed a request at function ImportVehicles.");

			try
			{
				return await ExecuteRequestAsync<VehiclesImportRequest, VehiclesImportResponse>(httpRequestData, new VehiclesImportRequest());
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while processing the request.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
				return response;
			}
		}

		[Function(nameof(AugmentVehicleData))]
		[OpenApiOperation(Summary = "Augment vehicle data", Description = "Augments vehicle data with additional information based on the provided request.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(VehiclesAugmentResponse), Description = "The response containing the augmented vehicle data.", Example = typeof(VehiclesAugmentResponseExample))]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ErrorsResponse), Description = "Invalid JSON payload received.", Example = typeof(ErrorResponseExample))]
		[OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "An error occurred while processing the request.")]
		public async Task<HttpResponseData> AugmentVehicleData([HttpTrigger(AuthorizationLevel.Function, "patch", Route = "vehicles/augment")] HttpRequestData httpRequestData)
		{
			logger.LogInformation("C# HTTP trigger function processed a request at function SearchVehicles.");

			try
			{
				var requestBody = await httpRequestData.ReadAsStringAsync();

				var request = JsonConvert.DeserializeObject<VehicleAugmentRequest>(requestBody, jsonSerializerSettings);

				return await ExecuteRequestAsync<VehicleAugmentRequest, VehiclesAugmentResponse>(httpRequestData, request);
			}
			catch (JsonException ex)
			{
				logger.LogError(ex, "Invalid JSON payload received.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.BadRequest);
				await response.WriteStringAsync(JsonConvert.SerializeObject(new ErrorsResponse
				{
					Error = "Invalid JSON payload received."
				}, jsonSerializerSettings));
				return response;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while processing the request.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
				return response;
			}
		}

		[Function(nameof(GetVehicles))]
		[OpenApiOperation(Summary = "Get a list of vehicles", Description = "Retrieves a list of vehicles based on the provided request parameters.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(VehiclesListResponse), Description = "The response containing the list of vehicles.", Example = typeof(VehiclesListResponseExample))]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ErrorsResponse), Description = "Invalid request payload.", Example = typeof(ErrorResponseExample))]
		[OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "An error occurred while processing the request.")]
		public async Task<HttpResponseData> GetVehicles([HttpTrigger(AuthorizationLevel.Function, "get", Route = "vehicles/list")] HttpRequestData httpRequestData)
		{
			logger.LogInformation("C# HTTP trigger function processed a request at function GetVehicles.");

			try
			{
				var body = await httpRequestData.ReadAsStringAsync();

				var request = JsonConvert.DeserializeObject<VehiclesListRequest>(body, jsonSerializerSettings);

				if (request is null)
				{
					var response = httpRequestData.CreateResponse(HttpStatusCode.BadRequest);
					await response.WriteStringAsync("Invalid request payload.");
					return response;
				}

				return await ExecuteRequestAsync<VehiclesListRequest, VehiclesListResponse>(httpRequestData, request);
			}
			catch (JsonException ex)
			{
				logger.LogError(ex, "Invalid JSON payload received.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.BadRequest);
				await response.WriteStringAsync(JsonConvert.SerializeObject(new ErrorsResponse
				{
					Error = "Invalid JSON payload received."
				}, jsonSerializerSettings));
				return response;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while processing the request.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
				return response;
			}
		}

		[Function(nameof(GetVehicle))]
		[OpenApiOperation(Summary = "Get vehicle by VIN", Description = "Retrieves a vehicle by its VIN (Vehicle Identification Number).")]
		[OpenApiParameter("vin", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The VIN of the vehicle to retrieve.")]
		[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(VehicleResponse), Description = "The response containing the vehicle data.", Example = typeof(VehicleResponseExample))]
		[OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(ErrorsResponse), Description = "Invalid VIN provided.", Example = typeof(ErrorResponseExample))]
		[OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "An error occurred while processing the request.")]
		public async Task<HttpResponseData> GetVehicle([HttpTrigger(AuthorizationLevel.Function, "get", Route = "vehicles")] HttpRequestData httpRequestData)
		{
			logger.LogInformation("C# HTTP trigger function processed a request at function GetVehicle.");

			var vin = httpRequestData.Query["vin"]?.ToString();

			try
			{
				if (string.IsNullOrWhiteSpace(vin) || !VinValidationRegex().IsMatch(vin))
				{
					var response = httpRequestData.CreateResponse(HttpStatusCode.BadRequest);
					await response.WriteStringAsync("Invalid VIN provided.");
					return response;
				}

				return await ExecuteRequestAsync<VehicleRequest, VehicleResponse>(httpRequestData, new VehicleRequest
				{
					VIN = vin
				});
			}
			catch (JsonException ex)
			{
				logger.LogError(ex, "Invalid JSON payload received.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.BadRequest);
				await response.WriteStringAsync(JsonConvert.SerializeObject(new ErrorsResponse
				{
					Error = "Invalid JSON payload received."
				}, jsonSerializerSettings));
				return response;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while processing the request.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
				return response;
			}
		}
	}
}