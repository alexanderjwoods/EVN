using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Serialization;

namespace EVN_NHTSA.Models.ResponseModels
{
    public class ErrorsResponse
    {
        public string Error { get; set; } = string.Empty;
    }

    public class ErrorResponseExample : IOpenApiExample<ErrorsResponse>
    {
        public IDictionary<string, OpenApiExample> Examples => new Dictionary<string, OpenApiExample>
        {
            {
                "ErrorResponseExample", new OpenApiExample
                {
                    Value = new OpenApiObject
                    {
                        ["Error"] = new OpenApiString("Invalid JSON payload received.")
                    }
                }
            }
        };
        public IOpenApiExample<ErrorsResponse> Build(NamingStrategy namingStrategy = null)
        {
            return this;
        }
	}
}