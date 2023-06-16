using ExcelDataReader;
using FLCHub.API.Services.Generic;
using FLCHub.Models.EdgarModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using FLCHub.Models.EdgarModels;
using Outlook = Microsoft.Office.Interop.Outlook;
using Newtonsoft.Json;
using FLCHub.API.Models;
using OfficeOpenXml.Drawing.Chart.ChartEx;
using System.Data.Entity.ModelConfiguration.Conventions;
using Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic;
using System.Xml.Linq;
using static Azure.Core.HttpHeader;
using System.Drawing;

namespace FLCHub.API.Services.Generic
{
    internal class EmailExtractor
    {
        public static string CombExEmails(string eml)
        {
            //Access Microsoft Outlook and filter into inbox then to do folder
            Outlook.Application outlookApp = new Outlook.Application();
            Outlook.NameSpace ns = outlookApp.GetNamespace("MAPI");
            Outlook.MAPIFolder inbox = ns.Folders["FLCImages"].Folders["Inbox"].Folders["To Do"];
            Outlook.Items items = inbox.Items;
            //Go through each email in the todo folder
            var files = new List<EmailFile>();
            foreach (Outlook.MailItem email in items)
            {                      
                //Filter out email based on entered subject, make sure the subject is flagged to do and not as completed
                if (email.Subject.Contains(eml) && email.FlagStatus == Outlook.OlFlagStatus.olFlagMarked && email.FlagStatus != Outlook.OlFlagStatus.olFlagComplete)
                {
                    //Loop though these filtered emails and check for attachments
                    for (int i = 1; i <= email.Attachments.Count; i++)
                    {
                        Outlook.Attachment attachment = email.Attachments[i];
                        if (attachment.Type == Outlook.OlAttachmentType.olByValue)
                        {
                            //Set a temp path with the attachment filename
                            var path = Path.GetTempPath() + $"{attachment.FileName}";
                            if (attachment.FileName.EndsWith(".csv"))
                            {                             
                            // check to see if its a csv file and set temp path replacing the csv with xlsx
                                var excelpath = Path.GetTempPath() + $"{attachment.FileName.Replace(".csv", ".xlsx")}";
                                var filen = attachment.FileName.Replace(".csv", ".xlsx");                             
                                attachment.SaveAsFile(excelpath);
                                //convert the excelpath that is a csv to xls
                                ExcelHelper.CsvToXlsx(excelpath, path);
                                var excelbytes = File.ReadAllBytes(excelpath);   
                                //add as a new EmailFile
                                files.Add(new EmailFile
                                {
                                    FileName = filen,
                                    FileData = excelbytes
                                });
                            }
                            //check to make sure the attachment is an xlsx file and add as a new EmailFile
                            if (attachment.FileName.EndsWith(".xlsx")) 
                            {
                                attachment.SaveAsFile(path);
                                var bytes = File.ReadAllBytes(path);
                                files.Add(new EmailFile
                                {
                                    FileName = attachment.FileName,
                                    FileData = bytes
                                });
                            }
                        }
                    }
                }
            }
            //List of the email files in an array
            var response = new Http.Response<EmailFile[]>
            {
                StatusCode = 200,
                Data = files.ToArray()
            };
            //converts the response to a json object and returns that response
            return JsonConvert.SerializeObject(response);
        }

        public static string EmailSubLister()
        {
            //Access Microsoft Outlook and filter into inbox then to do folder
            Outlook.Application outlookApp = new Outlook.Application();
            Outlook.NameSpace ns = outlookApp.GetNamespace("MAPI");
            Outlook.MAPIFolder inbox = ns.Folders["FLCImages"].Folders["Inbox"].Folders["To Do"];
            Outlook.Items items = inbox.Items;
            //Go through each email in the todo folder
            var subs = new List<String>();
            foreach (Outlook.MailItem email in items)
            {
                //Filter out email based on entered subject, make sure the subject is flagged to do and not as completed
                if (email.FlagStatus == Outlook.OlFlagStatus.olFlagMarked && email.FlagStatus != Outlook.OlFlagStatus.olFlagComplete)
                {
                    //Loop though these filtered emails and check for attachments
                    for (int i = 1; i <= email.Attachments.Count; i++)
                    {
                        Outlook.Attachment attachment = email.Attachments[i];
                        if (attachment.Type == Outlook.OlAttachmentType.olByValue)
                        {
                            var path = Path.GetTempPath() + $"{attachment.FileName}";
                            attachment.SaveAsFile(path);
                            var bytes = File.ReadAllBytes(path);
                            //check to make sure the attachment is an xlsx or csv file and then add subject to the list of subs
                            if (attachment.FileName.EndsWith(".xlsx") || attachment.FileName.EndsWith(".csv"))
                            {
                                subs.Add(
                                    email.Subject
                                );
                            }
                        }
                    }
                }
            }
            //List of email subjects in an array
            var response = new Http.Response<String[]>
            {
                StatusCode = 200,
                Data = subs.ToArray()
            };
            return JsonConvert.SerializeObject(response);
        }

        public class EmailSubjects
        {
            [JsonProperty("subjectName")]
            public string SubjectName { get; set; }
        }

        public class EmailFile
        {
            [JsonProperty("fileName")]
            public string FileName { get; set; }
            [JsonProperty("fileData")]
            public byte[] FileData { get; set; }
        }
    }
}