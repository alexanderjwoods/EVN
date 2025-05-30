using System.Net;

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Serialization;

namespace EVN_NHTSA.Models.ResponseModels
{
    public class VehiclesImportResponse : ApiResponseBase
    {
        public int? ImportedVehiclesCount { get; set; }
    }

	public class VehiclesImportResponseExample : IOpenApiExample<VehiclesImportResponse>
	{
		public IDictionary<string, OpenApiExample> Examples => new Dictionary<string, OpenApiExample>
		{
			{
				"VehiclesImportResponseExample", new OpenApiExample
				{
					Value = new OpenApiObject
					{
						["ImportedVehiclesCount"] = new OpenApiInteger(200),
						["Errors"] = new OpenApiArray
						{
							new OpenApiString("Error 1: Invalid VIN format.")
						},
						["StatusCode"] = new OpenApiInteger((int)HttpStatusCode.OK)
					}
				}
			}
		};

		public IOpenApiExample<VehiclesImportResponse> Build(NamingStrategy namingStrategy = null)
		{
			return this;
		}
	}
}