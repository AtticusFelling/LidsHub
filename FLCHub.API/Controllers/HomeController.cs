using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using FLCHub.API.Services;
using FLCHub.Models.EdgarModels;
using System.IO.Compression;
using FLCHub.API.Services.It;
using FLCHub.API.Models;
using FLCHub.API.Service.Buyer;
using FLCHub.API.Services.Generic;
using static FLCHub.API.Models.Http;
using FLCHub.API.Services.Buyer;

namespace FLCHub.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class HomeController : Controller
    {
        private readonly ItToolService _service;
        private readonly string _FolderPath;

        public HomeController(ItToolService service)
        {
            _service = service;
            _FolderPath = Global.ProjectPath;
        }

        [HttpPost]
        [Route("BrandInsertion")]
        [EnableCors]
        public IActionResult GenerateBrandScript([FromBody] BrandModelRequest model)
        {
            return Content(_service.CreateBrandInsertSql(model.Model));
        }

        [HttpPost]
        [Route("test")]
        public IActionResult Test()
        {
            var response = new Http.Response<string>
            {
                Status = "success",
                Data = "Success!"
            };

            var jsonResponse = JsonConvert.SerializeObject(response);
            return Content(jsonResponse);
        }

        [HttpPost]
        [Route("getBuyerReferenceData")]
        public IActionResult GetBuyerReferenceData()
        {
            return Content(BuyerReferenceData.GetBuyerReferenceData());
        }

        [HttpPost]
        [Route("getEdgarSkus")]
        public IActionResult GetEdgarSkus()
        {
            var response = EdgarProcess.GetAllSkus();
            return Content(response);
        }

        [HttpPost]
        [Route("getEdgarFiles")]
        public IActionResult GetEdgarFiles([FromBody] EdgarFileRequest request)
        {
            var response = EdgarProcess.GetEdgarFiles(request.Skus);
            return Content(response);
        }

        [HttpPost]
        [Route("emailExtractor")]
        public IActionResult CombExEmails([FromBody] EmailItem request)
        {
            var excel = EmailExtractor.CombExEmails(request.Subject);
            return Content(excel);
        }

        [HttpPost]
        [Route("emailSubLister")]
        public IActionResult EmailSubList()
        {
            var response = EmailExtractor.EmailSubLister();
            return Content(response);
        }

        public class EdgarFileRequest
        {
            [JsonProperty("skus")]
            public string[] Skus { get; set; }
        }

        [HttpPost]
        [Route("VendorImageSubmission")]
        public async Task<IActionResult> VendorImageSubmission([FromBody] VendorImgDataModel model)
        {
            return Content(_service.CreateImageFiles(model.Model));
        }

        [HttpPost]
        [Route("getTable")]
        public IActionResult GetTable(ICOE.TableRequest request)
        {
            return Content(DBTable.GetDBTable(request));
        }

        [HttpPost]
        [Route("getTableNames")]
        public IActionResult GetTableNames()
        {
            return Content(DBTable.GetDBTableNames());
        }

        [HttpPost]
        [Route("saveTable")]
        public IActionResult SaveTable(ICOE.SaveTableRequest request)
        {
            return Content(DBTable.SaveDBTable(request));
        }

        [HttpPost]
        [Route("Scrubber/Folders")]
        public IActionResult GetVendorFolders()
        {
            return Content(_service.GetVendorFolders());
        }

        [HttpPost]
        [Route("Scrubber/ScrubVendor")]
        public IActionResult ScrubVendorFolder([FromBody] ScrubberRequestModel model)
        {
            return Content(_service.ScrubVendorFolder(model.Model));
        }

        [HttpPost]
        [Route("MissingImages")]
        public IActionResult CreateVendorLists([FromBody] MissingImgDataModel model)
        {
            return Content(_service.CreateVendorLists(model.Model));
        }

        //---Permissions Endpoints Here---
        //--Get Permission Endpoints--
        [HttpPost]
        [Route("GetAllPermissions")]
        public IActionResult GetAllPermissions()
        {
            return Content(Permission.GetAllPermissions());
        }

        [HttpPost]
        [Route("GetUserPermissions/{user}")]
        public IActionResult GetUserPermissions([FromRoute] string user)
        {
            var data = Permission.GetUserPermissions(user);
            if (data != "[]") return Content(data);
            else return BadRequest("Couldn't find user permissions");
        }

        [HttpPost]
        [Route("GetPagePermissions/{page}")]
        public IActionResult GetPagePermissions([FromRoute] string page)
        {
            var data = Permission.GetPagePermissions(page);
            if (data != "null") return Content(data);
            else return BadRequest("Couldn't find pages permissions");
        }

        //---Post Permission Endpoints---
        [HttpPost]
        [Route("AddUsersToPage")]
        public IActionResult AddUsersToPage([FromBody] PermissionWrapper model)
        {
            return Content(Permission.AddUsersToPage(model.Model));
        }

        [HttpPost]
        [Route("RemoveUsersFromPage")]
        public IActionResult RemoveUsersFromPage([FromBody] PermissionWrapper model)
        {
            return Content(Permission.RemoveUsersFromPage(model.Model));
        }

        [HttpPost]
        [Route("DownloadBWS")]
        public IActionResult DownloadBWS(BWS.BWSDownloadRequest request)
        {
            return Content(BWS.Download(request)); 
        }

        [HttpPost]
        [Route("GetBWSVersions")]
        public IActionResult GetBWSVersions()
        {
            return Content(BWS.GetVersions());
        }
    }
}
