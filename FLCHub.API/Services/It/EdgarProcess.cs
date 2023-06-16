using System.Data;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using FLCHub.API.Services.Generic;
using FLCHub.API.Models;

namespace FLCHub.API.Services.It
{
    public class EdgarProcess
    {
        private static List<string> _skus = new List<string>();
        private static List<string> _skusSeperated = new List<string>();
        private string Folder900;
        private string EdgarFolder;
        private static string SkusCsvPath;
        private string ProcessingFolder;
        private string CompletedFolder;
        private static string ReleasedCsvPath;
        private static string VariantsCsvPath;
        private static string BarcodesCsvPath;
        private static string ExtrasCsvPath;
        private static string ExtrasExtrasCsvPath;
        private static string _timeStamp;
        private static string _currentDayFolder;

        public static string GetAllSkus()
        {
            // Get spreadsheets in 900 folder
            var files = Directory.GetFiles($@"C:\Users\{Environment.UserName}\OneDrive - Hat World, Inc\Company 900\Processing");
            //var files = Directory.GetFiles(Folder900);
            var sheets = new List<string>();
            foreach (var file in files)
            {
                if (file.EndsWith(".xlsx")) sheets.Add(file);
            }

            // For each sheet, get skus
            foreach (var sheetPath in sheets)
            {
                var csvPath = sheetPath.Replace(".xlsx", ".csv");
                ExcelHelper.SaveAsCsv(sheetPath, csvPath);
                GetSkus(csvPath);
                try
                {
                    File.Delete(csvPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // Remove duplicates from skus
            _skus = _skus.Distinct().ToList();

            var response = new Http.Response<EdgarSkusResponse>
            {
                Data = new EdgarSkusResponse
                {
                    Skus = _skus.ToArray(),
                    SeperatedSkus = _skusSeperated.Distinct().ToArray()
                },
                StatusCode = 200
            };

            return JsonConvert.SerializeObject(response);
        }

        private static void GetSkus(string path)
        {
            // Read spreadsheet
            var lines = File.ReadAllLines(path);
            // If skus list will be seperated (not compiled) add file name to seperate skus by file
            if (path.Contains(@"\Company 900\")) path = path.Substring(path.IndexOf(@"900\") + 4);
            _skusSeperated.Add(path.Replace(",", ""));

            // For each line, grab 1st value (sku)
            foreach (var line in lines)
            {
                var values = line.Split(",");
                var sku = values[0];
                bool validSku = sku.Length > 4;
                if (validSku)
                {
                    _skus.Add(sku);
                    if (!_skusSeperated.Contains(sku)) _skusSeperated.Add(sku);
                }
            }
        }

        public static string GetEdgarFiles(string[] skus)
        {
            // Generate time stamp for file names
            _timeStamp = DateTime.Now.Hour.ToString() + "_" + DateTime.Now.Minute.ToString() + "_" + DateTime.Now.Second.ToString() + "_";
            // Truncate table and upload skus to skus_for_900 table
            TruncateTable();
            _skus = skus.ToList();
            _skus = skus.ToList();
            InsertSkusDB();

            var edgarFiles = new List<EdgarFile>();
            edgarFiles.Add(RunReleased());
            edgarFiles.Add(RunVariants());
            edgarFiles.Add(RunBarCodes());
            edgarFiles.AddRange(RunExtras());
            edgarFiles.Add(RunExtrasExtras());
            // edgarFiles.Add(new EdgarFile { FileData = Convert.ToBase64String(File.ReadAllBytes(@"C:\Lids\Errors.xlsx")), FileName = "test" });
            var httpResponse = new Http.Response<EdgarFile[]>
            {
                Data = edgarFiles.ToArray(),
                StatusCode = 200
            };
            return JsonConvert.SerializeObject(httpResponse);
        }

        private static void InsertSkusDB()
        {
            // Connect to DB
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=DBA;Integrated Security=True;TrustServerCertificate=True";

            SqlConnection conn = new SqlConnection(connString);
            conn.Open();
            foreach (var sku in _skus)
            {
                string sqlCall =
                "INSERT INTO dbo.SKUs_for_900 " +
                $"VALUES ('{sku}')";

                SqlCommand cmd = new SqlCommand(sqlCall, conn);
                cmd.ExecuteNonQuery();
            }

            conn.Close();
        }

        private static void TruncateTable()
        {
            // Connect to DB
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=DBA;Integrated Security=True;TrustServerCertificate=True";
            // Query executing stored procedure
            string truncateSqlCall =
                "INSERT INTO dbo.SKUs_for_900_archive(sku, DATE) " +
                "SELECT sku, GETDATE() " +
                "FROM dbo.SKUs_for_900 " +
                "SELECT * FROM dbo.SKUs_for_900_archive " +
                "TRUNCATE TABLE dbo.SKUs_for_900 " +
                "SELECT * FROM dbo.SKUs_for_900";

            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(truncateSqlCall, conn) { CommandTimeout = 3000 };
            conn.Open();

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private static EdgarFile RunReleased()
        {
            // Connect to DB
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=DBA;Integrated Security=True;TrustServerCertificate=True";
            // Query executing stored procedure
            string query =
                "DECLARE @return_value int EXEC @return_value = [dbo].[D365_export_ReleasedProductsV2_900]" +
                "SELECT 'Return Value' = @return_value";

            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
            conn.Open();

            // Create data adapter
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            // Query database and return the result to datatable
            var dt = new DataTable();
            Console.WriteLine("Running Released...");
            da.Fill(dt);
            conn.Close();
            da.Dispose();
            // Save as csv
            return new EdgarFile { FileData = DownloadDtAsXlsx(dt), FileName = "Released" };
        }

        private static EdgarFile RunVariants()
        {
            // Connect to DB
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=DBA;Integrated Security=True;TrustServerCertificate=True";
            // Query executing stored procedure
            string query =
                "DECLARE @return_value int EXEC @return_value = [dbo].[D365_export_ReleasedProductVariants_900]" +
                "SELECT 'Return Value' = @return_value";

            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
            conn.Open();

            // Create data adapter
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            // Query database and return the result to datatable
            var dt = new DataTable();
            Console.WriteLine("Running Variants...");
            da.Fill(dt);
            conn.Close();
            da.Dispose();
            return new EdgarFile { FileData = DownloadDtAsXlsx(dt), FileName = "Variants" };
        }

        private static EdgarFile RunBarCodes()
        {
            // Connect to DB
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=DBA;Integrated Security=True;TrustServerCertificate=True";
            // Query executing stored procedure
            string query =
                "DECLARE @return_value int EXEC @return_value = [dbo].[D365_export_ItemBarCodes_900]" +
                "SELECT 'Return Value' = @return_value";

            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
            conn.Open();

            // Create data adapter
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            // Query database and return the result to datatable
            var dt = new DataTable();
            Console.WriteLine("Running Bar Codes...");
            da.Fill(dt);
            conn.Close();
            da.Dispose();
            return new EdgarFile { FileData = DownloadDtAsXlsx(dt), FileName = "BarCodes" };
        }

        private static EdgarFile[] RunExtras()
        {
            var edgarFiles = new List<EdgarFile>();
            // Connect to DB
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=DBA;Integrated Security=True;TrustServerCertificate=True";
            // Query executing stored procedure
            string query =
                "DECLARE @return_value int EXEC @return_value = [dbo].[D365_export_ReleasedProductsV2extra_900]" +
                "SELECT 'Return Value' = @return_value";

            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
            conn.Open();

            // Create data adapter
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            // Query database and return the result to datatable
            var dt = new DataTable();
            Console.WriteLine("Running Extras...");
            da.Fill(dt);
            conn.Close();
            da.Dispose();

            // This will store every extras sheet csv
            var extras = new List<ExtrasRow>();

            foreach (DataRow dr in dt.Rows)
            {
                var extrasRow = new ExtrasRow
                {
                    ITEMID = dr["ITEMID"].ToString(),
                    BNEDClassId = dr["BNEDClassId"].ToString(),
                    BNEDDeptId = dr["BNEDDeptId"].ToString(),
                    BNEDSubClassId = dr["BNEDSubClassId"].ToString(),
                    MSPR = dr["MSPR"].ToString()
                };
                extras.Add(extrasRow);
            }

            var extrasSheets = OrganizeExtras(extras);

            int i = 0;
            foreach (var sheet in extrasSheets)
            {
                var csv = new List<string> { "ITEMID,BNEDClassId,BNEDDeptId,BNEDSubClassId,MSPR" };
                foreach (var row in sheet)
                {
                    csv.Add(row.ITEMID + "," + row.BNEDClassId + "," + row.BNEDDeptId + "," + row.BNEDSubClassId + "," + row.MSPR);
                }
                //Save as csv
                //File.WriteAllLines(ExtrasCsvPath.Replace(".csv", _timeStamp + i + ".csv"), csv);
                //Save as xlsx
                edgarFiles.Add(new EdgarFile { FileData = Convert.ToBase64String(ExcelHelper.DownloadAsXlsx(csv)), FileName = $"Extras_{i}" });
                i++;
            }

            return edgarFiles.ToArray();
        }

        private static List<List<ExtrasRow>> OrganizeExtras(List<ExtrasRow> allExtrasSheet)
        {
            var seperatedExtras = new List<List<ExtrasRow>>();
            foreach (var row in allExtrasSheet)
            {
                bool rowAdded = false;
                foreach (var seperateSheet in seperatedExtras)
                {
                    // If sheet does not contains item id, add row
                    if (seperateSheet.Count(sr => sr.ITEMID == row.ITEMID) < 1)
                    {
                        seperateSheet.Add(row);
                        rowAdded = true;
                        break;
                    }
                }
                // If row wasn't added, create new sheet and add row to sheet
                if (!rowAdded) seperatedExtras.Add(new List<ExtrasRow> { row });
            }
            return seperatedExtras;
        }

        private static EdgarFile RunExtrasExtras()
        {
            // Connect to DB
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=DBA;Integrated Security=True;TrustServerCertificate=True";
            // Query executing stored procedure
            string query =
                "select " +
                "p.sku as ITEMNUMBER," +
                "Case when gendertypeid = 'U' then 'Male'" +
                "when gendertypeid = 'W' Then 'Female'" +
                "else 'Male' END as BNEDGENDER," +
                "'Baseball' as BNEDSPORT," +
                "Case when p.AgeRange is null Then 'N/A'" +
                "else p.AgeRange END as AGG," +
                "'N/A' as BT1," +
                "'N/A' as BT2," +
                "'N/A' as BT3," +
                "'N/A' as BT4," +
                "'N/A' as BT5," +
                "'N/A' as PRIME " +
                "from BMS.dbo.productmaster p join DBA.dbo.skus_for_900 s on p.sku = s.sku";

            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
            conn.Open();

            // Create data adapter
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            // Query database and return the result to datatable
            var dt = new DataTable();
            Console.WriteLine("Running Extras Extras...");
            da.Fill(dt);
            conn.Close();
            da.Dispose();
            return new EdgarFile { FileData = DownloadDtAsXlsx(dt), FileName = "ExtrasExtras" };
        }


        public class EdgarFile
        {
            [JsonProperty("fileName")]
            public string FileName { get; set; }
            [JsonProperty("fileData")]
            public string FileData { get; set; }
        }


        public class EdgarSkusResponse
        {
            [JsonProperty("skus")]
            public string[] Skus { get; set; }
            [JsonProperty("seperatedSkus")]
            public string[] SeperatedSkus { get; set; }
        }


        public class ExtrasRow
        {
            public string ITEMID { get; set; }
            public string BNEDClassId { get; set; }
            public string BNEDDeptId { get; set; }
            public string BNEDSubClassId { get; set; }
            public string MSPR { get; set; }
        }

        private static string DownloadDtAsXlsx(DataTable dt)
        {
            // Save as csv
            // Build csv as list
            var csv = new List<string>();
            var line = "";
            foreach (DataColumn dc in dt.Columns)
            {
                line += dc.ColumnName.ToString() + ",";
            }
            line = line.Trim(',');
            csv.Add(line);
            foreach (DataRow dr in dt.Rows)
            {
                line = "";
                foreach (var val in dr.ItemArray)
                {
                    // Check for null
                    line += val.ToString() + ",";
                }
                line = line.Trim(',');
                csv.Add(line);
            }

            // Export to xlsx
            return Convert.ToBase64String(ExcelHelper.DownloadAsXlsx(csv));
        }
    }
}