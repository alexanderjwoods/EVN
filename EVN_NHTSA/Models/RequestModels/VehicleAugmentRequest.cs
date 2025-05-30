using Newtonsoft.Json;

namespace EVN_NHTSA.Models.RequestModels
{
	public class VehicleAugmentRequest
	{
		[JsonRequired]
		public required string VIN { get; set; }
	}
}
