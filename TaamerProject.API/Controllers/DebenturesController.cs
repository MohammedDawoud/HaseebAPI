using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Models.Common;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.Services;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class DebenturesController : ControllerBase
    {
        private readonly IAcc_DebenturesService _Acc_DebenturesService;
        public GlobalShared _globalshared;

        public DebenturesController(IAcc_DebenturesService acc_DebenturesService)
        {
            _Acc_DebenturesService = acc_DebenturesService;
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
        }
        [HttpGet("GetAllDebentures")]

        public IActionResult GetAllDebentures(int Type)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var cate = _Acc_DebenturesService.GetAllDebentures(Type, _globalshared.YearId_G, _globalshared.BranchId_G);
            return cate == null ? NotFound() : Ok(cate);
        }

        [HttpPost("GetAllDebenturesSearch")]
        public IActionResult GetAllDebenturesSearch(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var cate = _Acc_DebenturesService.GetAllDebentures(voucherFilterVM.Type, _globalshared.YearId_G, _globalshared.BranchId_G);
            return cate == null ? NotFound() : Ok(cate);
        }
        [HttpPost("SaveDebenture")]

        public IActionResult SaveDebenture(Acc_Debentures Debenture)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_DebenturesService.SaveDebenture(Debenture, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("DeleteDebenture")]

        public IActionResult DeleteDebenture(int DebentureId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_DebenturesService.DeleteDebenture(DebentureId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpGet("GenerateDebentureNumber")]
        public IActionResult GenerateDebentureNumber(int Type)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var Value = _Acc_DebenturesService.GenerateDebentureNumber(Type, _globalshared.BranchId_G, _globalshared.YearId_G).Result;
            var NewValue = string.Format("{0:000000}", Value);
            var generatevalue = new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = NewValue };
            return Ok(generatevalue);
        }
    }
}
