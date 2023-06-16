using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Data;
using FLCHub.Models.EdgarModels;
using FLCHub.API.Services.Generic;
using FLCHub.API.Models;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
using Http = FLCHub.API.Models.Http;

namespace FLCHub.API.Services.It
{
    public static class ImgDept
    {
        public static Http.Response<FileHolder[]> CreateImageFiles(VendorImgData model)
        {
            //---Read data from files, and combine it together---
            //Creates array of tables for each excel file, and array to hold generated files
            DataTable[] tables = new DataTable[model.Files.Count()];
            FileHolder[] newFiles = new FileHolder[2];
            //Itterates throgh each file, and places the resulting tables into array
            int i = 0;
            foreach (FileHolder file in model.Files)
            {
                tables[i] = ExcelHelper.ReadExcelData(file);
                i++;
            }
            //Combines all tables from the files into one
            DataTable results = CombineAllTables(tables, model);

            //---Convert UPCs to SKUs---
            //Grab all Upcs
            List<string> upcs = GrabUpcsFromColumn(results);
            if (upcs.Count > 0)
            {
                //Convert upcs to dictionary with Sku as key
                Dictionary<string, string> upc_skus = SqlHelper.ConvertUpcsToSkus(upcs);
                //Put skus into DataTable again
                ReturnSkusToTable(results, upc_skus);
            }

            foreach (DataRow row in results.Rows)
            {
                Console.WriteLine(row[0].ToString());
            }

            //---Generate Submission sheet---
            newFiles[0] = new FileHolder
            {
                FileName = GenerateName(false, model),
                FileData = ExcelHelper.SubmissionSheet(results)
            };

            //---Grab QA info from SKUs---
            //Create a dictionary from table
            Dictionary<string, string> sku_urls = CreateSku_UrlDictionary(results);
            DataTable QaTable = SqlHelper.GetQaData(sku_urls);

            //---Generate QA sheets---
            newFiles[1] = new FileHolder
            {
                FileName = GenerateName(true, model),
                FileData = ExcelHelper.CreateQASheets(QaTable)
            };

            return GenerateResponse(newFiles);
        }

        private static Http.Response<FileHolder[]> GenerateResponse(FileHolder[] files)
        {
            if (files != null)
            {
                return new Http.Response<FileHolder[]>()
                {
                    StatusCode = 200,
                    Status = "File Sent",
                    Data = files
                };
            }
            return null;
        }
        private static Dictionary<string, string> CreateSku_UrlDictionary(DataTable table)
        {
            //---Takes ItemId and Primary columns and converts each row key value pairs
            Dictionary<string, string> sku_urls = new Dictionary<string, string>();
            foreach (DataRow row in table.Rows)
            {
                //sets Sku as key and primary url as value
                sku_urls[row[0].ToString()] = row[1].ToString();
            }
            return sku_urls;
        }

        private static void ReturnSkusToTable(DataTable table, Dictionary<string, string> upc_skus)
        {
            //---Convert Upcs from table to Sku via dictionary---
            //Itterates through every row on the table to find upcs
            foreach (DataRow row in table.Rows)
            {
                //If ItemId = Upc, it will be changed to the Sku matching Upc key in Dictionary
                if (row[0].ToString().Count() == 12)
                {
                    if (upc_skus.ContainsKey(row[0].ToString()))
                    {
                        row[0] = upc_skus[row[0].ToString()];
                    }
                }
            }
        }

        private static List<string> GrabUpcsFromColumn(DataTable table)
        {
            //---Pull values from column in DataTable---
            List<string> upcs = new List<string>();
            //iterates through row pulling 
            foreach (DataRow row in table.Rows)
            {
                //checks to see if ItemId is Upc
                if (row[0].ToString().Count() == 12)
                {
                    upcs.Add(row[0].ToString());
                }
            }
            return upcs;
        }

        private static DataTable CombineAllTables(DataTable[] tables, VendorImgData model)
        {
            //--Combines array of tables into single table---
            DataTable results = new DataTable();

            bool first = true;
            //Itterate through all tables
            foreach (DataTable table in tables)
            {
                if (first)
                {
                    results = table.Clone();
                    foreach (DataRow row in table.Rows)
                    {
                        if (row[0].ToString().Count() == 8 || row[0].ToString().Count() == 12)
                        {
                            //Generate url based on if it has dropbox or a / in it
                            string url = row[1].ToString();
                            string baseurl = "https://flc.hosted-by-files.com";

                            if (row[1].ToString().Contains("dropbox") || row[1].ToString().Contains(baseurl)) { }
                            else if (row[1].ToString().Contains("/"))
                            {
                                url = url.Substring(url.LastIndexOf("/") + 1);
                                row[1] = $"{GenerateUrlName(model)}{url}";
                            }
                            else
                            {
                                row[1] = $"{GenerateUrlName(model)}{url}";
                            }
                            DataRow newRow = results.NewRow();
                            newRow.ItemArray = row.ItemArray;
                            results.Rows.Add(newRow);
                        }
                    }
                    first = false;
                }
                else
                {
                    foreach (DataRow row in table.Rows)
                    {
                        if (row[0].ToString().Count() == 8 || row[0].ToString().Count() == 12)
                        {
                            DataRow newRow = results.NewRow();
                            newRow.ItemArray = row.ItemArray;
                            results.Rows.Add(newRow);
                        }
                    }
                }
            }
            foreach (DataRow row in results.Rows)
            {
                object[] values = row.ItemArray;
                foreach (object value in values)
                {
                    Console.WriteLine(value.ToString());
                }
            }
            return results;
        }

        public static string GenerateName(bool isQa, VendorImgData upload)
        {
            //---Create unique file name based off model data---
            //create formatted timestamp
            DateTime date = DateTime.Now;
            string formatDate = date.ToString("MM-dd-yyyy");
            //adds "-QA" if isQa == true
            if (isQa) return $"{upload.TicketNumber}-QA {upload.Vendor} - {formatDate}.xlsx";
            return $"{upload.TicketNumber} - {upload.Vendor} - {formatDate}.xlsx";
        }

        public static string GenerateUrlName(VendorImgData upload)
        {
            //return a base url with a vendor the user choses
            return $"https://flc.hosted-by-files.com/Image%20Downloads/{upload.Vendor}/";
        }

       
    }
}
