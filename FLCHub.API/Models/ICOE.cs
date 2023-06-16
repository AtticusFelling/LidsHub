using Newtonsoft.Json;

namespace FLCHub.API.Models
{
    public class ICOE
    {
        public class DbTable 
        {
            [JsonProperty("headers")]
            public TableHeader[] Headers {get;set;}
            [JsonProperty("items")]
            public Dictionary<string, string>[] Items {get;set;}
            [JsonProperty("subTable")]
            public bool IsSubTable {get;set;}
        }

        public class TableHeader
        {
            [JsonProperty("text")]
            public string Text {get;set;}
            [JsonProperty("value")]
            public string Value {get;set;}
            [JsonProperty("sortable")]
            public bool Sortable {get;set;}
            [JsonProperty("align")]
            public string Align {get;set;}
            [JsonProperty("dataType")]
            public string DataType {get;set;}
        }

        public class TableRequest
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("parentTable")]
            public string ParentTable { get; set; }
            [JsonProperty("tableKey")]
            public string TableKey { get; set; }
        }

        public class SaveTableRequest
        {
            [JsonProperty("tableName")]
            public string TableName {get;set;}
            [JsonProperty("updateItems")]
            public UpdateItem[] UpdateItems {get;set;}
            [JsonProperty("user")]
            public string User{get;set;}
        }

        public class UpdateItem
        {
            [JsonProperty("type")]
            public string Type {get;set;}
            [JsonProperty("items")]
            public Dictionary<string, string> Items {get;set;}
        }
    }
}