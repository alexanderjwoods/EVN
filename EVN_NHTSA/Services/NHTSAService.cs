using EVN_NHTSA.Models.ResponseModels;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace EVN_NHTSA.Services
{
	public class NHTSAService(ILogger<NHTSAService> logger, IHttpClientFactory httpClientFactory) : INHTSAService
	{
		private readonly string _decodeVinUrl = "https://vpic.nhtsa.dot.gov/api/vehicles/decodevin/";

		/// <inheritdoc/>
		public async Task<NHTSADecodedResponse?> SearchAsync(string vin, CancellationToken cancellationToken)
		{
			try
			{
				var httpClient = httpClientFactory.CreateClient();
				var response = await httpClient.GetAsync(
					$"{_decodeVinUrl}{vin}?format=json",
					cancellationToken);

				response.EnsureSuccessStatusCode();
				var content = await response.Content.ReadAsStringAsync(cancellationToken);

				// Parse response (simplified example)
				var nhtsaData = JsonConvert.DeserializeObject<NHTSADecodedResponse>(content);
				
				return nhtsaData;
			}
			catch (TaskCanceledException)
			{
				return null;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error querying NHTSA for ID: {vin}", vin);
				return null;
			}
		}
	}
}
