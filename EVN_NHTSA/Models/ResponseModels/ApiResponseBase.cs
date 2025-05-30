using System.Net;

namespace EVN_NHTSA.Models.ResponseModels
{
    public class ApiResponseBase
    {
        public string[]? Errors { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}