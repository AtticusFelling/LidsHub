using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

namespace FLCHub.API.Models
{
    public class Http
    {
        public class Response<T>
        {
            [JsonProperty("statusCode")]
            public int StatusCode { get; set; }
            [JsonProperty("status")]
            public string Status { get; set; }
            [JsonProperty("data")]
            public T Data { get; set; }
            [JsonProperty("headers")]
            public Dictionary<string, string>? Headers { get; set; }

            [JsonProperty("Access-Control-Allow-Origin")]
            public string AccessControlAllowOrigin = "*";
        }
    }
}
