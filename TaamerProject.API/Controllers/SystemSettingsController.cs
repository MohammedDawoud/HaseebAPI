using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Web.Script.Serialization;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.Services;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class SystemSettingsController : ControllerBase
    {
        private ISystemSettingsService _systemSettingsservice;
        private readonly IBranchesService _branchesService;
        private readonly ISys_SystemActionsService _sys_SystemActionsService;
        private IOrganizationsService _organizationsservice;
        private string? Con;
        private IConfiguration Configuration;

        public GlobalShared _globalshared;
        public SystemSettingsController(ISystemSettingsService systemSettingsservice, IBranchesService branchesService,
            ISys_SystemActionsService sys_SystemActionsService, IOrganizationsService organizationsservice,
            IConfiguration _configuration)
        {
            _systemSettingsservice = systemSettingsservice;
            _branchesService = branchesService;
            _sys_SystemActionsService = sys_SystemActionsService;
            _organizationsservice = organizationsservice;

            Configuration = _configuration;
            Con = this.Configuration.GetConnectionString("DBConnection");

            HttpContext httpContext = HttpContext;
            _globalshared = new GlobalShared(httpContext);
        }
        [HttpGet("GetSystemActionsAll")]

        public IActionResult GetSystemActionsAll(string Searchtxt)
        {

            var Res = _sys_SystemActionsService.GetAllSystemActionsAll().Result;
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;
            var result = new ContentResult
            {
                Content = serializer.Serialize(Res),
                ContentType = "application/json"
            };
            return result;
        }
        [HttpGet("GetSystemActions")]

        public IActionResult GetSystemActions(string? Searchtxt, string? DateFrom, string? DateTo, int? UserId, int? ActionType)
        {
            var Res = _sys_SystemActionsService.GetAllSystemActions(Searchtxt, DateFrom, DateTo, _globalshared.BranchId_G, UserId.Value, ActionType.Value).Result;
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(Res),
            //    ContentType = "application/json"
            //};
            return Ok(Res);
        }


        [HttpGet("GetSystemSettingsByBranchId")]

        public IActionResult GetSystemSettingsByBranchId()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var barnchData = _branchesService.GetBranchById(_globalshared.BranchId_G).Result;
            return Ok(_systemSettingsservice.GetSystemSettingsByBranchId(barnchData.OrganizationId));
        }
        [HttpGet("GetSystemSettingsByUserId")]
        public IActionResult GetSystemSettingsByUserId()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var barnchData = _branchesService.GetBranchById(_globalshared.BranchId_G).Result;
            return Ok(_systemSettingsservice.GetSystemSettingsByUserId(barnchData.OrganizationId, _globalshared.UserId_G, Con??""));
        }
        [HttpPost("SaveSystemSettings")]

        public IActionResult SaveSystemSettings(SystemSettings systemSettings)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var barnchData = _branchesService.GetBranchById(_globalshared.BranchId_G).Result;
            var result = _systemSettingsservice.SaveSystemSettings(systemSettings, _globalshared.UserId_G, barnchData.OrganizationId);
            return Ok(result);
        }
        [HttpPost("UpdateOrgdaterequire")]

        public IActionResult UpdateOrgdaterequire(bool isreq)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var barnchData = _branchesService.GetBranchById(_globalshared.BranchId_G).Result;
            var result = _systemSettingsservice.UpdateOrgDataRequired(isreq, _globalshared.UserId_G, barnchData.OrganizationId);
            return Ok(result);
        }

        [HttpPost("ValidateZatcaRequest")]

        public IActionResult ValidateZatcaRequest(bool Isuploadzatca)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var barnchData = _branchesService.GetBranchById(_globalshared.BranchId_G).Result;
            //var url = Server.MapPath("~/Email/MailStamp.html");
            var url = Path.Combine("/Email/MailStamp.html/");
            //var file = Server.MapPath("~/dist/assets/images/logo.png");
            var org = _organizationsservice.GetOrganizationDataLogin(_globalshared.Lang_G).Result;
            //var file = Server.MapPath("~") + org.LogoUrl;
            var file = Path.Combine(org.LogoUrl);

            var result = _systemSettingsservice.ValidateZatcaRequests(Isuploadzatca, _globalshared.UserId_G, barnchData.OrganizationId, url, file);
            return Ok(result);
        }
        [HttpPost("ValidateZatcaCode")]

        public IActionResult ValidateZatcaCode(bool Isuploadzatca, string SentCode)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var barnchData = _branchesService.GetBranchById(_globalshared.BranchId_G).Result;
            var result = _systemSettingsservice.ValidateZatcaCode(Isuploadzatca, SentCode, _globalshared.UserId_G, barnchData.OrganizationId);
            return Ok(result);
        }
        [HttpPost("MaintenanceFunc")]

        public IActionResult MaintenanceFunc(int Status)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _systemSettingsservice.MaintenanceFunc(Con, _globalshared.Lang_G, _globalshared.BranchId_G, _globalshared.UserId_G, Status);
            return Ok(result);
        }
    }
}
