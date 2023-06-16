using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLCHub.API.Services.Generic
{
    public static class CsvHelper
    {
        public static DataTable ReadCsvData(IFormFile file)
        {
            MemoryStream stream = new MemoryStream();
            file.OpenReadStream().CopyTo(stream);
            IExcelDataReader reader = ExcelReaderFactory.CreateCsvReader(stream);

            DataSet result = reader.AsDataSet();
            DataTable table = result.Tables[1];

            reader.Close();
            stream.Close();

            return table;
        }
    }
}
