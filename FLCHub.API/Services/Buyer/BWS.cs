using FLCHub.API.Models;
using Newtonsoft.Json;

namespace FLCHub.API.Services.Buyer
{
    public static class BWS
    {
        public static string GetVersions()
        {
            var files = new List<FileHolder>();

            foreach (var file in Directory.EnumerateFiles("C:\\bws"))
            {
                string fileName = file;
                if (fileName.Contains("\\bws\\") && fileName.Contains(".xlsm"))
                {
                    fileName = fileName.Substring(fileName.IndexOf("\\bws\\") + 5);
                }
                files.Add(new FileHolder
                {
                    FileName = fileName,
                    CreatedDate = File.GetCreationTime(file).ToString("MM/dd/yyyy")
                });
            }
            // byte[] bwsData = File.ReadAllBytes("Assets\\Buyer_Worksheet_V3.0_Name.xlsm");

            // FileHolder bwsFile = new FileHolder()
            // {
            //     FileName = "Buyer_Worksheet_V3.0_Name.xlsm",
            //     FileData = bwsData
            // };

            Http.Response<List<FileHolder>> response = new Http.Response<List<FileHolder>>()
            {
                StatusCode = 200,
                Status = "Worksheet delivered",
                Data = files
            };

            return JsonConvert.SerializeObject(response);
        }
        
        public static string Download(BWSDownloadRequest request)
        {
            var file = new FileHolder
            {
                FileName = request.FileName,
                FileData = File.ReadAllBytes("C:\\bws\\" + request.FileName)
            };

            Http.Response<FileHolder> response = new Http.Response<FileHolder>()
            {
                StatusCode = 200,
                Status = "Worksheet delivered",
                Data = file
            };

            return JsonConvert.SerializeObject(response);
        }

        public class BWSDownloadRequest
        {
            [JsonProperty("fileName")]
            public string FileName {get; set;}
        }
        
    }
}
