using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Service.Interfaces;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class ContacFilesController : ControllerBase
    {
        private IContacFilesService _ContacFileservice;
        public GlobalShared _globalshared;
        public ContacFilesController(IContacFilesService contacFileservice)
        {
            _ContacFileservice = contacFileservice;
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
        }
        [HttpGet("GetAllFiles")]
        public IActionResult GetAllFiles(int? OutInBoxId)
        {
            return Ok(_ContacFileservice.GetAllFiles(OutInBoxId));
        }
        [HttpGet("GetAllContacFiles")]
        public IActionResult GetAllContacFiles()
        {
            return Ok(_ContacFileservice.GetAllContacFiles());
        }
        [HttpPost("SaveFile")]
        public IActionResult SaveFile(ContacFiles ContacFiles)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _ContacFileservice.SaveContacFile(ContacFiles,_globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("DeleteFile")]
        public IActionResult DeleteFile(int FileId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _ContacFileservice.DeleteContacFile(FileId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpGet("GetAllFilesByParams")]
        public IActionResult GetAllFilesByParams(int? ArchiveFileId, int? Type, int? OutInType)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_ContacFileservice.GetAllFilesByParams(ArchiveFileId, Type, OutInType, _globalshared.BranchId_G));
        }
    }
}
