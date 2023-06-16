using ExcelDataReader;
using FLCHub.API.Models;
using FLCHub.API.Services.Generic;
using System.Data;
using System.IO.Compression;

namespace FLCHub.API.Services.It
{
    public class MissingImg
    {
        public static Http.Response<FileHolder> CreateVendorLists(MissingImgData data)
        {
            //---Creates table from missing image report and formats it---
            DataTable imgReportTable = ExcelHelper.ReadExcelData(data.ImgReport[0], "Logo");
            DataTable formatReportTable = FormatTableFromReport(imgReportTable);

            //---Collect POs, add to formatted table, remove rows from table with no POs---
            AddPOsToTable(formatReportTable, data.PoReport[0]);
            RemoveRowsWithNoPOs(formatReportTable);

            //---Creates one table for each vendor from formatted table---
            Dictionary<string, DataTable> vendor_table = GenerateVendorTables(formatReportTable);
            CombineHanesTables(vendor_table);

            //---Creates files, then stores them into a zipFolder---
            List<FileHolder> files = CreateVendorSheets(vendor_table, data.schools);
            FileHolder zip = CreateZipFile(files);

            return new Http.Response<FileHolder>()
            {
                StatusCode = 200,
                Status = "OK",
                Data = zip
            };
        }

        private static DataTable FormatTableFromReport(DataTable table)
        {
            DataTable result = new DataTable();
            for (int i = 0; i < 8; i++) result.Columns.Add();

            //Loops through all rows and grabs relevant columns for each row
            //Also filters relevant rows by making sure column "AD" contains "FLC"
            foreach (DataRow row in table.Rows)
            {
                if (row[29].ToString().Contains("FLC"))
                {
                    DataRow dataRow = result.NewRow();
                    for (int j = 0; j <= table.Columns.Count; j++)
                    {
                        switch (j)
                        {
                            case 0:
                                dataRow[0] = row[j].ToString();
                                break;
                            case 3:
                                dataRow[1] = row[j].ToString();
                                break;
                            case 5:
                                dataRow[2] = row[j].ToString();
                                break;
                            case 22:
                                dataRow[3] = row[j].ToString();
                                break;
                            case 23:
                                dataRow[4] = row[j];
                                break;
                            case 27:
                                dataRow[6] = row[j];
                                break;
                            case 28:
                                dataRow[7] = row[j].ToString();
                                break;
                            default:
                                break;
                        }
                    }
                    result.Rows.Add(dataRow);
                }
            }
            return result;
        }

        private static void AddPOsToTable(DataTable imgReportTable, FileHolder poReport)
        {
            Dictionary<string, string> sku_po = new Dictionary<string, string>();
            DataTable poData = ExcelHelper.ReadExcelData(poReport);

            //Grabs POs and SKUs from the table into a dictionary
            foreach (DataRow row in poData.Rows)
            {
                string sku = row[0].ToString();
                string po = row[1].ToString();
                if (po != "")
                {
                    sku_po[sku] = po;
                }
            }

            //Itterates through missing img table associating the skus with their proper POs
            foreach (DataRow row in imgReportTable.Rows)
            {
                string sku = row[3].ToString();
                if (sku_po.ContainsKey(sku))
                {
                    row[5] = sku_po[sku];
                }
            }
        }

        private static void RemoveRowsWithNoPOs(DataTable imgReportTable)
        {
            List<DataRow> rows = new List<DataRow>();
            //Itterates through table collecting list of rows with no POs
            foreach (DataRow row in imgReportTable.Rows)
            {
                if (row[5].ToString().Trim() == "")
                {
                    rows.Add(row);
                }
            }
            //Itterates through list of rows to completely remove them from the table
            foreach (DataRow row in rows)
            {
                imgReportTable.Rows.Remove(row);
            }
        }

        private static Dictionary<string, DataTable> GenerateVendorTables(DataTable imgReportTable)
        {
            //Turns DataTable into an enumerable
            //Group by creates a kvp with each unique word being key, and all rows with that key being the values 
            //^^^^ In this case values are DataRows ^^^^
            //Adds each group using the key as key and rows as values for DataTable

            Dictionary<string, DataTable> vendorTables = imgReportTable.AsEnumerable()
                .GroupBy(row => row.Field<string>(4))
                .ToDictionary(group => group.Key, group => group.CopyToDataTable());

            return vendorTables;
        }

        private static void CombineHanesTables(Dictionary<string, DataTable> vendorTables)
        {
            //---Goes through all vendor tables and combines Hanes' brands into one---
            string[] hanesBrands = { "Champion", "Under Armour", "Alternative Apparel", "Gear For Sports" };
            DataTable hanesTable = new DataTable();
            bool made = false;

            foreach (string brand in hanesBrands)
            { //if first to hit, create table by copying vendor table
                if (vendorTables.ContainsKey(brand) && made == false)
                {
                    hanesTable = vendorTables[brand].Copy();
                    vendorTables.Remove(brand);
                    made = true;
                }
                else if (vendorTables.ContainsKey(brand))
                { //if tables already built, itterate through and add vendor to Hanes table
                    foreach (DataRow row in vendorTables[brand].Rows)
                    {
                        DataRow newRow = hanesTable.NewRow();
                        newRow.ItemArray = row.ItemArray;
                        hanesTable.Rows.Add(newRow);
                        vendorTables.Remove(brand);
                    }
                }
            }

            vendorTables["Hanes"] = hanesTable;
        }

        private static List<FileHolder> CreateVendorSheets(Dictionary<string, DataTable> vendorTables, List<string> schools)
        {
            List<FileHolder> files = new List<FileHolder>();

            DateTime now = DateTime.Now;
            string formatTime = now.ToString("MM-dd-yy");

            foreach (KeyValuePair<string, DataTable> pair in vendorTables)
            {
                string fileName;
                //Formats name to be whatever key to dictionary is
                if (pair.Key != "N/A")
                {
                    fileName = $"{pair.Key} Missing Images {formatTime}.xlsx";
                }
                else
                { //if there was no vendor and the table's key is "N/A"
                    fileName = $"No Name Missing Images {formatTime}.xlsx";
                }

                pair.Value.Rows.Add();
                int rowIndex = pair.Value.Rows.Count - 1;

                //Grabs totals for Retail OH and OH total
                double retailTotal = GetTotalFromColumn(pair.Value, 6, rowIndex);
                double ohTotal = GetTotalFromColumn(pair.Value, 7, rowIndex);
                //Formats table
                AddDollarSignToColumn(pair.Value, 6);
                //Adds school specific table
                DataTable schoolSpecific = GetSchoolSpecificTable(pair.Value, schools);

                files.Add(new FileHolder()
                {
                    FileData = ExcelHelper.CreateReportSheets(pair.Value, schoolSpecific),
                    FileName = fileName
                });
            }

            return files;
        }

        private static double GetTotalFromColumn(DataTable table, int column, int rowIndex)
        {
            double[] numbers = new double[table.Rows.Count];
            double total = 0;

            int i = 0;
            foreach (DataRow row in table.Rows)
            {
                //Loops through each row grabbing specified column to convert to double
                string value = row[column].ToString();
                if (double.TryParse(value, out numbers[i]))
                { 
                    numbers[i] = Math.Round(numbers[i], 2);
                    row[column] = numbers[i];
                }
                else
                {
                }
                i++;
            }

            foreach (double number in numbers)
            {
                total += number;
            }

            //Rounds total to the nearest 
            total = Math.Round(total, 2);
            object[] values = table.Rows[rowIndex].ItemArray;
            values[column] = total.ToString();
            table.Rows[rowIndex].ItemArray = values;

            return total;
        }

        private static void AddDollarSignToColumn(DataTable table, int col)
        {
            foreach (DataRow row in table.Rows)
            {
                string value = row[col].ToString();
                row[col] = $"${value}";
            }
        }

        private static DataTable GetSchoolSpecificTable(DataTable table, List<string> schools)
        {
            DataTable schoolTable = new DataTable();
            //Gives school specific table same amount of columns as master table
            foreach (DataColumn column in table.Columns)
            {
                schoolTable.Columns.Add();
            }
            //checks to see if row contains school # being tracked
            //if so adds to school specific table
            foreach (DataRow row in table.Rows)
            {
                string location = row[0].ToString();
                foreach (string school in schools)
                {
                    if (school == location)
                    {
                        object[] values = row.ItemArray;
                        schoolTable.Rows.Add(values);
                    }
                }
            }
            return schoolTable;
        }

        private static FileHolder CreateZipFile(List<FileHolder> files)
        {
            //Creates memory stream for Zip folder
            using (MemoryStream zipStream = new MemoryStream())
            {
                //Creates zip folder object instance
                using (ZipArchive zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    //Itterates through all the files and adds them into the zip array buffer
                    foreach (FileHolder file in files)
                    {
                        var entry = zipArchive.CreateEntry(file.FileName);
                        using (var entryStream = entry.Open())
                        {
                            entryStream.Write(file.FileData, 0, file.FileData.Length);
                        }
                    }
                }

                //returns zip folder in a file holder format
                return new FileHolder()
                {
                    FileName = "Missing Vendor Lists.zip",
                    FileData = zipStream.ToArray()
                };
            }
        }
    }
}
