using System.Net;

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Serialization;

namespace EVN_NHTSA.Models.ResponseModels
{
	public class VehiclesAugmentResponse : ApiResponseBase
	{
		public Vehicle? UpdatedVehicle { get; set; }
	}

	public class VehiclesAugmentResponseExample : IOpenApiExample<VehiclesAugmentResponse>
	{
		public IDictionary<string, OpenApiExample> Examples => new Dictionary<string, OpenApiExample>
		{
			{
				"VehiclesAugmentResponseExample", new OpenApiExample
				{
					Value = new OpenApiObject
					{
						["UpdatedVehicle"] = new OpenApiObject
						{
							["VIN"] = new OpenApiString("1HGCM82633A123456"),
							["Make"] = new OpenApiString("Honda"),
							["Model"] = new OpenApiString("Accord"),
							["Year"] = new OpenApiString("2003"),
							["Trim"] = new OpenApiString("EX"),
							["VehicleType"] = new OpenApiString("Sedan"),
							["FuelTypePrimary"] = new OpenApiString("Gasoline")
						},
						["Errors"] = new OpenApiArray
						{
							new OpenApiNull()
						},
						["StatusCode"] = new OpenApiInteger((int)HttpStatusCode.OK)
					}
				}
			}
		};
		public IOpenApiExample<VehiclesAugmentResponse> Build(NamingStrategy namingStrategy = null)
		{
			return this;
		}
	}
}