using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.Services;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class StorehouseController : ControllerBase
    {
        private readonly IAcc_StorehouseService _Acc_StorehouseService;
        public GlobalShared _globalshared;

        public StorehouseController(IAcc_StorehouseService acc_StorehouseService)
        {
            _Acc_StorehouseService = acc_StorehouseService;
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
        }
        [HttpGet("GetAllStorehouses")]

        public IActionResult GetAllStorehouses()
        {
            var Storehouse = _Acc_StorehouseService.GetAllStorehouses("");
            return Storehouse == null ? NotFound() : Ok(Storehouse);
        }
        [HttpPost("SaveStorehouse")]

        public IActionResult SaveStorehouse(Acc_Storehouse Storehouse)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_StorehouseService.SaveStorehouse(Storehouse, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("DeleteStorehouse")]

        public IActionResult DeleteStorehouse(int StorehouseId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_StorehouseService.DeleteStorehouse(StorehouseId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpGet("FillStorehouseSelect")]

        public IActionResult FillStorehouseSelect()
        {
            return Ok(_Acc_StorehouseService.GetAllStorehouses("").Result.Select(s => new {
                Id = s.StorehouseId,
                Name = s.NameAr,
                NameEn = s.NameEn,
            }));
        }
    }
}
