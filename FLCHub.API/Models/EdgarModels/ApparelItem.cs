using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FLCHub.Models.EdgarModels
{
    public class ApparelItem
    {
        public string ItemId { get; set; }
        public string Primary { get; set; }
        public string Front { get; set; }
        public string Back { get; set; }

        public void CreateProperLinks(string baseUrl)
        {
            if (Primary != "")
            {
                if (Primary.Contains("/"))
                {
                    int startIndex = Primary.LastIndexOf("/") + 1;
                    Primary = $"{baseUrl}/{Primary.Substring(startIndex)}";
                }
                else Primary = $"{baseUrl}/{Primary}";
            }


            if (Front != "")
            {
                if (Front.Contains("/"))
                {
                    int startIndex = Front.LastIndexOf("/") + 1;
                    Front = $"{baseUrl}/{Front.Substring(startIndex)}";
                }
                else Front = $"{baseUrl}/{Front}";
            }


            if (Back != "")
            {
                if (Back.Contains("/"))
                {
                    int startIndex = Back.LastIndexOf("/") + 1;
                    Back = $"{baseUrl}/{Back.Substring(startIndex)}";
                }
                else Back = $"{baseUrl}/{Back}";
            }

        }
    }
}
