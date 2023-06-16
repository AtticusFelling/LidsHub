using ExcelDataReader;
using FLCHub.API.Services.Generic;
using FLCHub.Models.EdgarModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using FLCHub.API.Services.Generic;
using FLCHub.API.Models;
using System.Drawing;
using System.IO;

namespace FLCHub.API.Services.Generic
{
    internal class ExcelHelper
    {
        // Got this from stack overflow
        public static bool SaveAsCsv(string excelFilePath, string destinationCsvFilePath)
        {

            using (var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                IExcelDataReader reader = null;
                if (excelFilePath.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (excelFilePath.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }

                if (reader == null)
                    return false;

                var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = false
                    }
                });

                var csvContent = string.Empty;
                int row_no = 0;
                while (row_no < ds.Tables[0].Rows.Count)
                {
                    var arr = new List<string>();
                    for (int i = 0; i < ds.Tables[0].Columns.Count; i++)
                    {
                        arr.Add(ds.Tables[0].Rows[row_no][i].ToString());
                    }
                    row_no++;
                    csvContent += string.Join(",", arr) + "\n";
                }
                StreamWriter csv = new StreamWriter(destinationCsvFilePath, false);
                csv.Write(csvContent);
                csv.Close();
                return true;
            }
        }

        public static byte[] DownloadAsXlsx(List<string> skus)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage(new FileInfo("MyWorkbook.xlsx"));
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");
            worksheet.Row(1).Height = 20;
            worksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Style.Font.Bold = true;

            for (int i = 1; i <= skus.Count; i++)
            {
                var line = skus[i - 1].Split(',');
                for (int j = 1; j <= line.Length; j++)
                {
                    worksheet.Cells[i, j].Value = line[j - 1];
                }
            }

            // Write content to excel file 
            return package.GetAsByteArray();
        }

        public static DataTable ReadExcelData(FileHolder file)
        {
            if (Path.GetExtension(file.FileName) == ".xlsx")
            {
                Stream stream = new MemoryStream(file.FileData);
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                //Instance of Excel reader using stream instance
                IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                //Fills a data set with results from reader
                DataSet result = reader.AsDataSet();
                //Converts first sheet to data table
                DataTable table = result.Tables[0];

                //closes instance of Excel reader and file stream
                reader.Close();
                stream.Close();
                return table;
            }
            else return null;
        }

        public static DataTable ReadExcelData(FileHolder file, string sheet)
        {
            
                Stream stream = new MemoryStream(file.FileData);
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                //Instance of Excel reader using stream instance
                IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                //Fills a data set with results from reader
                DataSet result = reader.AsDataSet();
                //Converts first sheet to data table
                DataTable table = result.Tables[sheet];

                //closes instance of Excel reader and file stream
                reader.Close();
                stream.Close();
                return table;
            
            
        }

        public static byte[] CreateExavaultSheet(Dictionary<string, string> id_path)
        {
            //---Intial Settup Block---
            string[] headerText = { "ITEMID", "IMAGE", "IMAGE", "IMAGE" };
            MemoryStream memoryStream = new MemoryStream();
            ExcelPackage package = CreateExcelPackage(memoryStream);
            ExcelWorksheet ws = package.Workbook.Worksheets.Add("Results");
            
            //---Format and Data Population Block---
            FormatHeaders(ws, headerText, Color.Gray);
            ConvertDicToExcel(ws, id_path, 2);
            return FinalizeExcelPackage(memoryStream, package);
        }

        public static void CsvToXlsx(string xlsxPath, string csvPath)
        {
            var lines = File.ReadAllLines(csvPath).ToList();
            if (!xlsxPath.EndsWith(".xlsx"))
            {
                Console.WriteLine($"Path must be .xlsx file. {xlsxPath} not saved");
                return;
            }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage(new FileInfo("MyWorkbook.xlsx"));
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");
            worksheet.Row(1).Height = 20;
            worksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Style.Font.Bold = true;
            for (int i = 1; i <= lines.Count; i++)
            {
                var line = lines[i - 1].Split(',');
                for (int j = 1; j <= line.Length; j++)
                {
                    worksheet.Cells[i, j].Value = line[j - 1];
                }
            }
            if (File.Exists(xlsxPath))
                File.Delete(xlsxPath);
            // Create excel file on physical disk    
            FileStream objFileStrm = File.Create(xlsxPath);
            objFileStrm.Close();
            // Write content to excel file     
            File.WriteAllBytes(xlsxPath, package.GetAsByteArray());
            //Close Excel package      
            package.Dispose();
        }

        public static byte[] SubmissionSheet(System.Data.DataTable table)
        {
            MemoryStream memoryStream = new MemoryStream();
            ExcelPackage package = CreateExcelPackage(memoryStream);
            ExcelWorksheet ws = package.Workbook.Worksheets.Add("Submission");
            string[] headerText = { "ITEMID", "PRIMARY", "FRONT", "BACK" };

            FormatHeaders(ws, headerText, Color.LightGray);
            ConvertTableToExcel(ws, table, 2, 1);
            return FinalizeExcelPackage(memoryStream, package);
        }

        public static byte[] CreateQASheets(DataTable table)
        {
            MemoryStream memoryStream = new MemoryStream();
            ExcelPackage package = CreateExcelPackage(memoryStream);
            ExcelWorksheet ws = package.Workbook.Worksheets.Add("Q.A.");
            string[] headerText = { "Pass\r\n/\r\nFail", "FLC SKU", "ITEM DESCRIPTION", "School", "Image" };
            
            FormatHeaders(ws, headerText, Color.LightBlue);
            ws.Cells["B1"].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
            ConvertTableToExcel(ws, table, 2, 2);
            return FinalizeExcelPackage(memoryStream, package);
        }


        public static byte[] CreateReportSheets( System.Data.DataTable all, System.Data.DataTable school)
        {
            MemoryStream memoryStream = new MemoryStream();
            ExcelPackage package = CreateExcelPackage(memoryStream);
            AddTemplateSheet(package);
            ExcelWorksheet ws1 = package.Workbook.Worksheets.Add("School Specific");
            ExcelWorksheet ws = package.Workbook.Worksheets.Add("All Images");

            string[] headerText = { "LOC", "UPC", "ITEM DESCRIPTION", "FLC SKU", "BRAND", "PO#", "OH RETAIL", "OH UNITS" };

            if(school.Rows.Count > 0)
            {
                FormatHeaders(ws1, headerText, Color.LightGray);
                ConvertTableToExcel(ws1, school, 2, 1);
            }

            FormatHeaders(ws, headerText, Color.LightGray);
            ConvertTableToExcel(ws, all, 2, 1);
            return FinalizeExcelPackage(memoryStream, package);
        }
        

        public static string CreateExcelFiles(MemoryStream stream, string path)
        {
            File.Create(path);
            File.WriteAllBytes(path, stream.ToArray());
            return path;
        }

        public static FileInfo StreamFromFile()
        {
            FileInfo template = new FileInfo(@"Assets\ImgReportTemplate.xlsx");
            return template;
        }

        private static ExcelPackage CreateExcelPackage(MemoryStream memoryStream)
        {
            //---Creates a ready to use Excel Package---
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            return new ExcelPackage(memoryStream);
        }

        private static byte[] FinalizeExcelPackage(MemoryStream memoryStream, ExcelPackage package) 
        {
            //---Closes up stream and package, returning stream as byte[]---
            package.Save();
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();
            package.Dispose();
            return bytes;
        }

        private static void FormatHeaders(ExcelWorksheet ws, string[] headerText, Color color)
        {
            //---Basic Formatting for header---
            //Gets sheet to use, array for each header name, and a Color to make it
            int col = 1;
            foreach (string text in headerText)
            {
                ws.Cells[1, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[1, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[1, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws.Cells[1, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws.Cells[1, col].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws.Cells[1, col].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws.Cells[1, col].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws.Cells[1, col].Style.Font.Bold = true;
                ws.Cells[1, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, col].Style.Fill.BackgroundColor.SetColor(color);
                ws.Cells[1, col].Value = text;
                col++;
            }
            ws.Row(1).Height = 24;
        }

        private static void AddTemplateSheet(ExcelPackage package)
        {
            using (var sourcePackage = new ExcelPackage(StreamFromFile()))
            {
                var sourceWorksheet = sourcePackage.Workbook.Worksheets["Image Submission Steps"];

                var targetWorksheet = package.Workbook.Worksheets.Add("Image Submission Steps", sourceWorksheet);
            }
        }

        private static void ConvertTableToExcel(ExcelWorksheet ws, System.Data.DataTable table, int startRow, int startColunn)
        {
            //---Places DataTable in Excel sheet starting from given row and first column---
            int docRow = startRow;
            foreach (DataRow tableRow in table.Rows)
            {
                int docColumn = startColunn;
                object[] values = tableRow.ItemArray;
                foreach (object value in values)
                {
                    ws.Cells[docRow, docColumn].Value = value.ToString();
                    docColumn++;
                }
                docRow++;
            }
            ws.Columns.AutoFit();
        }

        private static void ConvertDicToExcel(ExcelWorksheet ws, Dictionary<string, string> id_path, int startRow)
        {
            //---Places Dictionary<string, string> in Excel sheet starting from given row and uses first two columns---
            int row = startRow;
            foreach (KeyValuePair<string, string> kvp in id_path)
            {
                ws.Cells[$"A{row}"].Value = kvp.Key;
                ws.Cells[$"B{row}"].Value = kvp.Value;
                row++;
            }
            ws.Columns.AutoFit();
        }
    }
}
