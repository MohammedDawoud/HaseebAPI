using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Service.Interfaces;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceTypeController : ControllerBase
    {
        private readonly IAcc_ServiceTypesService _Acc_ServiceTypesService;
        public GlobalShared _globalshared;

        public ServiceTypeController(IAcc_ServiceTypesService Acc_ServiceTypesService)
        {
            _Acc_ServiceTypesService = Acc_ServiceTypesService;
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
        }
        [HttpGet("GetAllServiceTypes")]

        public IActionResult GetAllServiceTypes()
        {
            var result = _Acc_ServiceTypesService.GetAllServiceTypes();
            return Ok(result);
        }
        [HttpPost("SaveServiceType")]

        public IActionResult SaveServiceType(Acc_ServiceTypes ServiceType)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_ServiceTypesService.SaveServiceType(ServiceType, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("DeleteServiceType")]

        public IActionResult DeleteServiceType(int ServiceTypesId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_ServiceTypesService.DeleteServiceType(ServiceTypesId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpGet("FillServiceTypesSelect")]

        public IActionResult FillServiceTypesSelect()
        {
            var act = _Acc_ServiceTypesService.GetAllServiceTypes().Result.Select(s => new
            {
                Id = s.ServiceTypeId,
                Name = s.NameAr,
                NameEn = s.NameEn
            });
            return Ok(act);
        }
    }
}
