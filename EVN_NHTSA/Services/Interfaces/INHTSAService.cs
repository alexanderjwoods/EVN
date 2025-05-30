using EVN_NHTSA.Models.ResponseModels;

namespace EVN_NHTSA.Services
{
	public interface INHTSAService
	{
		Task<NHTSADecodedResponse?> SearchAsync(string vin, CancellationToken cancellationToken);
	}
}