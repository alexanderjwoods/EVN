
using System.Net;

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Serialization;

namespace EVN_NHTSA.Models.ResponseModels
{
    public class VehicleResponse : ApiResponseBase
	{
		public Vehicle? Vehicle { get; set; }
	}

	public class VehicleResponseExample : IOpenApiExample<VehicleResponse>
	{
		public IDictionary<string, OpenApiExample> Examples => new Dictionary<string, OpenApiExample>
		{
			{
				"VehiclesResponseExample", new OpenApiExample
				{
					Value = new OpenApiObject
					{
						["Vehicle"] = new OpenApiObject
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

		public IOpenApiExample<VehicleResponse> Build(NamingStrategy namingStrategy = null)
		{
			return this;
		}
	}
}