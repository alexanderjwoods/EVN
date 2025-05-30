using System.Net;

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Serialization;

namespace EVN_NHTSA.Models.ResponseModels
{
	public class VehiclesListResponse : ApiResponseBase
	{
		public Vehicle[]? Vehicles { get; set; } = null;
		public int Page { get; set; } = 1;
		public int TotalCount { get; set; } = 0;
	}

	public class VehiclesListResponseExample : IOpenApiExample<VehiclesListResponse>
	{
		public IDictionary<string, OpenApiExample> Examples => new Dictionary<string, OpenApiExample>
		{
			{
				"VehiclesListResponseExample", new OpenApiExample
				{
					Value = new OpenApiObject
					{
						["Vehicles"] = new OpenApiArray
						{
							new OpenApiObject
							{
								["VIN"] = new OpenApiString("1HGCM82633A123456"),
								["Make"] = new OpenApiString("Honda"),
								["Model"] = new OpenApiString("Accord"),
								["Year"] = new OpenApiString("2003"),
								["Trim"] = new OpenApiString("EX"),
								["VehicleType"] = new OpenApiString("Sedan"),
								["FuelTypePrimary"] = new OpenApiString("Gasoline")
							},
							new OpenApiObject
							{
								["VIN"] = new OpenApiString("1HGCM82633A123456"),
								["Make"] = new OpenApiString("Honda"),
								["Model"] = new OpenApiString("Accord"),
								["Year"] = new OpenApiString("2003"),
								["Trim"] = new OpenApiString("EX"),
								["VehicleType"] = new OpenApiString("Sedan"),
								["FuelTypePrimary"] = new OpenApiString("Gasoline")
							},
							new OpenApiObject
							{
								["VIN"] = new OpenApiString("1HGCM82633A123456"),
								["Make"] = new OpenApiString("Honda"),
								["Model"] = new OpenApiString("Accord"),
								["Year"] = new OpenApiString("2003"),
								["Trim"] = new OpenApiString("EX"),
								["VehicleType"] = new OpenApiString("Sedan"),
								["FuelTypePrimary"] = new OpenApiString("Gasoline")
							},
							new OpenApiObject
							{
								["VIN"] = new OpenApiString("1HGCM82633A123456"),
								["Make"] = new OpenApiString("Honda"),
								["Model"] = new OpenApiString("Accord"),
								["Year"] = new OpenApiString("2003"),
								["Trim"] = new OpenApiString("EX"),
								["VehicleType"] = new OpenApiString("Sedan"),
								["FuelTypePrimary"] = new OpenApiString("Gasoline")
							},
							new OpenApiObject
							{
								["VIN"] = new OpenApiString("1HGCM82633A123456"),
								["Make"] = new OpenApiString("Honda"),
								["Model"] = new OpenApiString("Accord"),
								["Year"] = new OpenApiString("2003"),
								["Trim"] = new OpenApiString("EX"),
								["VehicleType"] = new OpenApiString("Sedan"),
								["FuelTypePrimary"] = new OpenApiString("Gasoline")
							}
						},
						["Page"] = new OpenApiInteger(1),
						["TotalCount"] = new OpenApiInteger(100),
						["Errors"] = new OpenApiArray
						{
							new OpenApiNull()
						},
						["StatusCode"] = new OpenApiInteger((int)HttpStatusCode.OK)
					}
				}
			}
		};
		public IOpenApiExample<VehiclesListResponse> Build(NamingStrategy namingStrategy = null)
		{
			return this;
		}
	}
}