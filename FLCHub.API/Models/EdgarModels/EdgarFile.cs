using Newtonsoft.Json;

namespace FLCHub.Models.EdgarModels 
{   
        public class EdgarFileRequest
        {
            [JsonProperty("skus")]
            public string[] Skus {get;set;}
        }
}