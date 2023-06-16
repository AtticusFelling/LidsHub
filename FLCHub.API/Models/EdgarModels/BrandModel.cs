using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FLCHub.Models.EdgarModels
{
    public class BrandModel
    {
        [JsonProperty("company")]
        public string Company { get; set; }
        [JsonProperty("vendorCode")]
        public string VendorCode { get; set; }
        [JsonProperty("vendorAccount")]
        public string VendorAccount { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class BrandModelRequest
    {
        [JsonProperty("model")]
        public BrandModel Model { get; set; }
    }
}
