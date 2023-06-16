using FLCHub.API.Models;
using FLCHub.Data;
using FLCHub.Models;
using FLCHub.Models.EdgarModels;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FLCHub.API.Services.Generic
{
    public static class SqlHelper
    {
        public static Http.Response<FileHolder> BrandMaker(BrandModel brand)
        {

            string query = GenerateBrandInsertSql(brand);
            FileHolder file =  new FileHolder
            {
                FileName = brand.Name + ".sql",
                FileData = SqlFileMaker(query)
            };

            return new Http.Response<FileHolder>()
            {
                Status = "OK",
                StatusCode = 200,
                Data = file
            };
        }

        public static Dictionary<string, List<string>> ConvertSkusToUpcs(List<string> skus)
        {
            Dictionary<string, List<string>> sku_upcs = new Dictionary<string, List<string>>();
            string query = "select SKU, u.Prefix + u.Number + u.CheckDigit from dbo.upccrossreference u " +
                $"where SKU in (";
            string connString = @"Data Source = PRDBMSDB; Initial Catalog = BMS; Integrated Security = True; TrustServerCertificate = True";
            query = SqlWhereInGenerator(skus, query);
            DataTable table = QueryHandler(query,connString);

            foreach (DataRow row in table.Rows)
            {
                object[] values = row.ItemArray;
                string sku = values[0].ToString();
                string upc = values[1].ToString();

                if (!sku_upcs.ContainsKey(sku))
                {
                    List<string> upcs = new List<string>();
                    upcs.Add(upc);
                    sku_upcs.Add(sku, upcs);
                }

                else sku_upcs[sku].Add(upc);
            }

            return sku_upcs;
        }

        public static Dictionary<string, string> ConvertUpcsToSkus(List<string> upcs)
        {
            //---Initializes Variable---
            Dictionary<string, string> dic = new Dictionary<string, string>();
            string connString = @"Data Source = PRDBMSDB; Initial Catalog = BMS; Integrated Security = True; TrustServerCertificate = True";
            string query = "select u.Prefix + u.Number + u.CheckDigit, SKU from dbo.upccrossreference u " +
                $"where u.Prefix + u.Number + u.CheckDigit in (";
            //Formats the rest of the query using List<string>
            query = SqlWhereInGenerator(upcs, query);

            //---Execute Command---
            //returns table recieved
            DataTable table = QueryHandler(query, connString);

            //---Populate Dictionaries---
            foreach (DataRow row in table.Rows)
            {
                //places Upc as key and sku as value
                object[] value = row.ItemArray;
                dic[value[0].ToString()] = value[1].ToString();
            }

            return dic;
        }

        public static string ConvertUpcToSku(string upc)
        {
            string connectionString = @"Data Source=PRDBMSDB;Initial Catalog=BMS;Integrated Security=True;TrustServerCertificate=True";
            string query = "select SKU from dbo.upccrossreference u " +
                $"where u.Prefix + u.Number + u.CheckDigit = '{upc}'";


            DataTable table = QueryHandler(query, connectionString);

            object[] value = table.Rows[0].ItemArray;
            return value[0].ToString();
        }

        public static DataTable GetQaData(Dictionary<string, string> dic)
        {
            //---Intialize query and execute---
            string connString = @"Data Source=PRDBMSDB;Initial Catalog=BMS;Integrated Security=True;TrustServerCertificate=True";
            string query =
                "SELECT M.SKU, M.ShortDescription, T.[Description] , M.ColorAbbr " +
                "FROM ProductMaster AS M " +
                "LEFT JOIN ProductTeams AS T ON M.TeamId = T.TeamID " +
                "WHERE M.SKU " +
                "IN (";
            //uses Dictionary to finish the query
            query = SqlWhereInGenerator(dic, query);
            //Creates table from results
            DataTable results =  QueryHandler(query, connString);

            //---Massages table for QA file---
            //adds column for Urls
            results.Columns.Add();
            //itterates through rows to add Urls
            foreach(DataRow row in results.Rows)
            {
                //sets Url column equal to the value of dictionary with sku as key
                string sku = row[0].ToString();
                row[3] = dic[sku];
            }

            return results;
        }

        private static string GenerateBrandInsertSql(BrandModel brand)
        {
            string query = "USE BMS \n" +
                $"select * from bms.dbo.D365_export_VendorInfo where VendorCode in ('{brand.VendorCode}'); \n" +
                "\n" + "-- released product vendor xref \n" +
                "BEGIN TRANSACTION; \n" + "\n" +
                "INSERT INTO dbo.D365_export_VendorInfo (Company, VendorCode, PSoftVendorId, VendorAccount, Name) \n" +
                $"VALUES ('{brand.Company}', '{brand.VendorCode}', '', '{brand.VendorAccount}', '{brand.Name}'); \n" +
                "\n" + $"select * from dbo.D365_export_VendorInfo where VendorCode in ('{brand.VendorCode}');" +
                "\n" + "-- Brand" + "\n" +
                $"select * from dbo.ProductBrands p where p.Brand in ( '{brand.Name}'); \n" +
                "INSERT INTO BMS.dbo.ProductBrands (BrandId, Brand, DateLastChanged) \n" +
                "select \n" + "max(p.brandid) + 1 as BrandId, \n" +
                $"'{brand.Name}' as Brand, \n" +
                "GETDATE() as Date \n" +
                "from BMS.dbo.productbrands p; \n" + "\n" +
                $"select * from dbo.ProductBrands p where p.Brand in ( '{brand.Name}'); \n" +
                "\n" + "ROLLBACK TRANSACTION; \n" + "COMMIT TRANSACTION;";

            return query;
        }

        private static byte[] SqlFileMaker(string query)
        {
            return Encoding.UTF8.GetBytes(query);
        }

        private static string SqlWhereInGenerator(List<string> items, string query)
        {
            foreach (var item in items) query += $"'{item}', ";
            query = query.Trim();
            query = query.Trim(',');
            query += " )";
            return query;
        }

        private static string SqlWhereInGenerator(Dictionary<string, string> items, string query)
        {
            foreach (var item in items) query += $"'{item.Key}', ";
            query = query.Trim();
            query = query.Trim(',');
            query += " )";
            return query;
        }

        private static DataTable QueryHandler(string query, string connString)
        {
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
            conn.Open();

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            conn.Close();
            da.Dispose();
            return dt;
        }
    }
}
