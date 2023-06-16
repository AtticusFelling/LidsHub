using Azure.Core;
using FLCHub.API.Models;
using FLCHub.API.Services.Generic;
using System.Data;
using System.Net.FtpClient;
using System.Numerics;


namespace FLCHub.API.Services.It
{
    public class Scrubber
    {
        public static Http.Response<ScrubberFolders> GetVendorFolders()
        {
            //---Grabs all folder names and paths from /Image Downloads/ folder---
            FtpClient client = FtpHelper.CreateFilesFtpClient();
            ScrubberFolders vendorFolders = new ScrubberFolders();
            vendorFolders.VendorFolders = FtpHelper.GenerateVendor_PathDictionary(client);
            client.Disconnect();

            return new Http.Response<ScrubberFolders>()
            {
                StatusCode = 200,
                Status = "Ftp Connection Established",
                Data = vendorFolders
            };
        }

        public static Http.Response<FileHolder> ScrubVendor(ScrubberRequest request) 
        {
            //---Reads Data and Declares FileHolder object---
            DataTable excelData = ExcelHelper.ReadExcelData(request.File, "All Images");
            List<string> skus = GrabSkus(excelData);
            FileHolder resultFile;

            //---Scrubs through Exavault Via Sku or Upc, returns resulting file to FileHolder---
            if (request.IsSku) resultFile = ScrubFtpWithSkus(skus, request.VendorPath);
            else resultFile = ScrubFtpWithUpcs(skus, request.VendorPath);

            //---Returns Response Containing FileHolder, If Results Found---
            if(resultFile != null)
            {
                return new Http.Response<FileHolder>()
                {
                    StatusCode = 200,
                    Status = "OK",
                    Data = resultFile
                };
            }
            else  
            {
                return new Http.Response<FileHolder>()
                {
                    StatusCode = 404,
                    Status = "Not Found",
                    Data = null
                };
            }
        }

        private static FileHolder ScrubFtpWithSkus(List<string> skus, string path)
        {
            //---Scrubs Vendor Folder With Skus in Document---
            Dictionary<string, string> id_path = new Dictionary<string, string>();
            FtpClient client = FtpHelper.CreateFilesFtpClient();
            List<string> imageFiles = FtpHelper.GrabAllItemsInVendorFolder(client, path);

            //itterates through list grabbing all files that contain sku in name
            foreach (string sku in skus)
            {
                //Sets first result, if there is one, to value of dictionary with SKU as key
                IEnumerable<string> foundFilePaths = imageFiles.Where(x => x.Contains(sku));
                if (foundFilePaths.Count() > 0)
                {
                    id_path[sku] = $"https://flc.hosted-by-files.com{foundFilePaths.First()}";
                }
            }

            if (id_path.Count() > 0) return GenerateScrubberFile(path, true, id_path);
            else return null;
        }

        private static FileHolder ScrubFtpWithUpcs(List<string> skus, string path)
        {
            //---Converts list of Skus to Dictionary<sku, List<upcs>>, then Scrubs vendor folder with list of UPCs---
            Dictionary<string, string> id_path = new Dictionary<string, string>();
            FtpClient client = FtpHelper.CreateFilesFtpClient();
            List<string> imageFiles = FtpHelper.GrabAllItemsInVendorFolder(client, path);
            Dictionary<string, List<string>> sku_upcs = SqlHelper.ConvertSkusToUpcs(skus);
            
            //Itterates through each kvp to access list of upcs
            foreach (KeyValuePair<string, List<string>> sku_upc in sku_upcs)
            {
                //itterates through list of upcs
                foreach(string upc in sku_upc.Value)
                {
                    //if one of the upcs are contained in the files sku will be added as key
                    //first item path on list will be the value
                    IEnumerable<string> foundFilePaths = imageFiles.Where(x => x.Contains(upc));
                    if (foundFilePaths.Count() > 0)
                    {
                        id_path[sku_upc.Key] = $"https://flc.hosted-by-files.com{foundFilePaths.First()}";
                    }
                }
            }

            if (id_path.Count() > 0) return GenerateScrubberFile(path, false, id_path);
            else return null;
        }

        private static List<string> GrabSkus(DataTable table)
        {
            //---Takes data table from Excel file and generates list of SKUs from it---
            List<string> skus = new List<string>();

            int i = 0;
            foreach (DataRow row in table.Rows)
            {
                if (i > 0)
                {
                    object[] values = row.ItemArray;
                    string sku = values[3].ToString();
                    if (sku.Count() == 8) skus.Add(sku);
                }
                i++;
            }

            return skus;
        }

        private static FileHolder GenerateScrubberFile(string vendorPath, bool isSKu, Dictionary<string, string> id_url)
        {
            //---Creates FileHolder with relevant information---
            return new FileHolder()
            {
                FileName = GenerateFileName(vendorPath, isSKu),
                FileData = ExcelHelper.CreateExavaultSheet(id_url)
            };
        }

        private static string GenerateFileName(string vendorPath, bool isSku)
        {
            //---Formats name using vendor and a bool to determine if 'SKU' or 'UPC' should be in name---
            DateTime now = DateTime.Now;
            string formatTime = now.ToString("MM_dd_yyyy");
            string vendor = vendorPath;
            vendor = vendor.Substring(vendor.LastIndexOf("/") + 1);
            if (isSku)
            {
                return $"{vendor} - Sku Exavault results - {formatTime}.xlsx";
            }
            else
            {
                return $"{vendor} - Upc Exavault results - {formatTime}.xlsx";
            }
        }
    }
}
