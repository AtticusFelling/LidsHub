using Newtonsoft.Json;

namespace FLCHub.API.Models
{
    public class MissingImgData
    {
        [JsonProperty("imgReport")]
        public FileHolder[] ImgReport { get; set; }

        [JsonProperty("poReport")]
        public FileHolder[] PoReport { get; set; }

        [JsonProperty("schools")]
        public List<string> schools { get; set; }
    }

    public class MissingImgDataModel
    {
        [JsonProperty("model")]
        public MissingImgData Model { get; set; }
    }
}
