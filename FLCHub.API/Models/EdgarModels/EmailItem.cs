using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FLCHub.Models.EdgarModels
{
    public class EmailItem
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }
    }
}
