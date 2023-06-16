using FLCHub.API.Models;
using FLCHub.API.Services.Generic;
using FLCHub.Data;
using FLCHub.Models.EdgarModels;
using Newtonsoft.Json;

namespace FLCHub.API.Services.It
{
    public class ItToolService
    {
        private readonly ApplicationDbContext _ctx;
        public ItToolService(ApplicationDbContext ctx) { _ctx = ctx; }

        public string CreateImageFiles(VendorImgData model)
        { return JsonConvert.SerializeObject(ImgDept.CreateImageFiles(model)); }

        public string GenereateImgNames(bool isQa, VendorImgData upload)
        { return ImgDept.GenerateName(isQa, upload); }

        public string CreateBrandInsertSql(BrandModel model)
        { return JsonConvert.SerializeObject(SqlHelper.BrandMaker(model)); }

        public string CreateExcelFiles(MemoryStream stream, string path)
        { return ExcelHelper.CreateExcelFiles(stream, path); }

        public string GetVendorFolders()
        { return JsonConvert.SerializeObject(Scrubber.GetVendorFolders()); }

        public string ScrubVendorFolder(ScrubberRequest request)
        { return JsonConvert.SerializeObject(Scrubber.ScrubVendor(request)); }

        public string CreateVendorLists(MissingImgData request)
        { return JsonConvert.SerializeObject(MissingImg.CreateVendorLists(request)); }
    }
}
