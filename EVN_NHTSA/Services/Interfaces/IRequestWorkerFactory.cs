using System.Threading.Channels;
using EVN_NHTSA.Models.RequestModels;

namespace EVN_NHTSA.Services
{
    public interface IRequestWorkerFactory
    {
		/// <summary>
		/// Starts a new request worker to process the provided request.
		/// /// </summary>
		/// typeparam name="TRequest">The type of the request to process.</typeparam>
		/// param name="request">The request to be processed.</param>
		/// param name="channelWriter">The channel writer to write the response to.</param>
		/// <Exception cref="InvalidOperationException">Thrown when a worker could not be located to process this request.</exception>
		Task ProcessRequestAsync<TRequest>(TRequest request, ChannelWriter<string> channelWriter);
    }
}