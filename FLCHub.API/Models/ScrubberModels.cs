using Newtonsoft.Json;

namespace FLCHub.API.Models
{
    
    public class ScrubberFolders
    {
        //---Sent out by API to give Front-End list of vendors to scrub---
        //Key: vendorName value: vendorPath
        [JsonProperty("vendorFolders")]
        public Dictionary<string, string> VendorFolders { get; set; }
    }

    public class ScrubberRequest
    {
        //---Recieved by front-end for scrubbing---

        //Holds Vendor list containing skus for scrub
        [JsonProperty("file")]
        public FileHolder File { get; set; }
        //Holds vendor ftp path
        [JsonProperty("vendorPath")]
        public string VendorPath { get; set; }
        //bool determines whether to check via SKU or UPC
        [JsonProperty("isSku")]
        public bool IsSku { get; set; }
    }

    public class ScrubberRequestModel
    {
        //---Wrapper for ScrubberRequest---
        [JsonProperty("model")]
        public ScrubberRequest Model { get; set; }
    }
}
