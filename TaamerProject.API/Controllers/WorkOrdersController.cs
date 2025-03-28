﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Models.Common;
using TaamerProject.Service.Interfaces;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class WorkOrdersController : ControllerBase
    {
        private IWorkOrdersService _workOrdersservice;
        public GlobalShared _globalshared;
        public WorkOrdersController(IWorkOrdersService workOrdersservice)
        {
            _workOrdersservice = workOrdersservice;
            HttpContext httpContext = HttpContext;
            _globalshared = new GlobalShared(httpContext);
        }
        [HttpPost("SaveWorkOrder")]
        public IActionResult SaveWorkOrder([FromForm]string? WorkOrderId, [FromForm] string? OrderNo, [FromForm] string? OrderDate
            , [FromForm] string? OrderHijriDate, [FromForm] string? ExecutiveEng, [FromForm] string? ResponsibleEng
            , [FromForm] string? CustomerId, [FromForm] string? Required, [FromForm] string? Note
            ,[FromForm] string? EndDate, [FromForm] string? WOStatus, [FromForm] string? AgentId, [FromForm] string? ProjectId,
            [FromForm] List<IFormFile>? postedFiles)
        {

            WorkOrders workOrders = new WorkOrders();
            workOrders.WorkOrderId = Convert.ToInt32(WorkOrderId);
            workOrders.OrderNo = OrderNo;
            workOrders.OrderDate = OrderDate;
            workOrders.OrderHijriDate = OrderHijriDate;
            workOrders.ExecutiveEng = Convert.ToInt32(ExecutiveEng);
            workOrders.ResponsibleEng = Convert.ToInt32(ResponsibleEng);
            workOrders.CustomerId = Convert.ToInt32(CustomerId);
            workOrders.Required = Required;
            workOrders.Note = Note;
            workOrders.EndDate = EndDate;
            workOrders.WOStatus = Convert.ToInt32(WOStatus);
            workOrders.AgentId = AgentId;
            if(ProjectId!=null)
                workOrders.ProjectId = Convert.ToInt32(ProjectId);


            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            string path = Path.Combine("Uploads/ProjectRequirements/");
            string pathW = Path.Combine("/Uploads/ProjectRequirements/");

            string fileName = "";string fname = "";string fnamepath = "";

            if(postedFiles!=null)
            {
                foreach (IFormFile postedFile in postedFiles)
                {
                    fname = postedFile.FileName;
                    fileName = System.IO.Path.GetFileName(GenerateRandomNo() + fname);
                    fnamepath = Path.Combine(path, fileName);

                    using (System.IO.FileStream stream = new System.IO.FileStream(fnamepath, System.IO.FileMode.Create))
                    {

                        postedFile.CopyTo(stream);
                        string atturl = Path.Combine(path, fname);
                        workOrders.AttatchmentUrl = "/Uploads/ProjectRequirements/" + fileName;
                    }
                }
            }
            var result = _workOrdersservice.SaveWorkOrders(workOrders, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("SaveWorkOrderFile")]
        public IActionResult SaveWorkOrderFile([FromForm] string? WorkOrderId,[FromForm] List<IFormFile>? postedFiles)
        {

            WorkOrders workOrders = new WorkOrders();
            workOrders.WorkOrderId = Convert.ToInt32(WorkOrderId);

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            string path = Path.Combine("Uploads/ProjectRequirements/");
            string pathW = Path.Combine("/Uploads/ProjectRequirements/");

            string fileName = ""; string fname = ""; string fnamepath = "";

            if (postedFiles != null)
            {
                foreach (IFormFile postedFile in postedFiles)
                {
                    fname = postedFile.FileName;
                    fileName = System.IO.Path.GetFileName(GenerateRandomNo() + fname);
                    fnamepath = Path.Combine(path, fileName);

                    using (System.IO.FileStream stream = new System.IO.FileStream(fnamepath, System.IO.FileMode.Create))
                    {

                        postedFile.CopyTo(stream);
                        string atturl = Path.Combine(path, fname);
                        workOrders.AttatchmentUrl = "/Uploads/ProjectRequirements/" + fileName;
                    }
                }
            }
            var result = _workOrdersservice.EditWorkOrdersFile(workOrders, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("DeleteWorkOrder")]
        public IActionResult DeleteWorkOrder(int WorkOrderId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _workOrdersservice.DeleteWorkOrders(WorkOrderId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("SearchWorkOrders")]
        public IActionResult SearchWorkOrders(WorkOrdersVM WorkOrdersSearch)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.SearchWorkOrders(WorkOrdersSearch, _globalshared.Lang_G, _globalshared.BranchId_G).Result);
        }
        [HttpGet("GetAllWorkOrders")]
        public IActionResult GetAllWorkOrders()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetAllWorkOrders(_globalshared.BranchId_G, _globalshared.UserId_G).Result);
        }

        [HttpGet("GetAllWorkOrdersFilterd")]
        public IActionResult GetAllWorkOrdersFilterd(int? CustomerId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetAllWorkOrdersFilterd(_globalshared.BranchId_G, _globalshared.UserId_G, CustomerId).Result);
        }

        [HttpGet("GetAllWorkOrdersFilterd_Paging")]
        public IActionResult GetAllWorkOrdersFilterd_Paging(int? CustomerId,string? Searchtext, int page = 1, int pageSize = 10)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var Wo=_workOrdersservice.GetAllWorkOrdersFilterd(_globalshared.BranchId_G, _globalshared.UserId_G, CustomerId, Searchtext).Result;
            var data = GeneratePagination<WorkOrdersVM>.ToPagedList(Wo.ToList(), page, pageSize);
            var result = new PagedLists<WorkOrdersVM>(data.MetaData, data);
            return Ok(result);
        }
        [HttpGet("GetAllWorkOrdersyProjectId")]

        public IActionResult GetAllWorkOrdersyProjectId(int ProjectId)
        {
            return Ok(_workOrdersservice.GetAllWorkOrdersyProjectId(ProjectId));
        }
        [HttpGet("GetMaxOrderNumber")]
        public IActionResult GetMaxOrderNumber()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetMaxOrderNumber(_globalshared.BranchId_G).Result);
        }
        [HttpGet("GetAllWorkOrdersByDateSearch")]
        public IActionResult GetAllWorkOrdersByDateSearch(string DateFrom, string DateTo)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetAllWorkOrdersByDateSearch(DateFrom, DateTo, _globalshared.BranchId_G, _globalshared.UserId_G));
        }
        [HttpGet("GetWorkOrderById")]
        public IActionResult GetWorkOrderById(int WorkOrderId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetWorkOrderById(WorkOrderId, _globalshared.Lang_G));
        }
        [HttpGet("GetLateWorkOrdersByUserId")]
        public IActionResult GetLateWorkOrdersByUserId(string EndDateP)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetLateWorkOrdersByUserId(EndDateP, _globalshared.UserId_G, _globalshared.BranchId_G));
        }
        [HttpGet("GetLateWorkOrdersByUserIdFilterd")]
        public IActionResult GetLateWorkOrdersByUserIdFilterd(string EndDateP,int? CustomerId,int? ProjectId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetLateWorkOrdersByUserIdFilterd(EndDateP, _globalshared.UserId_G, _globalshared.BranchId_G, CustomerId, ProjectId));
        }



        [HttpGet("GetLateWorkOrdersByUserIdFilterd_paging")]
        public IActionResult GetLateWorkOrdersByUserIdFilterd_paging(string? EndDateP, int? CustomerId, int? ProjectId, string? Searchtext, int page = 1, int pageSize = 10)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var WO=_workOrdersservice.GetLateWorkOrdersByUserIdFilterd(EndDateP??"", _globalshared.UserId_G, _globalshared.BranchId_G, CustomerId, ProjectId, Searchtext).Result;
            var data = GeneratePagination<WorkOrdersVM>.ToPagedList(WO.ToList(), page, pageSize);
            var result = new PagedLists<WorkOrdersVM>(data.MetaData, data);
            return Ok(result);
        }


       


        [HttpGet("GetNewWorkOrdersByUserId")]
        public IActionResult GetNewWorkOrdersByUserId(string EndDateP)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetNewWorkOrdersByUserId(EndDateP, _globalshared.UserId_G, _globalshared.BranchId_G));
        }

        [HttpGet("GetNewWorkOrdersByUserIdFilterd")]
        public IActionResult GetNewWorkOrdersByUserIdFilterd(string EndDateP, int? CustomerId, int? ProjectId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetNewWorkOrdersByUserIdFilterd(EndDateP, _globalshared.UserId_G, _globalshared.BranchId_G, CustomerId, ProjectId));
        }

        [HttpGet("GetNewWorkOrdersByUserIdFilterd_paging")]
        public IActionResult GetNewWorkOrdersByUserIdFilterd_paging(string? EndDateP, int? CustomerId, int? ProjectId, string? Searchtext, int page = 1, int pageSize = 10)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var WO=_workOrdersservice.GetNewWorkOrdersByUserIdFilterd(EndDateP??"", _globalshared.UserId_G, _globalshared.BranchId_G, CustomerId, ProjectId,Searchtext).Result;
            var data = GeneratePagination<WorkOrdersVM>.ToPagedList(WO.ToList(), page, pageSize);
            var result = new PagedLists<WorkOrdersVM>(data.MetaData, data);
            return Ok(result);
        }

        [HttpGet("GetWorkOrdersByUserId")]
        public IActionResult GetWorkOrdersByUserId()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetWorkOrdersByUserId(_globalshared.UserId_G, _globalshared.BranchId_G));
        }
        [HttpGet("GetWorkOrdersByUserIdCount")]
        public int GetWorkOrdersByUserIdCount()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _workOrdersservice.GetWorkOrdersByUserId(_globalshared.UserId_G, _globalshared.BranchId_G).Result.Count();
            return result;
        }
        [HttpGet("GetWorkOrdersFilterationByUserId")]
        public IActionResult GetWorkOrdersFilterationByUserId(int flag, string? startdate, string? enddate)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetDayWeekMonth_Orders(_globalshared.UserId_G, 0, _globalshared.BranchId_G, flag, startdate, enddate));

        }
        [HttpPost("FinishOrder")]

        public IActionResult FinishOrder(WorkOrders workOrders)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _workOrdersservice.FinishOrder(workOrders, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpGet("GetWorkOrdersByUserIdandtask")]
        public IActionResult GetWorkOrdersByUserIdandtask(string task)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetWorkOrdersByUserIdandtask(task, _globalshared.UserId_G, _globalshared.BranchId_G));
        }
        [HttpGet("GetWorkOrdersBytask")]
        public IActionResult GetWorkOrdersBytask(string task)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_workOrdersservice.GetWorkOrdersBytask(task, _globalshared.BranchId_G));
        }
        [HttpGet("GetDayWeekMonth_Orders")]
        public int GetDayWeekMonth_Orders(int flag, string? startdate, string? enddate)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _workOrdersservice.GetDayWeekMonth_Orders(_globalshared.UserId_G, 0, _globalshared.BranchId_G, flag, startdate, enddate).Result.Count();
            return result;
        }
        [HttpPost("GenerateRandomNo")]
        public int GenerateRandomNo()
        {
            int _min = 1000;
            int _max = 9999;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max);
        }
    }
}
