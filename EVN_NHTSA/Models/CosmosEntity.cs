using Newtonsoft.Json;

namespace EVN_NHTSA.Models
{
    public class CosmosEntity
    {
        /// <summary>
        /// Id used by database
        /// </summary>
        [JsonProperty("id")]
        public required string Id { get; set; }
    }
}