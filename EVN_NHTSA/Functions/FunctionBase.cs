using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Channels;

using EVN_NHTSA.Models.RequestModels;
using EVN_NHTSA.Models.ResponseModels;
using EVN_NHTSA.Services;

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace EVN_NHTSA.Functions
{
    public partial class FunctionBase<T>(ILogger<T> logger, IRequestWorkerFactory requestWorkerFactory)
    {
		[GeneratedRegex(@"^[A-HJ-NPR-Z0-9]{17}$")]
		internal static partial Regex VinValidationRegex();

        public async Task<HttpResponseData> ExecuteRequestAsync<TRequest, TResponse>(HttpRequestData httpRequestData, TRequest request)
             where TResponse : ApiResponseBase
        {
            var channel = Channel.CreateBounded<string>(
                new BoundedChannelOptions(1)
                {
                    SingleReader = true,
                    SingleWriter = true
                });

            await requestWorkerFactory.ProcessRequestAsync<TRequest>(request, channel.Writer);

            await channel.Reader.WaitToReadAsync();
            var workerResponseString = await channel.Reader.ReadAsync();

            try
            {
                var workerResponse = JsonConvert.DeserializeObject<TResponse>(workerResponseString);

                var response = httpRequestData.CreateResponse(
                    workerResponse is null ? HttpStatusCode.InternalServerError :
                        workerResponse.Errors is null
                            ? HttpStatusCode.OK : workerResponse.StatusCode
                );

                await response.WriteAsJsonAsync(workerResponse);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing the request response.");
				var response = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
                return response;
            }
        }
    }
}