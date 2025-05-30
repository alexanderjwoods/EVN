namespace EVN_NHTSA.Models.RequestModels
{
	public class VehiclesListRequest
	{
		public int PageSize { get; set; } = 10;
		public int PageNumber { get; set; } = 1;
		public string DealerId { get; set; } = string.Empty;
		public DateTime? ModifiedAfter { get; set; } = null;
	}
}