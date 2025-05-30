using System.Threading.Channels;
using EVN_NHTSA.Models.RequestModels;
using EVN_NHTSA.Models.ResponseModels;

namespace EVN_NHTSA.Services
{
    public interface IRequestWorker
    {
		/// <summary>
		/// Executes the request and returns the response.
		/// </summary>
		/// <typeparam name="TRequest">The response type that the worker will return.</typeparam>
		/// <param name="request">The request to be processed.</param>
		/// param name="channelWriter">The channel writer to write the response to.</param>
		/// <returns></returns>
		Task ExecuteRequestAsync<TRequest>(TRequest request,  ChannelWriter<string> channelWriter);
    }
}