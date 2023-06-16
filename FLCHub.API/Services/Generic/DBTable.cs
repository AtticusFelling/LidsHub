using System.Data;
using FLCHub.API.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.SqlClient;
using Microsoft.Office.Interop.Outlook;
using Newtonsoft.Json;
using System;

namespace FLCHub.API.Service.Buyer
{
    public static class DBTable
    {
        private static readonly string connString = @"Server=tcp:ssdevd365salesautomation.database.windows.net,1433;Authentication=Active Directory Integrated;Database=LidsRetailSupply;";
        //private static readonly string connString = @"";
        private static List<string> AccessibleTables = new List<string>
        {
            "B&N STORE KEY",
            "BMS Category",
            "BMS Departments",
            //"BMS SKU Upload",
            "BMS Subclass",
            //"BMS_DSW_Store_Xref$",
            //"BWS",
            //"BWS_Header",
            //"Core Approved POs",
            //"Export Worksheet",
            //"FLC ITems Created",
            "List_Table",
            //"Logo_List",
            //"Name AutoCorrect Save Failures",
            //"old B&N Store Key",
            "ProductLineHeadings",
            //"Source",
            "Vendor & Buyer Key"
        };

        // Gets and return table data based on the table name provided 
        public static string GetDBTable(ICOE.TableRequest request)
        {
            try
            {
                // Initialize custom table object
                var table = new ICOE.DbTable();

                var isSubTable = request.ParentTable != "na";

                // Select everything from table
                string query;

                if (request.ParentTable == "na") query = $"SELECT * FROM [{request.Name}]"; 
                else query = $"SELECT * FROM [{request.ParentTable}] WHERE {request.TableKey} = '{request.Name}'";

                // Establish db connection
                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
                conn.Open();

                // Create data adapter
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                // Query database and return the result to datatable
                var dt = new DataTable();
                da.Fill(dt);
                conn.Close();
                da.Dispose();

                // Sort datatable into list of headers and a list of items to format to vue front end table
                // Making headers which are just column names
                var headers = new List<ICOE.TableHeader>();
                foreach (DataColumn dc in dt.Columns)
                {
                    headers.Add(new ICOE.TableHeader
                    {
                        Text = dc.ColumnName,
                        Value = dc.ColumnName,
                        DataType = dc.DataType.Name,
                        Sortable = true
                    });
                }

                var rows = new List<Dictionary<string, string>>();

                foreach (DataRow dr in dt.Rows)
                {
                    var vals = new Dictionary<string, string>();
                    var i = 0;
                    foreach (var val in dr.ItemArray)
                    {
                        if (headers[i].DataType == "String")
                        {
                            vals[headers[i].Value] = val.ToString();
                        }
                        i++;
                    }

                    // Add an empty ections property (this will make a column for buttons on front end)
                    vals["actions"] = "";

                    // add primary key (I have this hardcoded as "ID" but ideally it needs to dynamically get primary key)
                    vals["ID"] = dr["ID"].ToString();
                    rows.Add(vals);
                }
                table.Items = rows.ToArray();
                // Remove all fields that aren't strings (will change eventually but this is to prevent errors)
                headers = headers.Where(h => h.DataType == "String").ToList();
                // Add action header
                headers.Add(new ICOE.TableHeader
                {
                    Text = "actions",
                    Value = "actions",
                    Sortable = false,
                    Align = "end"
                });
                table.Headers = headers.ToArray();
                table.IsSubTable = isSubTable;

                // Put custom table object in response object
                var response = new Http.Response<ICOE.DbTable>
                {
                    StatusCode = 201,
                    Data = table
                };

                // Convert to JSON
                var json = JsonConvert.SerializeObject(response);
                return json;
            }
            catch (System.Exception ex)
            {
                var response = new Http.Response<string>
                {
                    Data = ex.Message,
                    StatusCode = 500
                };
                return JsonConvert.SerializeObject(response);
            }
        }

        // Gets list of accessible table names to select from db
        public static string GetDBTableNames()
        {
            try 
            {
                // Connect to DB
                SqlConnection conn = new SqlConnection(connString);        
                conn.Open();
                // Get schema of db
                DataTable t = conn.GetSchema("Tables");
                var items = new List<ICOE.TableRequest>();
                // Add each table name to list
                foreach (DataRow dr in t.Rows)
                {
                    var tableName = dr["TABLE_NAME"].ToString();
                    if (AccessibleTables.Contains(tableName))
                    {
                        if (tableName == "List_Table") items.AddRange(GetSubTables("List_Table", "List_Title"));
                        else items.Add(new ICOE.TableRequest
                        {
                            Name = tableName,
                            ParentTable = "na",
                            TableKey = "na"
                        });
                    }
                }
                // Alphebetic order
                items = items.OrderBy(i => i.Name).ToList();

                conn.Close();
                
                // Return list in a response object
                var response = new Http.Response<ICOE.TableRequest[]>
                {
                    Data = items.ToArray(),
                    StatusCode = 200
                };
                return JsonConvert.SerializeObject(response);
            }
            catch (System.Exception ex) 
            {
                // Return error message if something goes wrong
                var response = new Http.Response<string>
                {
                    Data = ex.Message,
                    StatusCode = 500
                };
                return JsonConvert.SerializeObject(response);
            }
        }

        public static ICOE.TableRequest[] GetSubTables(string parentTable, string tableKey) 
        {
            // Select everything from table
            string query =
                $"SELECT DISTINCT {tableKey} FROM [{parentTable}]";

            // Establish db connection
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 300 };
            conn.Open();

            // Create data adapter
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            // Query database and return the result to datatable
            var dt = new DataTable();
            da.Fill(dt);
            conn.Close();
            da.Dispose();
            
            var lists = new List<ICOE.TableRequest>();
            foreach(DataRow row in dt.Rows)
            {
                lists.Add(new ICOE.TableRequest
                {
                    Name = row[tableKey].ToString(),
                    ParentTable = parentTable,
                    TableKey = tableKey
                });
            }
            return lists.ToArray();
        }
    
        public static string SaveDBTable(ICOE.SaveTableRequest saveTableRequest)
        {
            try
            {
                // Get table name and update items from request
                // Each update item specifies whether it is an CREATE, an UPDATE, or a DELETE
                // CREATEs contain fields and values
                // UPDATEs contain fields, values, and ID (primary key)
                // DELETEs contain just ID
                var tableName = saveTableRequest.TableName;
                var items = saveTableRequest.UpdateItems;
                var errors = new List<string>();

                SaveDbChange(saveTableRequest);

                // Connect to db
                SqlConnection conn = new SqlConnection(connString);
                conn.Open();

                // Sort items as to only deal with CREATEs
                foreach (var createItem in items.Where(i => i.Type == "CREATE"))
                {
                    try
                    {
                        // Put items in new dictionary so we can remove 'actions' and 'ID' items
                        var dic = new Dictionary<string, string>();
                        foreach (var item in createItem.Items)
                        {
                            if (item.Key != "actions" && item.Key != "ID") dic[item.Key] = item.Value;
                        }
                        createItem.Items = dic;

                        // Build Insert sql call
                        var addQuery = $"INSERT INTO [{saveTableRequest.TableName}] (";
                        foreach (var item in createItem.Items)
                        {
                            addQuery += "[" + item.Key + "],";
                        }
                        addQuery = addQuery.Trim(',');
                        addQuery += ") VALUES (";
                        foreach (var item in createItem.Items)
                        {
                            addQuery += "'" + item.Value.Replace("'", "").Replace("\\", "") + "',";
                        }
                        addQuery = addQuery.Trim(',');
                        addQuery += ")";

                        // Execute INSERT/CREATE
                        SqlCommand cmd = new SqlCommand(addQuery, conn) { CommandTimeout = 3000 };
                        cmd.ExecuteNonQuery();
                    }
                    catch (System.Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                }

                // Sort items as to only deal with UPDATEs
                foreach (var updateItem in items.Where(i => i.Type == "UPDATE"))
                {
                    try
                    {
                        // Build UPDATE sql call
                        var updateQuery = $"UPDATE [{saveTableRequest.TableName}] SET ";
                        foreach (var item in updateItem.Items)
                        {
                            if (item.Key != "ID" && item.Key != "actions") updateQuery += "[" + item.Key + "] = '" + item.Value + "',";
                        }
                        updateQuery = updateQuery.Trim(',');
                        updateQuery += " WHERE ID = '" + updateItem.Items["ID"] + "'";

                        //Exectute
                        SqlCommand cmd = new SqlCommand(updateQuery, conn) { CommandTimeout = 3000 };
                        cmd.ExecuteNonQuery();
                    }
                    catch (System.Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                }

                // Sort to do DELETEs
                foreach (var deleteItem in items.Where(i => i.Type == "DELETE"))
                {
                    try
                    {
                        // Build DELETE sql call
                        var deleteQuery = $"DELETE FROM [{saveTableRequest.TableName}] WHERE ";
                        deleteQuery += "ID = '" + deleteItem.Items["ID"] + "'";

                        // Execute
                        SqlCommand cmd = new SqlCommand(deleteQuery, conn) { CommandTimeout = 3000 };
                        cmd.ExecuteNonQuery();
                    }
                    catch (System.Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                }

                // Build response message
                // Array/list of errors and the amount of updates successful

                var responseMessage = new List<string> { (items.Count() - errors.Count()) + "/" + items.Count() + " rows successfully updated" };
                foreach (var error in errors)
                {
                    responseMessage.Add(error);
                }

                var response = new Http.Response<string[]>
                {
                    Data = responseMessage.ToArray()
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (System.Exception ex)
            {
                var response = new Http.Response<string>
                {
                    Data = ex.Message,
                    StatusCode = 500
                };
                return JsonConvert.SerializeObject(response);
            }
        }
    
        private static void SaveDbChange(ICOE.SaveTableRequest request) 
        {
            
            // Connect to db
            SqlConnection conn = new SqlConnection(connString);
            conn.Open();

            foreach (var item in request.UpdateItems)
            {
                var query = $"INSERT INTO [ICOE Changes] VALUES ('{item.Type}', '{request.User}', '{request.TableName}', '{item.Items["ID"]}', '{DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}', '')";
                SqlCommand cmd = new SqlCommand(query, conn) { CommandTimeout = 3000 };
                cmd.ExecuteNonQuery();
            }
        }
    }
}