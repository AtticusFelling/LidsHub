using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FLCHub.API.Models
{
    public class FileHolder
    {
        [JsonProperty("fileName")]
        public string FileName {get; set;}
        [JsonProperty("fileData")]
        public byte[] FileData {get; set;}
        [JsonProperty("createdDate")]
        public string CreatedDate {get; set;}
    }
}