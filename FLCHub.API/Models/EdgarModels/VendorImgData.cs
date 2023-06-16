using FLCHub.API.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLCHub.Models.EdgarModels
{
    public class VendorImgData
    {
        [JsonProperty("ticketNumber")]
        public string TicketNumber { get; set; }
        [JsonProperty("vendor")]
        public string Vendor { get; set; }
        [JsonProperty("files")]
        public FileHolder[] Files {get; set;}
    }

    public class VendorImgDataModel
    {
        [JsonProperty("model")]
        public VendorImgData Model {get; set;}
    }
}
