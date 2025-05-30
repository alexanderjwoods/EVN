namespace EVN_NHTSA.Models.ResponseModels
{
    /// <summary>
    /// Represents the response for a polling request.
    /// </summary>
    public class PollingRequestResponse
    {
        /// <summary>
        /// The unique identifier for the polling request.
        /// </summary>
        public required string PollingIdentifier { get; set; }
    }
}