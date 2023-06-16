using FLCHub.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace FLCHub.API.Services.Generic
{
    public static class Permission
    {
        //---Home of permissions json file---
        private static readonly string JsonPath = @"Assets\roles.json";

        //---Get Permission Methods---
        public static string GetAllPermissions()
        {
            var response = new Http.Response<PermissionList>()
            {
                StatusCode = 200,
                Status = "Permissions",
                Data = new PermissionList { Model = ReadAllPermissions() }
            };
            return JsonConvert.SerializeObject(response);
        }

        public static string GetUserPermissions(string name)
        {
            var response = new Http.Response<List<string>>()
            {
                StatusCode = 200,
                Status = "User Permissions",
                Data = ReadUserPermissions(name)
            };
            return JsonConvert.SerializeObject(response);
        }

        public static string GetPagePermissions(string name)
        {
            var response = new Http.Response<PermissionItem>()
            {
                StatusCode = 200,
                Status = "Page Permissions",
                Data = ReadPagePermissions(name)
            };
            return JsonConvert.SerializeObject(response);
        }

        //---Post Permission Methods---
        public static string AddUsersToPage(PermissionItem model)
        {
            int code;
            string status;

            if (WriteUsersToPage(model)) code = 200;
            else code = 400;
            if (code == 200) status = $"Users added to {model.Page}";
            else status = $"Users couldn't be added to {model.Page}";

            return JsonConvert.SerializeObject(FormatResponse(code, status));
        }

        public static string RemoveUsersFromPage(PermissionItem model)
        {
            int code;
            string status;

            if (EraseUsersFromPage(model)) code = 200;
            else code = 400;
            if (code == 200) status = $"Users removed from {model.Page}";
            else status = $"Users couldn't be removed from {model.Page}";

            return JsonConvert.SerializeObject(FormatResponse(code, status));
        }


        //---Private Get Methods---
        private static List<PermissionItem> ReadAllPermissions()
        {
            //Returns all of roles.json as List<PermissionItem> to easily manipulate data
            return JsonConvert.DeserializeObject<List<PermissionItem>>(File.ReadAllText(JsonPath));
        }

        private static List<string> ReadUserPermissions(string name)
        {
            //Reads roles.json, then finds all PermissionItems that contain 'name'
            IEnumerable<PermissionItem> pages = ReadAllPermissions();
            var userPages = pages.Where(x => x.Users.Contains(name));

            //Itterates through collected list: 'userPages'
            //Adds each page name to list
            List<string> results = new List<string>();
            foreach (var page in userPages)
            {
                results.Add(page.Page);
            }
            return results;
        }

        private static PermissionItem ReadPagePermissions(string name)
        {
            //Finds Page by name from roles.json and returns the users associated with the page
            IEnumerable<PermissionItem> pages = ReadAllPermissions();
            return pages.FirstOrDefault(x => x.Page == name);
        }

        //---Private Post Methods---
        private static bool WriteUsersToPage(PermissionItem model)
        {
            //Checks if model is valid
            if (model.Page != null && model.Users.Count > 0)
            {
                //Checks if page exists
                if (ReadPagePermissions(model.Page).Page != null)
                { //Within here it is assumed Page exists
                    var data = ReadAllPermissions();
                    UpdateRoles(model, data);
                    string newData = JsonConvert.SerializeObject(data);
                    return WriteToFile(newData);
                }
            }
            return false;
        }

        //Erasing users follows a very similar pattern
        private static bool EraseUsersFromPage(PermissionItem model)
        {
            //Model Validation
            if (model.Page != null && model.Users.Count > 0)
            {
                //Validating Existence of page
                if(ReadPagePermissions(model.Page).Page != null)
                { //^^Should Exist
                    var data = ReadAllPermissions();
                    DeleteRoles(model, data);
                    string newData = JsonConvert.SerializeObject(data);
                    return WriteToFile(newData);
                }
            }
            return false;
        }

        private static bool WriteToFile(string text)
        {
            try
            { //Determines whether save was successful
                File.WriteAllText(JsonPath, text);
                return true;
            }
            catch { return false; }
        }

        //If data doesn't contain a user, add that user
        private static void UpdateRoles(PermissionItem model, List<PermissionItem> data)
        {
            //Grabs index for page
            int index = data.FindIndex(x => x.Page == model.Page);
            foreach (string user in model.Users)
            { //Checks if user exists in specified PermissionItem.Users
                if (!data[index].Users.Contains(user))
                { //Adds if user missing
                    data[index].Users.Add(user);
                }
            }
        }
        //If data contains a user, remove that user
        private static void DeleteRoles(PermissionItem model, List<PermissionItem> data)
        {
            int index = data.FindIndex(x => x.Page == model.Page);
            foreach (string user in model.Users)
            {
                if (data[index].Users.Contains(user))
                {
                    data[index].Users.Remove(user);
                }
            }
        }

        //---Helpful Response messages---
        private static Http.Response<string> FormatResponse(int code, string status)
        {
            return new Http.Response<string>()
            {
                Status = status,
                StatusCode = code,
                Data = $"{code}"
            };
        }

    }
    //---Useful structures for Permissions---
    //Used for deserialization of Json file
    public struct PermissionItem
    {
        [JsonProperty("page")]
        public string Page { get; set; }

        // '?' allows for it to be null (I think)
        [JsonProperty("open")]
        public bool? Open { get; set; }
        [JsonProperty("users")]
        public List<string> Users { get; set; }
    }

    //Used as wrapper for client
    public struct PermissionWrapper
    {
        [JsonProperty("model")]
        public PermissionItem Model { get; set; }
    }

    public struct PermissionList
    {
        [JsonProperty("model")]
        public List<PermissionItem> Model { get; set; }
    }
}
