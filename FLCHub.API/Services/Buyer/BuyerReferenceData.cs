using FLCHub.API.Models;
using Newtonsoft.Json;

namespace FLCHub.API.Service.Buyer
{
    public static class BuyerReferenceData
    {
        public static string GetBuyerReferenceData()
        {
            var colleges = new[] { "Indiana", "UCLA", "Alabama", "North Carolina", "Notre Dame" };
            var brands = new[] { "Nike", "Adidas", "New Era", "Champion", "Mitchell & Ness", "Retro Brand" };
            // Http.Response is a class I made that can take any data type as the "data" property (so if you wanted to return an array you would declare it as new Http.Response<string[]> or 
            // if you wanted to return an int you would declare it as new Http.Response<int>)
            var response = new Http.Response<BuyerData[]>
            {
                Status = "success",
                Data = new BuyerData[]
                {
                    new BuyerData
                    {
                        Topic = "Colleges",
                        Values = colleges
                    },
                    new BuyerData
                    {
                        Topic = "Brands",
                        Values = brands
                    }
                }
            };

            return JsonConvert.SerializeObject(response);
        }

        public class BuyerData
        {
            [JsonProperty("topic")]
            public string Topic { get; set; }
            [JsonProperty("values")]
            public string[] Values { get; set; }
        }
    }

}


