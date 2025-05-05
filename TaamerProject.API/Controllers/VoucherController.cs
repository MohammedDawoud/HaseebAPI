using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Data;
//using iTextSharp.text.pdf;
//using iTextSharp.text;
using System.Web.Script.Serialization; 

//using iText.Kernel.Pdf;
//using iText.Html2pdf;
//using iText.Layout.Font;
//using iText.Html2pdf.Resolver.Font;
//using iText.IO.Font;
//using iText.Kernel.Events;
//using Rectangle = iText.Kernel.Geom.Rectangle;
//using iText.Kernel.Pdf.Canvas;
//using Paragraph = iText.Layout.Element.Paragraph;
//using IEventHandler = iText.Kernel.Events.IEventHandler;
//using iText.Kernel.Pdf.Xobject;
//using iText.Layout;
//using iText.Layout.Element;
//using iText.Layout.Properties;
//using iText.Kernel.Geom;
using QRCoder;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json;
using System.Xml;
using Spire.Doc;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using TaamerProject.Service.Interfaces;
using TaamerProject.Models;
using TaamerProject.API.Helper;
using TaamerProject.Models.Common;
using System.Net;
using TaamerProject.API.Helper.QRCode.Tags;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Twilio.TwiML.Messaging;
using TaamerProject.Service.Services;
using TaamerProject.API.pdfHandler;
using static TaamerProject.Models.ReportGridVM;
using Haseeb.Service.LocalResources;
using static TaamerProject.API.Controllers.VoucherController;
using static TaamerProject.API.Controllers.SupervisionsController;
using iTextSharp.text.pdf;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class VoucherController : ControllerBase
    {
        // GET: Voucher
        private readonly IVoucherService _voucherService;
        private readonly IAcc_SuppliersService _acc_SuppliersService;

        private readonly IServicesPriceService _servicesPriceService;
        private readonly IServicesPriceOfferService _servicesPriceOfferService;

        private IBranchesService _BranchesService;
        private IOrganizationsService _organizationsservice;
        private ICostCenterService _CostCenterservice;
        private IProjectService _projectservice;
        private ICustomerService _customerService;
        private ISystemSettingsService _systemSettingsService;
        private readonly IFiscalyearsService _FiscalyearsService;
        private IEmployeesService _employesService;
        private readonly ICustomerSMSService _sMSService;


        private byte[] ReportPDF;
        private IConfiguration Configuration;
        public GlobalShared _globalshared;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private string Con;
        public VoucherController(IVoucherService voucherService, IAcc_SuppliersService acc_SuppliersService, IServicesPriceService servicesPriceService, IServicesPriceOfferService servicesPriceOfferService, IBranchesService branchesService,
            IOrganizationsService organizationsService, ICostCenterService costCenterService, IProjectService projectService, ICustomerService customerService,
            ISystemSettingsService systemSettingsService, ICustomerSMSService sMSService, IFiscalyearsService fiscalyearsService, IEmployeesService employeesService, IConfiguration _configuration, IWebHostEnvironment webHostEnvironment)
        {
            _projectservice = projectService;

            this._BranchesService = branchesService;
            this._organizationsservice = organizationsService;
            _voucherService = voucherService;
            _acc_SuppliersService = acc_SuppliersService;
            this._customerService = customerService;
            this._systemSettingsService = systemSettingsService;
            this._FiscalyearsService = fiscalyearsService;
            this._employesService = employeesService;
            _sMSService = sMSService;
            this._CostCenterservice = costCenterService;
            _servicesPriceService = servicesPriceService;
            _servicesPriceOfferService = servicesPriceOfferService;
            Configuration = _configuration; Con = this.Configuration.GetConnectionString("DBConnection");
            HttpContext httpContext = HttpContext;

            _globalshared = new GlobalShared(httpContext);
            _hostingEnvironment = webHostEnvironment;
        }

        //اخر سند قبض
        [HttpPost("GetLastVouchersre")]
        public IActionResult GetLastVouchersre(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var someVoucher = _voucherService.GetAllVouchers(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            var lastinvoice = someVoucher.Max(p => p.InvoiceId);
            var lastone = _voucherService.GetVoucherById(lastinvoice);

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(lastone),
            //    ContentType = "application/json"
            //};

            return Ok(lastone);
        }
        [HttpPost("GetAllVouchersNew")]
        public IActionResult GetAllVouchersNew(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var someVoucher = _voucherService.GetAllVouchers(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            return Ok(someVoucher);

        }
        public class InvoicemailWhatsapp
        {
            public int InvoiceId { get; set; }
            public string? Notes { get; set; }

            public string? AttachmentFile { get; set; }
        }
        [HttpPost("SendWInvoice2")]
        public IActionResult SendWInvoice2(IFormFile? UploadedFile, [FromForm] int InvoiceId, [FromForm] string? Notes, [FromForm] string? environmentURL, [FromForm] string? fileTypeUpload)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            //var filePath = Path.Combine(invoicemailWhatsapp.AttachmentFile ?? "");
            //var replacementPath = filePath.Replace('/', '\\');
            //var link = replacementPath;

            int ReportType = 4;
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;

            DataTable voucher = _voucherService.ReceiptCashingPaying(InvoiceId, Con).Result;
            InvoicesVM InvoicesVM = _voucherService.GetVoucherById(InvoiceId).Result;
            //costCenterNam = _CostCenterservice.GetCostCenterByProId(InvoicesVM.ProjectId.Value);

            var costCenterNam = "";
            if (InvoicesVM.VoucherDetails[0].CostCenterId != null)
            {

                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
            }
            else
            {
                costCenterNam = "بدون";
            }

            List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(InvoiceId).Result.ToList();
            if (ReportType == 1)//النموذج الاول للفاتورة
                ReportPDF = inVoiceReports.GenInvoice(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
            else if (ReportType == 2)///النموذج التانى للفاتورة
                ReportPDF = inVoiceReports.GenInvoice2(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
            else if (ReportType == 4)///فاتورة صغيره
                ReportPDF = inVoiceReports.GenInvoiceN_S(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam ?? "");
            else //New
                ReportPDF = inVoiceReports.GenInvoiceN(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam ?? "");

            //dawoudprint
            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;




            var FileUrl = "";

            if (UploadedFile != null)
            {
                string path = System.IO.Path.Combine("TempFiles/");
                string pathW = System.IO.Path.Combine("/TempFiles/");

                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }

                List<string> uploadedFiles = new List<string>();
                string pathes = "";
                string fileName = System.IO.Path.GetFileName(Guid.NewGuid() + UploadedFile.FileName);
                var path2 = Path.Combine(path, fileName);
                if (System.IO.File.Exists(path2))
                {
                    System.IO.File.Delete(path2);
                }
                using (System.IO.FileStream stream = new System.IO.FileStream(System.IO.Path.Combine(path, fileName), System.IO.FileMode.Create))
                {
                    UploadedFile.CopyTo(stream);
                    uploadedFiles.Add(fileName);
                    pathes = pathW + fileName;
                }


                if (pathes != "")
                {
                    FileName = fileName;
                    FileUrl = "/TempFiles/" + fileName;
                }
            }
            var FileAtt = "";
            if (fileTypeUpload=="2")
            {
                FileAtt = FilePathReturn;
            }
            else
            {
                FileAtt = FileUrl;
            }


            //FilePathReturn

            var result = _voucherService.SendWInvoice(InvoiceId, _globalshared.UserId_G, _globalshared.BranchId_G, FileAtt, environmentURL??"", fileTypeUpload??"");
            return Ok(result);
        }

        [HttpPost("SendWInvoice")]
        public IActionResult SendWInvoice(IFormFile? UploadedFile, [FromForm] int InvoiceId, [FromForm] string? Notes, [FromForm] string? environmentURL, [FromForm] string? fileTypeUpload)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            //var filePath = Path.Combine(invoicemailWhatsapp.AttachmentFile ?? "");
            //var replacementPath = filePath.Replace('/', '\\');
            //var link = replacementPath;

            int ReportType = 4;
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;

            DataTable voucher = _voucherService.ReceiptCashingPaying(InvoiceId, Con).Result;
            InvoicesVM InvoicesVM = _voucherService.GetVoucherById(InvoiceId).Result;
            //costCenterNam = _CostCenterservice.GetCostCenterByProId(InvoicesVM.ProjectId.Value);

            var costCenterNam = "";
            if (InvoicesVM.VoucherDetails[0].CostCenterId != null)
            {

                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
            }
            else
            {
                costCenterNam = "بدون";
            }

            List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(InvoiceId).Result.ToList();
            if (ReportType == 1)//النموذج الاول للفاتورة
                ReportPDF = inVoiceReports.GenInvoice(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
            else if (ReportType == 2)///النموذج التانى للفاتورة
                ReportPDF = inVoiceReports.GenInvoice2(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
            else if (ReportType == 4)///فاتورة صغيره
                ReportPDF = inVoiceReports.GenInvoiceN_S(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam ?? "");
            else //New
                ReportPDF = inVoiceReports.GenInvoiceN(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam ?? "");

            //dawoudprint
            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;




            var FileUrl = "";

            if (UploadedFile != null)
            {
                string path = System.IO.Path.Combine("TempFiles/");
                string pathW = System.IO.Path.Combine("/TempFiles/");

                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }

                List<string> uploadedFiles = new List<string>();
                string pathes = "";
                string fileName = System.IO.Path.GetFileName(Guid.NewGuid() + UploadedFile.FileName);
                var path2 = Path.Combine(path, fileName);
                if (System.IO.File.Exists(path2))
                {
                    System.IO.File.Delete(path2);
                }
                using (System.IO.FileStream stream = new System.IO.FileStream(System.IO.Path.Combine(path, fileName), System.IO.FileMode.Create))
                {
                    UploadedFile.CopyTo(stream);
                    uploadedFiles.Add(fileName);
                    pathes = pathW + fileName;
                }


                if (pathes != "")
                {
                    FileName = fileName;
                    FileUrl = "/TempFiles/" + fileName;
                }
            }
            var FileAtt = "";
            if (fileTypeUpload == "2")
            {
                FileAtt = FilePathReturn;
            }
            else
            {
                FileAtt = FileUrl;
            }


            //FilePathReturn
            var Message = Notes;
            //var result2 = _voucherService.SendWInvoice(InvoiceId, _globalshared.UserId_G, _globalshared.BranchId_G, FileAtt, environmentURL ?? "", fileTypeUpload ?? "");
            var result = _sMSService.SendWhatsApp_Notification(InvoicesVM.CustomerMobile ?? "", Message??"", _globalshared.UserId_G, _globalshared.BranchId_G, environmentURL??"", FileAtt);
            return Ok(result);
        }




        [HttpGet("GetInvoiceByCustomer")]
        public IActionResult GetInvoiceByCustomer(int CustomerId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var someVoucher = _voucherService.GetInvoiceByCustomer(CustomerId, _globalshared.YearId_G).Result.ToList();
            return Ok(someVoucher);

        }

        [HttpGet("GetInvoiceByNo")]
        public IActionResult GetInvoiceByNo(string VoucherNo)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var someVoucher = _voucherService.GetInvoiceByNo(VoucherNo, _globalshared.YearId_G).Result;
            return Ok(someVoucher);

        }

        [HttpGet("GetInvoiceByNo_purches")]
        public IActionResult GetInvoiceByNo_purches(string VoucherNo)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var someVoucher = _voucherService.GetInvoiceByNo_purches(VoucherNo, _globalshared.YearId_G).Result;
            return Ok(someVoucher);

        }

        [HttpPost("GetAllVouchers")]
        public IActionResult GetAllVouchers(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            decimal TotalCredit = 0;
            decimal TotalDepit = 0;


            var someVoucher = _voucherService.GetAllVouchers(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            if (someVoucher.Count() > 0)
            {
                foreach (var item in someVoucher)
                {
                    TotalCredit = 0;
                    TotalDepit = 0;

                    TotalCredit = GetAllCreditNotiTotalValue(item.InvoiceId).Item1;
                    TotalDepit = GetAllCreditNotiTotalValue(item.InvoiceId).Item2;

                    item.CreditNotiTotal = TotalCredit;
                    item.DepitNotiTotal = TotalDepit;

                }
            }
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);

        }
        [HttpPost("GetAllVouchersLastMonth")]
        public IActionResult GetAllVouchersLastMonth(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            string DateFrom = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            string DateTo = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            if (!(voucherFilterVM.IsSearch??false))
            {
                voucherFilterVM.dateFrom = DateFrom;
                voucherFilterVM.dateTo = DateTo;
            }


            var someVoucher = _voucherService.GetAllVouchersLastMonth(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);
        }
        [HttpPost("GetAllVouchersSearch")]
        public IActionResult GetAllVouchersSearch(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var someVoucher = _voucherService.GetAllVouchersSearch(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);
        }

        //public Tuple<decimal, decimal> GetAllCreditNotiTotalValue(int invoiceid)
        //{

        //    decimal TotalValueCredit = 0;
        //    decimal TotalValueDepit = 0;

        //    try
        //    {
        //        var Voucher = _voucherService.GetVoucherByIdNoti(invoiceid).ToList();
        //        if (Voucher.Count() > 0)
        //        {
        //            foreach (var item in Voucher)
        //            {
        //                if (item.Type == 29)
        //                {
        //                    TotalValueCredit += item.TotalValue ?? 0;
        //                }
        //                if (item.Type == 30)
        //                {
        //                    TotalValueDepit += item.TotalValue ?? 0;
        //                }

        //            }
        //        }
        //        else
        //        {
        //            TotalValueCredit = 0;
        //            TotalValueDepit = 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TotalValueCredit = 0;
        //        TotalValueDepit = 0;
        //    }
        //    return Tuple.Create(TotalValueCredit, TotalValueDepit);
        //}
        [HttpGet("GetAllCreditNotiTotalValue")]
        public Tuple<decimal, decimal> GetAllCreditNotiTotalValue(int invoiceid)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            decimal TotalValueCredit = 0;
            decimal TotalValueDepit = 0;

            try
            {
                var Voucher = _voucherService.GetVoucherByIdNoti(invoiceid).Result.ToList();
                if (Voucher.Count() > 0)
                {
                    foreach (var item in Voucher)
                    {
                        if (item.Type == 29)
                        {
                            TotalValueCredit += item.CreditNotiTotal ?? 0;
                        }
                        if (item.Type == 30)
                        {
                            TotalValueDepit += item.DepitNotiTotal ?? 0;
                        }

                    }
                }
                else
                {
                    TotalValueCredit = 0;
                    TotalValueDepit = 0;
                }
            }
            catch (Exception ex)
            {
                TotalValueCredit = 0;
                TotalValueDepit = 0;
            }
            return Tuple.Create(TotalValueCredit, TotalValueDepit);
        }
        [HttpGet("GetAllCreditNotiTotalValuePurchase")]
        public Tuple<decimal, decimal> GetAllCreditNotiTotalValuePurchase(int invoiceid)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            decimal TotalValueCredit = 0;
            decimal TotalValueDepit = 0;

            try
            {
                var Voucher = _voucherService.GetVoucherByIdNoti(invoiceid).Result.ToList();
                if (Voucher.Count() > 0)
                {
                    foreach (var item in Voucher)
                    {
                        if (item.Type == 32)
                        {
                            TotalValueCredit += item.CreditNotiTotal ?? 0;
                        }
                        if (item.Type == 33)
                        {
                            TotalValueDepit += item.DepitNotiTotal ?? 0;
                        }

                    }
                }
                else
                {
                    TotalValueCredit = 0;
                    TotalValueDepit = 0;
                }
            }
            catch (Exception ex)
            {
                TotalValueCredit = 0;
                TotalValueDepit = 0;
            }
            return Tuple.Create(TotalValueCredit, TotalValueDepit);
        }

        [HttpPost("GetAllVouchersSearchInvoice")]
        public IActionResult GetAllVouchersSearchInvoice(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            decimal TotalCredit = 0;
            decimal TotalDepit = 0;


            var someVoucher = _voucherService.GetAllVouchersSearch(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            if (someVoucher.Count() > 0)
            {
                foreach (var item in someVoucher)
                {
                    TotalCredit = 0;
                    TotalDepit = 0;

                    TotalCredit = GetAllCreditNotiTotalValue(item.InvoiceId).Item1;
                    TotalDepit = GetAllCreditNotiTotalValue(item.InvoiceId).Item2;

                    item.CreditNotiTotal = TotalCredit;
                    item.DepitNotiTotal = TotalDepit;

                }
            }
            return Ok(someVoucher);
        }
        [HttpGet("GetVouchersSearchInvoiceByID")]

        public IActionResult GetVouchersSearchInvoiceByID(int InvoiceId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            decimal TotalCredit = 0;
            decimal TotalDepit = 0;


            var someVoucher = _voucherService.GetVouchersSearchInvoiceByID(InvoiceId, _globalshared.BranchId_G, _globalshared.YearId_G).Result;
            if (someVoucher != null)
            {
                TotalCredit = GetAllCreditNotiTotalValue(someVoucher.InvoiceId).Item1;
                TotalDepit = GetAllCreditNotiTotalValue(someVoucher.InvoiceId).Item2;

                someVoucher.CreditNotiTotal = TotalCredit;
                someVoucher.DepitNotiTotal = TotalDepit;
            }
            return Ok(someVoucher);

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            //return result;
        }
        [HttpGet("GetVouchersSearchInvoicePurchaseByID")]
        public IActionResult GetVouchersSearchInvoicePurchaseByID(int InvoiceId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            //decimal TotalCredit = 0;
            decimal TotalDepit = 0;


            var someVoucher = _voucherService.GetVouchersSearchInvoicePurchaseByID(InvoiceId, _globalshared.BranchId_G, _globalshared.YearId_G).Result;
            if (someVoucher != null)
            {
                //TotalCredit = GetAllCreditNotiTotalValue(someVoucher.InvoiceId).Item1;
                TotalDepit = GetAllCreditNotiTotalValuePurchase(someVoucher.InvoiceId).Item2;

                //someVoucher.CreditNotiTotal = TotalCredit;
                someVoucher.DepitNotiTotal = TotalDepit;
            }
            return Ok(someVoucher);

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            //return result;
        }
        [HttpPost("GetAllVouchersRetSearch")]
        public IActionResult GetAllVouchersRetSearch(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            //var userBranchs = _BranchesService.GetAllBranchesByUserId(Lang, _globalshared.UserId_G);
            //var someVoucher = _voucherService.GetAllVouchersRetSearch(voucherFilterVM, 0, _globalshared.YearId_G);
            //foreach (var userBranch in userBranchs)
            //{
            //    var AllVouchers = _voucherService.GetAllVouchersRetSearch(voucherFilterVM, userBranch.BranchId, _globalshared.YearId_G).ToList();
            //    var Vouchers = someVoucher.Union(AllVouchers);
            //    someVoucher = Vouchers.ToList();
            //}


            var someVoucher = _voucherService.GetAllVouchersRetSearch(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};

            return Ok(someVoucher);

        }
        [HttpPost("GetAllVouchersfromcontractSearch")]
        public IActionResult GetAllVouchersfromcontractSearch(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var someVoucher = _voucherService.GetAllVouchersfromcontractSearch(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};

            return Ok(someVoucher);

        }


        [HttpPost("GetAllVouchersPurchase")]
        public IActionResult GetAllVouchersPurchase(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            // decimal TotalCredit = 0;
            decimal TotalDepit = 0;


            var someVoucher = _voucherService.GetAllVouchersPurchase(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            if (someVoucher.Count() > 0)
            {
                foreach (var item in someVoucher)
                {
                    //TotalCredit = 0;
                    TotalDepit = 0;

                    //TotalCredit = GetAllCreditNotiTotalValue(item.InvoiceId).Item1;
                    TotalDepit = GetAllCreditNotiTotalValuePurchase(item.InvoiceId).Item2;

                    //item.CreditNotiTotal = TotalCredit;
                    item.DepitNotiTotal = TotalDepit;

                }
            }
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);
        }
        [HttpPost("GetAllVouchersRetSearchPurchase")]
        public IActionResult GetAllVouchersRetSearchPurchase(VoucherFilterVM voucherFilterVM)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var someVoucher = _voucherService.GetAllVouchersRetSearchPurchase(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);
        }

        //اخر فاتورة
        [HttpGet("GetLastVoucher")]
        public IActionResult GetLastVoucher()
        {
            var invoic = _voucherService.GetAllVouchersback().Result;
            var lastinvoice = invoic.Max(p => p.InvoiceId);
            var lastone = _voucherService.GetVoucherById(lastinvoice);
            return Ok(lastone);
        }
        [HttpPost("GetAllVouchersQR")]
        public IActionResult GetAllVouchersQR(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            return Ok(_voucherService.GetAllVouchersQR(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G));
        }
        [HttpGet("GetAllVouchersProject")]
        public IActionResult GetAllVouchersProject()
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_voucherService.GetAllVouchersProject(_globalshared.BranchId_G, _globalshared.YearId_G));
        }
        [HttpPost("GetLastVouchersRet")]
        public IActionResult GetLastVouchersRet(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);



            var someVoucher = _voucherService.GetAllVouchersRet(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var lastinvoice = someVoucher.Max(p => p.InvoiceId);
            //var lastone = _voucherService.GetVoucherById(lastinvoice);
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};

            ////return Ok(lastone);
            return Ok(someVoucher);

        }
        [HttpPost("GetAllVouchersRet")]
        public IActionResult GetAllVouchersRet(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);



            //var userBranchs = _BranchesService.GetAllBranchesByUserId(Lang, _globalshared.UserId_G);
            //var someVoucher = _voucherService.GetAllVouchersRet(voucherFilterVM, 0, _globalshared.YearId_G);
            //foreach (var userBranch in userBranchs)
            //{
            //    var AllVouchers = _voucherService.GetAllVouchersRet(voucherFilterVM, userBranch.BranchId, _globalshared.YearId_G).ToList();
            //    var Vouchers = someVoucher.Union(AllVouchers);
            //    someVoucher = Vouchers.ToList();
            //}


            var someVoucher = _voucherService.GetAllVouchersRet(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);
        }
        [HttpPost("GetAllVouchersRetPurchase")]

        public IActionResult GetAllVouchersRetPurchase(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);


            var someVoucher = _voucherService.GetAllVouchersRetPurchase(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);
        }
        [HttpPost("GetAllCreditDepitNotiReport")]
        public IActionResult GetAllCreditDepitNotiReport(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);


            var someVoucher = _voucherService.GetAllCreditDepitNotiReport(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);

        }
        [HttpPost("GetAllVouchersRetReport")]
        public IActionResult GetAllVouchersRetReport(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);


            var someVoucher = _voucherService.GetAllVouchersRetReport(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);

        }
        [HttpPost("GetAllVouchersRetReport_Pur")]
        public IActionResult GetAllVouchersRetReport_Pur(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var someVoucher = _voucherService.GetAllVouchersRetReport_Pur(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);

        }
        [HttpPost("GetAllPayVouchersRet")]
        public IActionResult GetAllPayVouchersRet(VoucherFilterVM voucherFilterVM)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var someVoucher = _voucherService.GetAllPayVouchersRet(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};
            return Ok(someVoucher);
        }

        [HttpGet("GetVoucherRpt")]
        public IActionResult GetVoucherRpt()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            return Ok(_voucherService.GetVoucherRpt(_globalshared.BranchId_G, _globalshared.YearId_G));
        }
        [HttpGet("GetCustRevenueExpensesDetails")]
        public IActionResult GetCustRevenueExpensesDetails(string FromDate, string ToDate)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            return Ok(_voucherService.GetCustRevenueExpensesDetails(FromDate, ToDate, _globalshared.BranchId_G, _globalshared.YearId_G));
        }
        [HttpGet("PrintReceiptExchangeVoucher")]
        public IActionResult PrintReceiptExchangeVoucher(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var voucher = _voucherService.GetVoucherById(VoucherId);
            return Ok(voucher);
        }
        [HttpGet("GetInvoiceById")]
        public IActionResult GetInvoiceById(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var voucher = _voucherService.GetVoucherById(VoucherId);
            return Ok(voucher);
        }
        [HttpGet("GetInvoiceById_Tran")]
        public IActionResult GetInvoiceById_Tran(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            decimal TotalCredit = 0;
            decimal TotalDepit = 0;

            var voucher = _voucherService.GetVoucherById(VoucherId).Result;
            if (voucher != null)
            {
                TotalCredit = 0;
                TotalDepit = 0;

                TotalCredit = GetAllCreditNotiTotalValue(voucher.InvoiceId).Item1;
                TotalDepit = GetAllCreditNotiTotalValue(voucher.InvoiceId).Item2;

                voucher.CreditNotiTotal = TotalCredit;
                voucher.DepitNotiTotal = TotalDepit;
            }

            return Ok(voucher);
        }
        [HttpGet("GetInvoiceDateById")]
        public IActionResult GetInvoiceDateById(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var voucher = _voucherService.GetInvoiceDateById(VoucherId);
            return Ok(voucher);
        }
        //[HttpGet("VouchersRetReport")]
        //public IActionResult VouchersRetReport(VoucherFilterVM param)
        //    {
        //    HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

        //    var result = _voucherService.GetAllVouchersRetReport(param, _globalshared.BranchId_G, _globalshared.YearId_G);
        //        int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
        //        var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
        //        string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email,
        //        objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
        //        ReportPDF = Bayanatech.TameerUI.pdfHandler.ReportsOf7sabat.VouchersRetReport(result.ToList(), param, infoDoneTasksReport);

        //        string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //        if (!Directory.Exists(existTemp))
        //        {
        //            Directory.CreateDirectory(existTemp);
        //        }
        //        //File  
        //        string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //        string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        //        //create and set PdfReader  
        //        System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        //        //return file 
        //        string FilePathReturn = @"TempFiles/" + FileName;
        //        return Content(FilePathReturn);
        //    }
        [HttpPost("VouchersCreditDepitNotiReport")]
        public IActionResult VouchersCreditDepitNotiReport(VoucherFilterVM param)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.GetAllCreditDepitNotiReport(param, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email,
                objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
            var branch = _BranchesService.GetBranchById(_globalshared.BranchId_G).Result;
            param.BranchName = branch.NameAr;
            ReportPDF = ReportsOf7sabat.VouchersCreditDepitNotiReport(result.ToList(), param, infoDoneTasksReport);

            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;
            return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = FilePathReturn });
        }

        //    public IActionResult PurchaseVoucherRetReport(VoucherFilterVM param)
        //    {


        //        var result = _voucherService.GetAllVouchersRetReport_Pur(param, _globalshared.BranchId_G, _globalshared.YearId_G);
        //        int orgId = _BranchesService.GetOrganizationId(BranchId);
        //        var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        //        string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email,
        //        objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
        //        ReportPDF = Bayanatech.TameerUI.pdfHandler.ReportsOf7sabat.PurchaseVoucherRetReport(result.ToList(), param.dateFrom, param.dateTo, infoDoneTasksReport);

        //        string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //        if (!Directory.Exists(existTemp))
        //        {
        //            Directory.CreateDirectory(existTemp);
        //        }
        //        //File  
        //        string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //        string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        //        //create and set PdfReader  
        //        System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        //        //return file 
        //        string FilePathReturn = @"TempFiles/" + FileName;
        //        return Content(FilePathReturn);
        //    }


        //public IActionResult PrintVoucher(int VoucherId)
        //{
        //    var voucher = _voucherService.GetVoucherById(VoucherId);
        //    return View(voucher);
        //}
        [HttpGet("PrintDailyVoucher")]
        public IActionResult PrintDailyVoucher(int VoucherId)
        {
            var voucher = _voucherService.GetVoucherById(VoucherId);
            return Ok(voucher);
        }
        [HttpGet("GetAllDetailsByVoucherId")]
        public IActionResult GetAllDetailsByVoucherId(int? voucherId)
        {
            return Ok(_voucherService.GetAllDetailsByVoucherId(voucherId));
        }
        [HttpGet("GetInvoiceIDByProjectID")]
        public IActionResult GetInvoiceIDByProjectID(int? ProjectId)
        {
            return Ok(_voucherService.GetInvoiceIDByProjectID(ProjectId));
        }
        [HttpGet("GetAllDetailsByInvoiceId")]
        public IActionResult GetAllDetailsByInvoiceId(int? voucherId)
        {
            return Ok(_voucherService.GetAllDetailsByInvoiceId(voucherId));
        }
        [HttpGet("GetAllDetailsByInvoiceIdFirstOrDef")]
        public IActionResult GetAllDetailsByInvoiceIdFirstOrDef(int? voucherId)
        {
            var Result = _voucherService.GetAllDetailsByInvoiceIdFirstOrDef(voucherId);
            return Ok(Result);
        }
        [HttpGet("GetAllTransByLineNo")]
        public IActionResult GetAllTransByLineNo(int LineNo)
        {
            return Ok(_voucherService.GetAllTransByLineNo(LineNo));

        }
        [HttpGet("GetAllTrans")]
        public IActionResult GetAllTrans(int VouDetailsID)
        {
            return Ok(_voucherService.GetAllTrans(VouDetailsID));

        }
        [HttpPost("SaveVoucher")]
        public IActionResult SaveVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }
        [HttpPost("SaveandPostVoucher")]
        public IActionResult SaveandPostVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveandPostVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveVoucherP")]
        public IActionResult SaveVoucherP(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveVoucherP(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }



        [HttpPost("UploadPayVoucherImage")] 
        public IActionResult UploadPayVoucherImage(IFormFile? UploadedFile,[FromForm] int? InvoiceId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            string FileName = "";
            string FileUrl = "";
            if (InvoiceId > 0)
            {
                if (UploadedFile != null)
                {
                    string path = System.IO.Path.Combine("Uploads/", "Financefile/");
                    string pathW = System.IO.Path.Combine("/Uploads/", "Financefile/");

                    if (!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }

                    List<string> uploadedFiles = new List<string>();
                    string pathes = "";
                    //foreach (IFormFile postedFile in postedFiles)
                    //{
                    string fileName = System.IO.Path.GetFileName(Guid.NewGuid() + UploadedFile.FileName);
                    //string fileName = System.IO.Path.GetFileName(postedFiles.FileName);

                    var path2 = Path.Combine(path, fileName);
                    if (System.IO.File.Exists(path2))
                    {
                        System.IO.File.Delete(path2);
                    }
                    using (System.IO.FileStream stream = new System.IO.FileStream(System.IO.Path.Combine(path, fileName), System.IO.FileMode.Create))
                    {


                        UploadedFile.CopyTo(stream);
                        uploadedFiles.Add(fileName);
                        // string returnpath = host + path + fileName;
                        //pathes.Add(pathW + fileName);
                        pathes = pathW + fileName;
                    }


                    if (pathes != "")
                    {
                        FileName = fileName;
                        FileUrl = "/Uploads/Financefile/" + fileName;
                    }
                }
                var result = _voucherService.SaveVoucherPUpdateImage(InvoiceId??0, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G, FileName, FileUrl);
                return Ok(result);
            }
            else
            {
                return Ok(new GeneralMessage{ StatusCode= HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed });
            }



        }


        [HttpPost("SaveandPostVoucherP")]
        public IActionResult SaveandPostVoucherP(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveandPostVoucherP(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }
        [HttpPost("UpdateVoucher")]
        public IActionResult UpdateVoucher(int InvoiceId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.UpdateVoucher(InvoiceId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("UpdateVoucherDraft")]
        public IActionResult UpdateVoucherDraft(int InvoiceId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.UpdateVoucherDraft(InvoiceId, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }

        [HttpPost("UpdateVoucher_recipient")]
        public IActionResult UpdateVoucher_recipient(string InvoiceId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.UpdateVoucherRecepient(InvoiceId, _globalshared.UserId_G, _globalshared.BranchId_G,_globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("UpdateVoucher_payed")]
        public IActionResult UpdateVoucher_payed(int InvoiceId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.UpdateVoucher_payed(InvoiceId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }

        [HttpPost("UpdateVoucher_payed_bySupp")]
        public IActionResult UpdateVoucher_payed_bySupp(string SupplierInvoiceNo)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.UpdateVoucher_payed_by(SupplierInvoiceNo, _globalshared.UserId_G, _globalshared.BranchId_G,_globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("VousherRe_Sum")]
        public IActionResult VousherRe_Sum(int InvoiceId)
        {
            var result = _voucherService.VousherRe_Sum(InvoiceId);
            var res = new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = result.ToString() };
            return Ok(res);
        }


        [HttpPost("PayVousher_Sum")]
        public IActionResult PayVousher_Sum(int InvoiceId)
        {
            var result = _voucherService.PayVousher_Sum(InvoiceId);
            var res = new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = result.ToString() };
            return Ok(res);
        }


        [HttpPost("SaveInvoice")]
        public IActionResult SaveInvoice(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveInvoice(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveInvoiceForServices")]
        public IActionResult SaveInvoiceForServices(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveInvoiceForServices(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            //var syssetting = _systemSettingsService.GetSystemSettingsByBranchId(BranchId);
            //if (syssetting.UploadInvZatca == true)
            //{

            //    if (result.Result == true)
            //    {
            //if (voucher.TotalValue > 1000)
            //{
            //    btn_StandardInvoice_Click(result.ReturnedParm);
            //}
            //else
            //{
            //    btn_SimplifiedInvoice_Click(result.ReturnedParm);
            //}

            //    }
            //}
            return Ok(result);
        }
        [HttpPost("SaveInvoiceForServicesDraft")]
        public IActionResult SaveInvoiceForServicesDraft(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveInvoiceForServicesDraft(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveInvoiceForServicesNoti")]
        public IActionResult SaveInvoiceForServicesNoti(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            //var result2 = _voucherService.GetVoucherById(voucher.InvoiceId);


            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveInvoiceForServicesNoti(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G, Con ?? "");
            //var syssetting = _systemSettingsService.GetSystemSettingsByBranchId(BranchId);
            //if (syssetting.UploadInvZatca == true)
            //{
            //    if (result.Result == true)
            //    {
            //        createxmlInvoicecreditdevit(result.ReturnedParm, 29);
            //    }
            //    if (result2.TotalValue > 1000)
            //    {
            //        btn_StandardCreditNote_Click(result.ReturnedParm);
            //    }
            //    else
            //    {
            //        btn_SimplifiedCreditNote_Click(result.ReturnedParm);
            //    }
            //}
            return Ok(result);
        }


        [HttpPost("SaveInvoiceForServicesNotiDepit")]
        public IActionResult SaveInvoiceForServicesNotiDepit(Invoices voucher)
        {
            //var result2 = _voucherService.GetVoucherById(voucher.InvoiceId);

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveInvoiceForServicesNotiDepit(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            //var syssetting = _systemSettingsService.GetSystemSettingsByBranchId(BranchId);
            //if (syssetting.UploadInvZatca == true)
            //{
            //    if (result2.TotalValue > 1000)
            //    {
            //        btn_StandaredDebitNote_Click(result.ReturnedParm);
            //    }
            //    else
            //    {
            //        btn_SimplifiedDebitNote_Click(result.ReturnedParm);
            //    }
            //}
            return Ok(result);
        }
        [HttpPost("SaveandPostInvoiceForServices")]
        public IActionResult SaveandPostInvoiceForServices(Invoices voucher)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveandPostInvoiceForServices(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            //var syssetting = _systemSettingsService.GetSystemSettingsByBranchId(BranchId);
            //if (syssetting.UploadInvZatca == true)
            //{
            //    if (result.Result == true)
            //    {
            //        if (voucher.TotalValue > 1000)
            //        {
            //            btn_StandardInvoice_Click(result.ReturnedParm);
            //        }
            //        else
            //        {
            //            btn_SimplifiedInvoice_Click(result.ReturnedParm);
            //        }

            //    }
            //}
            return Ok(result);
        }
        [HttpPost("SaveInvoiceForServicesRet")]
        public IActionResult SaveInvoiceForServicesRet(Invoices voucher)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.SaveInvoiceForServicesRet(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveInvoiceForServicesRetNEW")]
        public IActionResult SaveInvoiceForServicesRetNEW(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.SaveInvoiceForServicesRetNEW_func(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G, _globalshared.Lang_G, Con);

            return Ok(result);
        }
        [HttpPost("ReturnNotiCreditBack")]
        public IActionResult ReturnNotiCreditBack(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.ReturnNotiCreditBack(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("ReturnNotiDepitBack")]
        public IActionResult ReturnNotiDepitBack(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.ReturnNotiDepitBack(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveInvoiceForServicesRet_Back")]
        public IActionResult SaveInvoiceForServicesRet_Back(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.SaveInvoiceForServicesRet_Back(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        //Start Purchase
        [HttpPost("SavePurchaseForServices")]
        public IActionResult SavePurchaseForServices(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SavePurchaseForServices(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }
        [HttpPost("SavePurchaseForServicesNotiDepit")]
        public IActionResult SavePurchaseForServicesNotiDepit(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SavePurchaseForServicesNotiDepit(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G, Con ?? "");
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }
        [HttpPost("SaveandPostPurchaseForServices")]
        public IActionResult SaveandPostPurchaseForServices(Invoices voucher)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveandPostPurchaseForServices(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }
        [HttpPost("SavePurchaseForServicesRet")]
        public IActionResult SavePurchaseForServicesRet(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.SavePurchaseForServicesRet(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SavePurchaseForServicesRetNew")]
        public IActionResult SavePurchaseForServicesRetNew(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.SavePurchaseForServicesRetNEW_func(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G, _globalshared.Lang_G, Con);
            return Ok(result);
        }
        //End Purchase
        [HttpPost("SavePayVoucherForServicesRet")]
        public IActionResult SavePayVoucherForServicesRet(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);


            var result = _voucherService.SavePayVoucherForServicesRet(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveInvoiceForServices2")]
        public IActionResult SaveInvoiceForServices2(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);


            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveInvoiceForServices2(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveandPostInvoiceForServices2")]
        public IActionResult SaveandPostInvoiceForServices2(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);


            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveandPostInvoiceForServices2(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveConvertVoucher")]
        public IActionResult SaveConvertVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveConvertVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveDailyVoucher")]
        public IActionResult SaveDailyVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveDailyVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }
        [HttpPost("SaveandPostDailyVoucher")]
        public IActionResult SaveandPostDailyVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveandPostDailyVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(new { result.StatusCode, result.ReasonPhrase, result.ReturnedParm });
        }
        [HttpPost("SaveClosingVoucher")]
        public IActionResult SaveClosingVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            int? dataVoucher = _voucherService.GenerateVoucherNumberClosing(25, _globalshared.BranchId_G, _globalshared.YearId_G).Result;

            var result = _voucherService.SaveClosingVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, dataVoucher, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveEmpVoucher")]
        public IActionResult SaveEmpVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveEmpVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveRecycleVoucher")]
        public IActionResult SaveRecycleVoucher(int YearID)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.SaveRecycleVoucher(YearID, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G, Con);
            return Ok(result);
        }
        [HttpPost("SaveRecycleReturnVoucher")]
        public IActionResult SaveRecycleReturnVoucher(int YearID)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.SaveRecycleReturnVoucher(YearID, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G, Con);
            return Ok(result);
        }
        [HttpPost("SaveOpeningVoucher")]
        public IActionResult SaveOpeningVoucher(Invoices voucher)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            int? dataVoucher = _voucherService.GenerateVoucherNumberOpening(10, _globalshared.BranchId_G, _globalshared.YearId_G)?.Result??0;

            var result = _voucherService.SaveOpeningVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, dataVoucher, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveandPostOpeningVoucher")]
        public IActionResult SaveandPostOpeningVoucher(Invoices voucher)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var VoucherDatetime = DateTime.ParseExact(voucher.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            int? dataVoucher = _voucherService.GenerateVoucherNumberOpening(10, _globalshared.BranchId_G, _globalshared.YearId_G)?.Result??0;
            var result = _voucherService.SaveandPostOpeningVoucher(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, dataVoucher, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("SaveDailyVoucher2")]
        public IActionResult SaveDailyVoucher2(List<Transactions> voucher)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var VoucherDatetime = DateTime.ParseExact(voucher.FirstOrDefault().TransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_globalshared.YearId_G != VoucherDatetime.Year)
            {
                var Msg = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يمكن حفظ تاريخ في سنة مالية مختلفة" };
                return Ok(Msg);
            }
            var result = _voucherService.SaveDailyVoucher2(voucher, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpGet("GenerateVoucherNumber")]
        public IActionResult GenerateVoucherNumber(int Type)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var Value = _voucherService.GenerateVoucherNumber(Type, _globalshared.BranchId_G, _globalshared.YearId_G).Result;
            var NewValue = string.Format("{0:000000}", Value);
            var generatevalue = new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = NewValue };
            return Ok(generatevalue);
        }
        [HttpGet("GenerateVoucherNumberNew")]

        public ActionResult GenerateVoucherNumberNew(int Type)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.GenerateVoucherNumberNewPro(Type, _globalshared.BranchId_G, _globalshared.YearId_G, Type, Con ?? "").Result;
            var generatevalue = new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = result };
            return Ok(generatevalue);
        }
        [HttpGet("GenerateVoucherNumberOpening")]
        public IActionResult GenerateVoucherNumberOpening(int Type)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var Value = _voucherService.GenerateVoucherNumberOpening(Type, _globalshared.BranchId_G, _globalshared.YearId_G).Result;
            var NewValue = string.Format("{0:000000}", Value);
            var generatevalue = new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = NewValue };
            return Ok(generatevalue);
        }
        [HttpGet("GenerateVoucherNumberClosing")]
        public IActionResult GenerateVoucherNumberClosing(int Type)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var Value = _voucherService.GenerateVoucherNumberClosing(Type, _globalshared.BranchId_G, _globalshared.YearId_G).Result;
            var NewValue = string.Format("{0:000000}", Value);
            var generatevalue = new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = NewValue };
            return Ok(generatevalue);
        }
        [HttpGet("GetAllTransByVoucherId")]
        public IActionResult GetAllTransByVoucherId(int? voucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            if (voucherId == 0)
            {
                return null;
            }
            else
            {


                return Ok(_voucherService.GetAllTransByVoucherId(voucherId));
            }
        }

        //عرض دائن اولا
        [HttpGet("GetAllTransByVoucherIddaen")]
        public IActionResult GetAllTransByVoucherIddaen(int? voucherId)
        {
            if (voucherId == 0)
            {
                return null;
                // return GetAllTransByVoucherIddaen(voucherId);
            }
            else
            {
                IEnumerable<TransactionsVM> test = _voucherService.GetAllTransByVoucherId(voucherId).Result;
                test = test.OrderBy(a => a.DepitOrCreditName);
                return Ok(test);
            }
            // return Ok(_voucherService.GetAllTransByVoucherId(voucherId));
        }
        //عرض مدين اولا
        [HttpGet("GetAllTransByVoucherIdmaden")]
        public IActionResult GetAllTransByVoucherIdmaden(int? voucherId, string Dipt = "")
        {

            IEnumerable<TransactionsVM> test = _voucherService.GetAllTransByVoucherId(voucherId).Result;
            test = test.OrderByDescending(a => a.DepitOrCreditName);

            return Ok(test);
            // return Ok(_voucherService.GetAllTransByVoucherId(voucherId));
        }
        [HttpPost("PostVouchers")]
        public IActionResult PostVouchers(List<Invoices> vouchers)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.PostVouchers(vouchers, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("PostVouchersCustody")]
        public IActionResult PostVouchersCustody(List<Custody> vouchers)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.PostVouchersCustody(vouchers, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("PostVouchersCheckBox")]
        public IActionResult PostVouchersCheckBox([FromForm]List<Int32> voucherIds)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.PostVouchersCheckBox(voucherIds, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }

        [HttpPost("PostBackVouchers")]
        public IActionResult PostBackVouchers(List<Invoices> vouchers)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var result = _voucherService.PostBackVouchers(vouchers, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("PostBackVouchersCustody")]
        public IActionResult PostBackVouchersCustody(List<Custody> vouchers)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.PostBackVouchersCustody(vouchers, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("CancelPostVouchers")]
        public IActionResult CancelPostVouchers(List<Invoices> vouchers)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);


            var result = _voucherService.CancelPostVouchers(vouchers, _globalshared.UserId_G, _globalshared.BranchId_G, _globalshared.YearId_G);
            return Ok(result);
        }
        [HttpPost("DeleteVoucher")]
        public IActionResult DeleteVoucher(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.DeleteVoucher(VoucherId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("SaveVoucherAlarmDate")]
        public IActionResult SaveVoucherAlarmDate(int VoucherId, string VoucherAlarmDate)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.SaveVoucherAlarmDate(VoucherId, VoucherAlarmDate, _globalshared.UserId_G, _globalshared.BranchId_G);

            return Ok(result);
        }
        [HttpPost("SaveVueDate")]
        public IActionResult SaveVueDate(int VoucherId, string VueDate)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _voucherService.SaveVueDate(VoucherId, VueDate, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpGet("GetProjectManagerRevene")]
        public IActionResult GetProjectManagerRevene(int? ManagerId, string FromDate, string ToDate)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_voucherService.GetProjectManagerRevene(ManagerId, FromDate, ToDate, _globalshared.BranchId_G, _globalshared.YearId_G));
        }

        [HttpPost("SaveServicesPrice")]
        public IActionResult SaveServicesPrice(Acc_Services_Price services_Price)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _servicesPriceService.SaveService(services_Price, _globalshared.UserId_G, _globalshared.BranchId_G,null);
            return Ok(result);
        }
        [HttpPost("SaveServicePriceWithDetails")]
        public IActionResult SaveServicePriceWithDetails(Services_PriceWithDetails services_PriceWithDetails)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _servicesPriceService.SaveService(services_PriceWithDetails?.services_price??new Acc_Services_Price(), _globalshared.UserId_G, _globalshared.BranchId_G, services_PriceWithDetails?.details);
            return Ok(result);
        }

        [HttpPost("DeleteService")]
        public IActionResult DeleteService(int? servicesId)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _servicesPriceService.DeleteService(servicesId, _globalshared.UserId_G, _globalshared.BranchId_G);

            return Ok(result);
        }
        [HttpGet("GetAllServicesPrice")]
        public IActionResult GetAllServicesPrice()
        {
            return Ok(_servicesPriceService.GetAllServicesPrice());
        }
        [HttpGet("GetServicesPriceByProjectId")]
        public IActionResult GetServicesPriceByProjectId(int? param)
        {
            return Ok(_servicesPriceService.GetServicesPriceByProjectId(param));
        }
        [HttpGet("GetServicesPriceByServiceId")]
        public IActionResult GetServicesPriceByServiceId(int ServiceId)
        {
            return Ok(_servicesPriceService.GetServicesPriceByServiceId(ServiceId));
        }
        [HttpGet("FillAllServicePrice")]
        public IActionResult FillAllServicePrice()
        {
            var someProject = _servicesPriceService.GetAllServicesPrice().Result.Select(s => new {
                Id = s.ServicesId,
                Name = s.ServicesName,
                ItemCode=s.ItemCode,
            });
            return Ok(someProject);
        }
        [HttpPost("FillAllAlarmVoucher")]
        public IActionResult FillAllAlarmVoucher(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            voucherFilterVM.Type = 2;
            var someVoucher = _voucherService.GetAllAlarmVoucher(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};

            var AlarmVoucher = someVoucher.Select(s => new {
                Id = s.InvoiceId,
                Name = " فاتورة رقم  " + s.InvoiceNumber + " - " + s.CustomerName
            });
            //var result2 = new ContentResult
            //{
            //    Content = serializer.Serialize(AlarmVoucher),
            //    ContentType = "application/json"
            //};
            //return Ok(AlarmVoucher);
            return Ok(AlarmVoucher);

        }
        [HttpPost("FillAllNotiVoucher")]
        public IActionResult FillAllNotiVoucher(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            voucherFilterVM.Type = 2;

            var Accyear = _globalshared.YearId_G;
            if (voucherFilterVM.PrevInvoices == true)
            {
                Accyear = _globalshared.YearId_G - 1;
            }
            var someVoucher = _voucherService.GetAllNotioucher(voucherFilterVM, _globalshared.BranchId_G, Accyear).Result.ToList();
            var AlarmVoucher = someVoucher.Select(s => new {
                Id = s.InvoiceId,
                Name = " فاتورة رقم  " + s.InvoiceNumber + " - " + s.CustomerName
            });
            return Ok(AlarmVoucher);

        }
        [HttpPost("FillAllNotiPurchaseVoucher")]
        public IActionResult FillAllNotiPurchaseVoucher(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            voucherFilterVM.Type = 1;
            var someVoucher = _voucherService.GetAllNotioucher(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            //var serializer = new JavaScriptSerializer();
            //serializer.MaxJsonLength = Int32.MaxValue;
            //var result = new ContentResult
            //{
            //    Content = serializer.Serialize(someVoucher),
            //    ContentType = "application/json"
            //};

            var Voucher = someVoucher.Select(s => new {
                Id = s.InvoiceId,
                Name = " فاتورة رقم  " + s.InvoiceNumber + " - " + s.SupplierName
            });
            //var result2 = new ContentResult
            //{
            //    Content = serializer.Serialize(Voucher),
            //    ContentType = "application/json"
            //};
            return Ok(Voucher);
        }
        [HttpGet("FillServicesPriceByProjectId")]
        public IActionResult FillServicesPriceByProjectId(int Param)
        {
            var someProject = _servicesPriceService.GetServicesPriceByProjectId(Param).Result.Select(s => new {
                Id = s.ServicesId,
                Name = s.ServicesName
            });
            return Ok(someProject);
        }
        [HttpGet("GetServicesPriceByProjectId2")]
        public IActionResult GetServicesPriceByProjectId2(int? param, int? param2)
        {
            return Ok(_servicesPriceService.GetServicesPriceByProjectId2(param, param2));
        }
        [HttpGet("GetServicePriceByProject_Search")]
        public IActionResult GetServicePriceByProject_Search(int? Project1, int? Project2, string? ServiceName, string? ServiceDesc, decimal? Amount)
        {

            return Ok(_servicesPriceService.GetServicePriceByProject_Search(Project1, Project2, ServiceName??"", ServiceDesc??"", Amount));
        }
        [HttpGet("GetServicesPriceByParentId")]
        public IActionResult GetServicesPriceByParentId(int? ParentId)
        {
            return Ok(_servicesPriceService.GetServicesPriceByParentId(ParentId));
        }
        [HttpGet("GetServicesPriceVouByParentId")]
        public IActionResult GetServicesPriceVouByParentId(int? ParentId,int? offerid)
        {
            return Ok(_servicesPriceOfferService.GetServicesPriceByParentId(ParentId, offerid));
        }
        [HttpGet("GetServicesPriceVouByParentIdAndContract")]
        public IActionResult GetServicesPriceVouByParentIdAndContract(int? ParentId, int? ContractId)
        {
            return Ok(_servicesPriceOfferService.GetServicesPriceByParentIdAndContractId(ParentId, ContractId));
        }
        [HttpGet("GetServicesPriceVouByParentIdAndInvoiceId")]
        public IActionResult GetServicesPriceVouByParentIdAndInvoiceId(int? ParentId, int? InvoiceId)
        {
            return Ok(_servicesPriceOfferService.GetServicesPriceVouByParentIdAndInvoiceId(ParentId, InvoiceId));
        }
        [HttpGet("GetServicesPriceAmountByServicesId")]
        public IActionResult GetServicesPriceAmountByServicesId(int? ServicesId)
        {
            return Ok(_servicesPriceService.GetServicesPriceAmountByServicesId(ServicesId));
        }

        [HttpGet("PrintVoucher")]
        public IActionResult PrintVoucher(int VoucherId, int ReportType)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;

            DataTable voucher = _voucherService.ReceiptCashingPaying(VoucherId, Con).Result;
            InvoicesVM InvoicesVM = _voucherService.GetVoucherById(VoucherId).Result;
            //costCenterNam = _CostCenterservice.GetCostCenterByProId(InvoicesVM.ProjectId.Value);

            var costCenterNam = "";
            if (InvoicesVM.VoucherDetails[0].CostCenterId != null)
            {

                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
            }
            else
            {
                costCenterNam = "بدون";
            }

            List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(VoucherId).Result.ToList();
            if (ReportType == 1)//النموذج الاول للفاتورة
                ReportPDF = inVoiceReports.GenInvoice(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
            else if (ReportType == 2)///النموذج التانى للفاتورة
                ReportPDF = inVoiceReports.GenInvoice2(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
            else if (ReportType == 4)///فاتورة صغيره
                ReportPDF = inVoiceReports.GenInvoiceN_S(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam??"");
            else if (ReportType == 5)///مسودة صغيره
                ReportPDF = inVoiceReports.GenInvoiceN_SDraft(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam ?? "");
            else //New
                ReportPDF = inVoiceReports.GenInvoiceN(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam??"");

            //dawoudprint
            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;
            return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = FilePathReturn });
        }


        //[HttpGet]
        //public IActionResult PrintVoucher(int VoucherId, int ReportType)
        //{
        //    int orgId = _BranchesService.GetOrganizationId(BranchId);

        //    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);

        //    DataTable voucher = _voucherService.ReceiptCashingPaying(VoucherId, Con);
        //    InvoicesVM InvoicesVM = _voucherService.GetVoucherById(VoucherId);
        //    //costCenterNam = _CostCenterservice.GetCostCenterByProId(InvoicesVM.ProjectId.Value);

        //    var costCenterNam = "";
        //    if (InvoicesVM.VoucherDetails[0].CostCenterId != null)
        //    {

        //        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //    }
        //    else
        //    {
        //        costCenterNam = "بدون";
        //    }

        //    List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(VoucherId).ToList();
        //    if (ReportType == 1)//النموذج الاول للفاتورة
        //        ReportPDF = inVoiceReports.GenInvoice(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
        //    else if (ReportType == 2)///النموذج التانى للفاتورة
        //        ReportPDF = inVoiceReports.GenInvoice2(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
        //    else if (ReportType == 4)///فاتورة صغيره
        //    {
        //        ReportPDF = inVoiceReports.GenInvoiceN_S(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);

        //        CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, Lang);

        //        string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());
        //        var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //        var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(BranchId).OrgDataIsRequired;
                        //if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;

        //        ViewData["VoucherDetailsVM"] = VoucherDetailsVM;
        //        ViewData["voucher"] = voucher;
        //        ViewData["InvoicesVM"] = InvoicesVM;
        //        ViewData["costCenterNam"] = costCenterNam;
        //        ViewData["CustomerVM_VD"] = CustomerVM;
        //        ViewData["ValueNumString_VD"] = ValueNumString;


        //        ViewData["Branch_VD"] = objBranch;

        //        ViewData["OrgIsRequired_VD"] = OrgIsRequired;

        //        string Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
        //        ViewData["Date"] = Date;

        //        var objOrganization2 = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //        if (objOrganization2 != null)
        //            ViewData["Org_VD"] = objOrganization2;
        //        else
        //            ViewData["Org_VD"] = null;


        //        DateTime Date2 = InvoicesVM.AddDate ?? DateTime.Now;
        //        string ActionDate = Date2.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
        //        var SupplierName = objOrganization.NameAr.TrimStart();
        //        SupplierName = SupplierName.TrimEnd();
        //        string qrBarcodeHash = QRCodeEncoder.encode(
        //            new Seller_Inv(SupplierName),
        //            new TaxNumber_Inv(objOrganization.TaxCode),
        //            new InvoiceDate_Inv(ActionDate),
        //            new TotalAmount_Inv(InvoicesVM.TotalValue.ToString()),
        //            new TaxAmount_Inv(InvoicesVM.TaxAmount.ToString())
        //        );
        //        try
        //        {
        //            string ImgReturn = "";
        //            QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrBarcodeHash, QRCodeGenerator.ECCLevel.Q, true);
        //            QRCode qrCode = new QRCode(qrCodeData);
        //            using (Bitmap bitMap = qrCode.GetGraphic(20))
        //            {
        //                using (MemoryStream ms = new MemoryStream())
        //                {
        //                    bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //                    byte[] byteImage = ms.ToArray();
        //                    System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
        //                    ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
        //                }
        //            }
        //            ViewData["QRCodeString_VD"] = ImgReturn;

        //        }
        //        catch (Exception ex)
        //        {
        //            ViewData["QRCodeString_VD"] = null;
        //        }



        //        return PartialView("_simpleInvoice");
        //    }
        //    else //New
        //        ReportPDF = inVoiceReports.GenInvoiceN(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);
        //    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //    if (!Directory.Exists(existTemp))
        //    {
        //        Directory.CreateDirectory(existTemp);
        //    }
        //    //File  
        //    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        //    //create and set PdfReader  
        //    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        //    //if(InvoicesVM.QRCodeNum!="")
        //    //{
        //    //    QRCodeFun(existTemp, FileName, InvoicesVM.QRCodeNum);
        //    //    string FilePathReturn = @"TempFiles/" + "_B_" + FileName;

        //    //    return Content(FilePathReturn);
        //    //}
        //    //else
        //    //{
        //    //    string FilePathReturn = @"TempFiles/" + FileName;

        //    //    return Content(FilePathReturn);
        //    //}



        //    string FilePathReturn = @"TempFiles/" + FileName;

        //    return Content(FilePathReturn);

        //    //return file 

        //}


        //public IActionResult PrintCreditNoti(int VoucherId, int ReportType)
        //{
        //    int orgId = _BranchesService.GetOrganizationId(BranchId);
        //    //List<VoucherDetailsVM> VoucherDetailsVM2 = _voucherService.GetAllDetailsByInvoiceId(0).ToList();

        //    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);

        //    DataTable voucher = new DataTable();
        //    if (ReportType == 29)
        //        voucher = _voucherService.ReceiptCashingPayingNoti(VoucherId, Con);
        //    else if (ReportType == 30)
        //        voucher = _voucherService.ReceiptCashingPayingNotiDepit(VoucherId, Con);
        //    else
        //        voucher = _voucherService.ReceiptCashingPayingNoti(VoucherId, Con);

        //    InvoicesVM InvoicesVM = _voucherService.GetVoucherById(VoucherId);
        //    var costCenterNam = "";
        //    if (InvoicesVM.VoucherDetails[0].CostCenterId != null)
        //    {

        //        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //    }
        //    else
        //    {
        //        costCenterNam = "بدون";
        //    }

        //    //for (int i = 0; i < voucher.Rows.Count; i++)
        //    //{
        //    //    try
        //    //    {
        //    //        var InvId = Convert.ToInt32(voucher.Rows[i]["InvoiceId"].ToString());

        //    //        var TempVoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(InvId).ToList();
        //    //        var Vouchers = VoucherDetailsVM.Union(TempVoucherDetailsVM);
        //    //        VoucherDetailsVM = Vouchers.ToList();

        //    //    }
        //    //    catch (Exception ex)
        //    //    {

        //    //    }

        //    //}
        //    var InvId2 = Convert.ToInt32(voucher.Rows[0]["InvoiceId"].ToString());
        //    List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(InvId2).ToList();

        //    if (ReportType == 29)
        //        ReportPDF = inVoiceReports.GenInvoiceNoti(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);
        //    else if (ReportType == 30)
        //        ReportPDF = inVoiceReports.GenInvoiceNotiDepit(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);
        //    else
        //        ReportPDF = inVoiceReports.GenInvoiceNoti(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);
        //    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //    if (!Directory.Exists(existTemp))
        //    {
        //        Directory.CreateDirectory(existTemp);
        //    }
        //    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        //    System.IO.File.WriteAllBytes(FilePath, ReportPDF);

        //    string FilePathReturn = @"TempFiles/" + FileName;

        //    return Content(FilePathReturn);

        //}


        ////public IActionResult QRCodeFun(string URL, string Filename, string NumberCode)
        ////{
        ////    var result = _projectservice.BarcodePDF(1, _globalshared.UserId_G);
        ////    string File = Path.Combine(URL, Filename);
        ////    string newFile = Path.Combine(URL, "_B_" + Filename);


        ////    // open the reader
        ////    PdfReader reader = new PdfReader(File);
        ////    iTextSharp.text.Rectangle size = reader.GetPageSizeWithRotation(1);
        ////    Document document = new Document(size);

        ////    // open the writer
        ////    FileStream fs = new FileStream(newFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        ////    PdfWriter writer = PdfWriter.GetInstance(document, fs);
        ////    document.Open();


        ////    for (var i = 1; i <= reader.NumberOfPages; i++)
        ////    {
        ////        document.NewPage();
        ////        // the pdf content
        ////        PdfContentByte cb = writer.DirectContent;

        ////        //var bc = new Barcode128
        ////        //{
        ////        //    Code = NumberCode,
        ////        //    TextAlignment = Element.ALIGN_CENTER,
        ////        //    StartStopText = true,
        ////        //    CodeType = Barcode.CODE128,
        ////        //    ChecksumText = true,
        ////        //    GenerateChecksum = true,
        ////        //    Extended = false
        ////        //};




        ////        string fontpath = Environment.GetEnvironmentVariable("SystemRoot") +
        ////         "\\fonts\\tahoma.ttf";
        ////        BaseFont bf = BaseFont.CreateFont(fontpath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        ////        Font tahomaFont = new Font(bf, 8, Font.NORMAL, BaseColor.DARK_GRAY);

        ////        cb.SetColorFill(BaseColor.DARK_GRAY);
        ////        cb.SetFontAndSize(bf, 8);

        ////        //ColumnText ct = new ColumnText(writer.DirectContent);
        ////        //ct.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
        ////        //ct.SetSimpleColumn(0, 0, 170, 120, 8, Element.ALIGN_LEFT);

        ////        //var chunk = new Chunk(OrgEngName, tahomaFont);
        ////        //var chunk2 = new Chunk(ProjectNo + " - " + CustomerName, tahomaFont);


        ////        //ct.AddElement(chunk2);

        ////        //ct.AddElement(chunk);

        ////        //ct.Go();

        ////        //iTextSharp.text.Image img = bc.CreateImageWithBarcode(cb, BaseColor.BLACK, BaseColor.BLACK);
        ////        //var barCodeRect = new iTextSharp.text.Rectangle(bc.BarcodeSize);
        ////        //iTextSharp.text.Rectangle tempRect;
        ////        //tempRect = new iTextSharp.text.Rectangle(0, 0, 140, 40);//(,,3rd,toool)

        ////        //img.ScaleAbsolute(tempRect);
        ////        //img.SetAbsolutePosition(30, 30);
        ////        //cb.AddImage(img);

        ////        var bc2 = new BarcodeQRCode(NumberCode, 50, 50, null);
        ////        iTextSharp.text.Image img1 = bc2.GetImage();
        ////        iTextSharp.text.Rectangle tempRect2;
        ////        tempRect2 = new iTextSharp.text.Rectangle(0, 0, 150, 150);//(,,3rd,toool)

        ////        img1.ScaleAbsolute(tempRect2);
        ////        img1.SetAbsolutePosition(40,100);

        ////        //img1.SetAbsolutePosition((size.Width - 150), 0);
        ////        cb.AddImage(img1);


        ////        PdfImportedPage page = writer.GetImportedPage(reader, i);
        ////        cb.AddTemplate(page, 0, 0);
        ////    }


        ////    // close the streams and voilá the file should be changed :)
        ////    document.Close();
        ////    fs.Close();
        ////    writer.Close();
        ////    reader.Close();

        ////    return Ok(new {result.StatusCode, result.ReasonPhrase });

        ////}


        //public IActionResult PrintVoucherMail(int VoucherId, int ReportType)
        //{
        //    int orgId = _BranchesService.GetOrganizationId(BranchId);

        //    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);

        //    DataTable voucher = _voucherService.ReceiptCashingPaying(VoucherId, Con);
        //    InvoicesVM InvoicesVM = _voucherService.GetVoucherById(VoucherId);
        //    var costCenterNam = "";
        //    if (InvoicesVM.VoucherDetails[0].CostCenterId != null)
        //    {

        //        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //    }
        //    else
        //    {
        //        costCenterNam = "بدون";
        //    }
        //    //var costCenterNam = _CostCenterservice.GetCostCenterByProId(InvoicesVM.ProjectId.Value);
        //    List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(VoucherId).ToList();
        //    if (ReportType == 1)//النموذج الاول للفاتورة
        //        ReportPDF = inVoiceReports.GenInvoice(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
        //    else if (ReportType == 2)///النموذج التانى للفاتورة
        //        ReportPDF = inVoiceReports.GenInvoice2(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);

        //    else //New
        //        ReportPDF = inVoiceReports.GenInvoiceN(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);
        //    string existTemp = HttpContext.Server.MapPath(@"~\Uploads\CustomersMails\");

        //    if (!Directory.Exists(existTemp))
        //    {
        //        Directory.CreateDirectory(existTemp);
        //    }
        //    //File  
        //    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //    string FilePath = HttpContext.Server.MapPath(@"~\Uploads\CustomersMails\") + FileName;

        //    //create and set PdfReader  
        //    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        //    //return file 
        //    string FilePathReturn = @"Uploads/CustomersMails/" + FileName;
        //    return Content(FilePathReturn);
        //}


        //public IActionResult PrintVoucherRet(int VoucherId, int ReportType)
        //{
        //    int orgId = _BranchesService.GetOrganizationId(BranchId);

        //    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);

        //    DataTable voucher = _voucherService.ReceiptCashingPaying(VoucherId, Con);
        //    InvoicesVM InvoicesVM = _voucherService.GetVoucherById(VoucherId);
        //    //var costCenterNam = _CostCenterservice.GetCostCenterByProId(InvoicesVM.ProjectId.Value);
        //    var costCenterNam = "";
        //    if (InvoicesVM.VoucherDetails[0].CostCenterId != null)
        //    {

        //        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //    }
        //    else
        //    {
        //        costCenterNam = "بدون";
        //    }
        //    List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(VoucherId).ToList();
        //    if (ReportType == 1)//النموذج الاول للفاتورة
        //        ReportPDF = inVoiceReports.GenInvoice(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
        //    else if (ReportType == 2)///النموذج التانى للفاتورة
        //        ReportPDF = inVoiceReports.GenInvoice2(VoucherDetailsVM, objOrganization, InvoicesVM, voucher);
        //    else if (ReportType == 4)//
        //        ReportPDF = inVoiceReports.GenInvoiceNRet_S(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);

        //    else //New
        //        ReportPDF = inVoiceReports.GenInvoiceNRet(VoucherDetailsVM, objOrganization, InvoicesVM, voucher, costCenterNam);
        //    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //    if (!Directory.Exists(existTemp))
        //    {
        //        Directory.CreateDirectory(existTemp);
        //    }
        //    //File  
        //    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        //    //create and set PdfReader  
        //    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        //    //return file 
        //    string FilePathReturn = @"TempFiles/" + FileName;
        //    return Content(FilePathReturn);
        //}

        ////سند قبض
        ////public IActionResult GetReport(int VoucherId)
        ////{
        ////    // InvoicesVM res=_voucherService.GetVoucherById(VoucherId);
        ////    DataTable res1 = _voucherService.ReceiptCashingPaying(VoucherId, Con);
        ////    int orgId = _BranchesService.GetOrganizationId(BranchId);
        ////    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        ////    string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };

        ////    if (res1 != null && res1.Rows.Count > 0)
        ////        ReportPDF = Bayanatech.TameerUI.pdfHandler.ReportsOf7sabat.receiptVoucher(res1, infoDoneTasksReport);
        ////    else
        ////        ReportPDF = new byte[0];
        ////    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        ////    if (!Directory.Exists(existTemp))
        ////    {
        ////        Directory.CreateDirectory(existTemp);
        ////    }
        ////    //File  
        ////    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        ////    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        ////    //create and set PdfReader  
        ////    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        ////    //return file 
        ////    string FilePathReturn = @"TempFiles/" + FileName;
        ////    return Content(FilePathReturn);
        ////}

        [HttpGet("GetReport")]

        public IActionResult GetReport(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            VoucherReportVM _entryVoucherReportVM = new VoucherReportVM();
            InvoicesVM res = _voucherService.GetVoucherById(VoucherId).Result;
            DataTable res1 = _voucherService.ReceiptCashingPaying(VoucherId, Con).Result;
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };


            List<VoucherVMDatatable> VMList = new List<VoucherVMDatatable>();
            for (int i = 0; i < res1.Rows.Count; i++)
            {
                VoucherVMDatatable invoice = new VoucherVMDatatable();
                invoice.InvoiceNumber = res1.Rows[i]["InvoiceNumber"].ToString();
                invoice.JoNo = res1.Rows[i]["JoNo"].ToString();
                invoice.Date = res1.Rows[i]["Date"].ToString();
                invoice.HijriDate = res1.Rows[i]["HijriDate"].ToString();
                invoice.CustomerName = res1.Rows[i]["CustomerName"].ToString();
                invoice.InvoiceValueText = res1.Rows[i]["InvoiceValueText"].ToString();
                invoice.TotalValue = res1.Rows[i]["TotalValue"].ToString();
                invoice.RecevierTxt = res1.Rows[i]["RecevierTxt"].ToString();
                invoice.PayType = res1.Rows[i]["PayType"].ToString();
                invoice.MoneyOrderDate = res1.Rows[i]["MoneyOrderDate"].ToString();
                invoice.CheckDate = res1.Rows[i]["CheckDate"].ToString();
                invoice.BankName = res1.Rows[i]["BankName"].ToString();
                invoice.Description = res1.Rows[i]["Description"].ToString();
                invoice.Notes = res1.Rows[i]["Notes"].ToString();
                invoice.CostCenterName = res1.Rows[i]["CostCenterName"].ToString();
                invoice.FullName = res1.Rows[i]["FullName"].ToString();
                invoice.SupplierName = res1.Rows[i]["SupplierName"].ToString();
                invoice.ToInvoiceId = res1.Rows[i]["ToInvoiceId"].ToString();

                VMList.Add(invoice);
            }
            string ValueNumString2 = ConvertNumToString(res1.Rows[0]["TotalValue"].ToString());
            _entryVoucherReportVM.NumString = ValueNumString2;
            _entryVoucherReportVM.VoucherVM = VMList;

            string Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            _entryVoucherReportVM.DateTimeNow = Date;

            var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
            _entryVoucherReportVM.DateTimeNow = Date;
            _entryVoucherReportVM.Branch = objBranch;

            var objOrganization2 = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
            _entryVoucherReportVM.Org_VD = objOrganization2;
            return Ok(_entryVoucherReportVM);
        }


        //// سند صرف
        ////public IActionResult PrintPayVoucher(int VoucherId)
        ////{
        ////    // InvoicesVM res=_voucherService.GetVoucherById(VoucherId);
        ////    DataTable res1 = _voucherService.ReceiptCashingPaying(VoucherId, Con);
        ////    int SupplierId = _voucherService.GetVoucherById(VoucherId).SupplierId??0;
        ////    string SupplierName = "";
        ////    if (SupplierId==0)
        ////    {
        ////        SupplierName = "";
        ////    }
        ////    else
        ////    {
        ////        SupplierName= _acc_SuppliersService.GetSuppNameBySuppId(SupplierId, "rtl", _globalshared.BranchId_G);
        ////    }
        ////    int orgId = _BranchesService.GetOrganizationId(BranchId);
        ////    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        ////    string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };

        ////    if (res1 != null && res1.Rows.Count > 0) ReportPDF = Bayanatech.TameerUI.pdfHandler.ReportsOf7sabat.PayVoucher(res1, infoDoneTasksReport, SupplierName);
        ////    else ReportPDF = new byte[0];
        ////    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        ////    if (!Directory.Exists(existTemp))
        ////    {
        ////        Directory.CreateDirectory(existTemp);
        ////    }
        ////    //File  
        ////    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        ////    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        ////    //create and set PdfReader  
        ////    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        ////    //return file 
        ////    string FilePathReturn = @"TempFiles/" + FileName;
        ////    return Content(FilePathReturn);
        ////}

        ////سند صرف جديد
        //public IActionResult PrintPayVoucher(int VoucherId)
        //{
        //    // InvoicesVM res=_voucherService.GetVoucherById(VoucherId);
        //    DataTable res1 = _voucherService.ReceiptCashingPaying(VoucherId, Con);
        //    int SupplierId = _voucherService.GetVoucherById(VoucherId).SupplierId ?? 0;
        //    string SupplierName = "";
        //    if (SupplierId == 0)
        //    {
        //        SupplierName = "";
        //    }
        //    else
        //    {
        //        SupplierName = _acc_SuppliersService.GetSuppNameBySuppId(SupplierId, "rtl", _globalshared.BranchId_G);
        //    }
        //    int orgId = _BranchesService.GetOrganizationId(BranchId);
        //    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        //    string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };

        //    //if (res1 != null && res1.Rows.Count > 0) ReportPDF = Bayanatech.TameerUI.pdfHandler.ReportsOf7sabat.PayVoucher(res1, infoDoneTasksReport, SupplierName);
        //    //else ReportPDF = new byte[0];
        //    //string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //    string ValueNumString2 = ConvertNumToString(res1.Rows[0]["TotalValue"].ToString());

        //    ViewData["NumString"] = ValueNumString2;

        //    ViewData["res1"] = res1;
        //    ViewData["SupplierName"] = SupplierName;


        //    string Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
        //    ViewData["Date"] = Date;
        //    var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //    ViewData["Branch"] = objBranch;

        //    var objOrganization2 = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //    if (objOrganization2 != null)
        //        ViewData["Org_VD"] = objOrganization2;
        //    else
        //        ViewData["Org_VD"] = null;




        //    return PartialView("_PayvoucherPDF");
        //}

        [HttpPost("PrintPayVoucherRet")]
        public IActionResult PrintPayVoucherRet(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            // InvoicesVM res=_voucherService.GetVoucherById(VoucherId);
            DataTable res1 = _voucherService.ReceiptCashingPaying(VoucherId, Con).Result;
            int SupplierId = _voucherService.GetVoucherById(VoucherId).Result.SupplierId ?? 0;
            string SupplierName = _acc_SuppliersService.GetSuppNameBySuppId(SupplierId, "rtl", _globalshared.BranchId_G);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };

            if (res1 != null && res1.Rows.Count > 0) ReportPDF = ReportsOf7sabat.PayVoucherRet(res1, infoDoneTasksReport, SupplierName);
            else ReportPDF = new byte[0];
            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;
            return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = FilePathReturn });
        }

        ////تجميع السندات
        ////public IActionResult PrintGetVoucherRpt()
        ////{
        ////    int orgId = _BranchesService.GetOrganizationId(BranchId);

        ////    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        ////    string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };

        ////    List<InvoicesVM> InvoicesVM = _voucherService.GetVoucherRpt(BranchId, _globalshared.YearId_G).ToList();

        ////    ReportPDF = ReportsOf7sabat.PrintGetVoucherRpt(InvoicesVM, infoDoneTasksReport);
        ////    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        ////    if (!Directory.Exists(existTemp))
        ////    {
        ////        Directory.CreateDirectory(existTemp);
        ////    }
        ////    //File  
        ////    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        ////    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        ////    //create and set PdfReader  
        ////    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        ////    //return file 
        ////    string FilePathReturn = @"TempFiles/" + FileName;
        ////    return Content(FilePathReturn);
        ////}


        [HttpGet("PrintGetVoucherRpt")]

        public IActionResult PrintGetVoucherRpt()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            ReportGridVM _reportGrid = new ReportGridVM();
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };

            List<InvoicesVM> InvoicesVM = _voucherService.GetVoucherRpt(_globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();

            _reportGrid.InvoicesVM = InvoicesVM;

            string Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            _reportGrid.DateTimeNow = Date;

            var objOrganization2 = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
            _reportGrid.Org_VD = objOrganization2;
            var branch = _BranchesService.GetBranchById(_globalshared.BranchId_G).Result;
            _reportGrid.BranchName = branch.NameAr;
            return Ok(_reportGrid);
        }






        //تقرير سندات الصرف ، القبض ، واليومية   
      [HttpPost("Printdiffrentvoucher")]

        public IActionResult Printdiffrentvoucher(VoucherFilterVM voucherFilterVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };



            List<InvoicesVM> InvoicesVM = _voucherService.GetVoucherRpt(_globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            var someVoucher = _voucherService.GetAllVouchers(voucherFilterVM, _globalshared.BranchId_G, _globalshared.YearId_G).Result.ToList();
            if (voucherFilterVM.Type == 5)
            {
                ReportPDF = ReportsOf7sabat.PrintpayvoucherN(someVoucher, infoDoneTasksReport, voucherFilterVM.dateFrom, voucherFilterVM.dateTo);
            }
            else if (voucherFilterVM.Type == 6)
            {
                ReportPDF = ReportsOf7sabat.PrintRevoucherN(someVoucher, infoDoneTasksReport, voucherFilterVM.dateFrom, voucherFilterVM.dateTo);
            }
            else if (voucherFilterVM.Type == 8)
            {
                ReportPDF = ReportsOf7sabat.PrintEntryVoucher(someVoucher, infoDoneTasksReport, voucherFilterVM.dateFrom, voucherFilterVM.dateTo);
            }


            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;
            return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = FilePathReturn });
        }




        //سند قيود  يومية
        //public IActionResult DailyVoucherReport(int VoucherId)
        //{
        //    int orgId = _BranchesService.GetOrganizationId(BranchId);

        //    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        //    string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
        //    DataTable InvoicesVM = _voucherService.DailyVoucherReport(VoucherId, Con);
        //    ReportPDF = inVoiceReports.PrintDailyVoucherReport(InvoicesVM, infoDoneTasksReport);
        //    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //    if (!Directory.Exists(existTemp))
        //    {
        //        Directory.CreateDirectory(existTemp);
        //    }
        //    //File  
        //    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        //    //create and set PdfReader  
        //    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        //    //return file 
        //    string FilePathReturn = @"TempFiles/" + FileName;
        //    return Content(FilePathReturn);
        //}
        //قيد يومية جديد
        [HttpGet("DailyVoucherReport")]

        public IActionResult DailyVoucherReport(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            EntryVoucherReportVM _entryVoucherReportVM = new EntryVoucherReportVM();
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
            DataTable InvoicesVM = _voucherService.DailyVoucherReport(VoucherId, Con).Result;
            List<EntryVoucherVMDatatable> InvoicesVMList = new List<EntryVoucherVMDatatable>();
            for (int i = 0; i < InvoicesVM.Rows.Count; i++)
            {
                EntryVoucherVMDatatable invoice = new EntryVoucherVMDatatable();
                invoice.InvoiceNumber = InvoicesVM.Rows[i]["InvoiceNumber"].ToString();
                invoice.InvoiceDate = InvoicesVM.Rows[i]["InvoiceDate"].ToString();
                invoice.InvoiceHijriDate = InvoicesVM.Rows[i]["InvoiceHijriDate"].ToString();
                invoice.AccountCode = InvoicesVM.Rows[i]["AccountCode"].ToString();
                invoice.AccountNameAr = InvoicesVM.Rows[i]["AccountNameAr"].ToString();
                invoice.Depit = InvoicesVM.Rows[i]["Depit"].ToString();
                invoice.Credit = InvoicesVM.Rows[i]["Credit"].ToString();
                invoice.Notes = InvoicesVM.Rows[i]["Notes"].ToString();
                invoice.CostCenterNameAr = InvoicesVM.Rows[i]["CostCenterNameAr"].ToString();
                InvoicesVMList.Add(invoice);
            }
            _entryVoucherReportVM.InvoicesVM = InvoicesVMList;

            string Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            _entryVoucherReportVM.DateTimeNow = Date;

            var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
            _entryVoucherReportVM.Branch = objBranch;
            var objOrganization2 = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
            _entryVoucherReportVM.Org_VD = objOrganization2;
            return Ok(_entryVoucherReportVM);
        }

        [HttpGet("DailyVoucherReport_Custody")]
        public IActionResult DailyVoucherReport_Custody(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
            DataTable InvoicesVM = _voucherService.DailyVoucherReport(VoucherId, Con).Result;
            ReportPDF = inVoiceReports.PrintDailyVoucherReport_Custody(InvoicesVM, infoDoneTasksReport);
            //dawoudprint
            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;
            return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = FilePathReturn });
        }

        ////سند قيود  اقفال
        ////public IActionResult ClosingVoucherReport(int VoucherId)
        ////{
        ////    int orgId = _BranchesService.GetOrganizationId(BranchId);

        ////    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        ////    string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
        ////    DataTable InvoicesVM = _voucherService.DailyVoucherReport(VoucherId, Con);
        ////    ReportPDF = inVoiceReports.PrintClosingVoucherReport(InvoicesVM, infoDoneTasksReport);
        ////    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        ////    if (!Directory.Exists(existTemp))
        ////    {
        ////        Directory.CreateDirectory(existTemp);
        ////    }
        ////    //File  
        ////    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        ////    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        ////    //create and set PdfReader  
        ////    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        ////    //return file 
        ////    string FilePathReturn = @"TempFiles/" + FileName;
        ////    return Content(FilePathReturn);
        ////}

        [HttpGet("ClosingVoucherReport")]

        public IActionResult ClosingVoucherReport(int VoucherId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            EntryVoucherReportVM _entryVoucherReportVM = new EntryVoucherReportVM();
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
            DataTable InvoicesVM = _voucherService.DailyVoucherReport(VoucherId, Con).Result;
            List<EntryVoucherVMDatatable> InvoicesVMList = new List<EntryVoucherVMDatatable>();
            for (int i = 0; i < InvoicesVM.Rows.Count; i++)
            {
                EntryVoucherVMDatatable invoice = new EntryVoucherVMDatatable();
                invoice.InvoiceNumber = InvoicesVM.Rows[i]["InvoiceNumber"].ToString();
                invoice.InvoiceDate = InvoicesVM.Rows[i]["InvoiceDate"].ToString();
                invoice.InvoiceHijriDate = InvoicesVM.Rows[i]["InvoiceHijriDate"].ToString();
                invoice.AccountCode = InvoicesVM.Rows[i]["AccountCode"].ToString();
                invoice.AccountNameAr = InvoicesVM.Rows[i]["AccountNameAr"].ToString();
                invoice.Depit = InvoicesVM.Rows[i]["Depit"].ToString();
                invoice.Credit = InvoicesVM.Rows[i]["Credit"].ToString();
                invoice.Notes = InvoicesVM.Rows[i]["Notes"].ToString();
                invoice.CostCenterNameAr = InvoicesVM.Rows[i]["CostCenterNameAr"].ToString();
                InvoicesVMList.Add(invoice);
            }
            _entryVoucherReportVM.InvoicesVM = InvoicesVMList;

            string Date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            _entryVoucherReportVM.DateTimeNow = Date;

            var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
            _entryVoucherReportVM.Branch = objBranch;
            var objOrganization2 = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
            _entryVoucherReportVM.Org_VD = objOrganization2;
            return Ok(_entryVoucherReportVM);
        }

        [HttpGet("GetViewDetailsGrid")]
        public IActionResult GetViewDetailsGrid(int Type, int Status)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var SelectStetment = "";
            if (Status == 1)
            {
                SelectStetment = "select inv.InvoiceNumber as InvoiceNumber,trat.NameAr,sum(Credit) as Credit ,sum(Depit) as Depit , sum(Credit)-sum(Depit) as Diff from Acc_Transactions tra"
               + " join Acc_Invoices inv on tra.InvoiceId = inv.InvoiceId"
               + " join Acc_TransactionTypes trat on trat.TransactionTypeId=tra.Type"
               + " where tra.YearId = " + _globalshared.YearId_G + " and tra.IsDeleted = 0 and inv.IsDeleted = 0 group by tra.YearId,inv.InvoiceId,inv.InvoiceNumber,trat.NameAr";

            }
            else if (Status == 2)
            {
                SelectStetment = "select inv.InvoiceNumber as InvoiceNumber,trat.NameAr,sum(Credit) as Credit ,sum(Depit) as Depit , sum(Credit)-sum(Depit) as Diff from Acc_Transactions tra"
                + " join Acc_Invoices inv on tra.InvoiceId = inv.InvoiceId"
                + " join Acc_TransactionTypes trat on trat.TransactionTypeId=tra.Type"
                + " where tra.Type in(" + Type + ") and tra.YearId = " + _globalshared.YearId_G + " and tra.IsDeleted = 0 and inv.IsDeleted = 0 group by tra.YearId,inv.InvoiceId,inv.InvoiceNumber,trat.NameAr";

            }
            else if (Status == 3)
            {
                SelectStetment = "select inv.InvoiceNumber as InvoiceNumber,trat.NameAr,sum(Credit) as Credit ,sum(Depit) as Depit , sum(Credit)-sum(Depit) as Diff from Acc_Transactions tra"
                + " join Acc_Invoices inv on tra.InvoiceId = inv.InvoiceId"
                + " join Acc_TransactionTypes trat on trat.TransactionTypeId=tra.Type"
                + " where tra.Type in(" + Type + ") and tra.YearId = " + _globalshared.YearId_G + " and tra.IsDeleted = 0 and inv.IsDeleted = 0 group by tra.YearId,inv.InvoiceId,inv.InvoiceNumber,trat.NameAr"
                + " having sum(Credit)-sum(Depit) != 0";
            }
            else
            {
                SelectStetment = "select inv.InvoiceNumber as InvoiceNumber,trat.NameAr,sum(Credit) as Credit ,sum(Depit) as Depit , sum(Credit)-sum(Depit) as Diff from Acc_Transactions tra"
                + " join Acc_Invoices inv on tra.InvoiceId = inv.InvoiceId"
                + " join Acc_TransactionTypes trat on trat.TransactionTypeId=tra.Type"
                + " where tra.YearId = " + _globalshared.YearId_G + " and tra.IsDeleted = 0 and inv.IsDeleted = 0 group by tra.YearId,inv.InvoiceId,inv.InvoiceNumber,trat.NameAr"
                + " having sum(Credit)-sum(Depit) != 0";
            }

            var ViewDetails = _projectservice.GetViewDetailsGrid(Con, SelectStetment);

            return Ok(ViewDetails);

        }
        //سند الافتتاحى
        [HttpGet("OpeningVoucherReport")]

        public IActionResult OpeningVoucherReport(int VoucherId)
        {

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;

            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            string[] infoDoneTasksReport = { _globalshared.Lang_G == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
            DataTable InvoicesVM = _voucherService.OpeningVoucherReport(VoucherId, Con).Result;
            ReportPDF = inVoiceReports.PrintOpeningVoucherReport(InvoicesVM, infoDoneTasksReport);

            //dawoudprint
            string existTemp = System.IO.Path.Combine("TempFiles/");
            if (!Directory.Exists(existTemp))
            {
                Directory.CreateDirectory(existTemp);
            }
            string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
            string FilePath = System.IO.Path.Combine("TempFiles/", FileName);
            System.IO.File.WriteAllBytes(FilePath, ReportPDF);
            string FilePathReturn = "/TempFiles/" + FileName;
            return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = FilePathReturn });
        }
        ////حركة مراكز التكبفة

        //public IActionResult CostCenterMovementReport(int CostCenterId, string CostCenterName, string FromDate, string ToDate)
        //{


        //    int orgId = _BranchesService.GetOrganizationId(BranchId);

        //    var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        //    string[] infoDoneTasksReport = { Lang == "en" ? objOrganization.NameEn : objOrganization.NameAr, objOrganization.LogoUrl, objOrganization.Address, objOrganization.Email, objOrganization.Fax, objOrganization.Mobile, objOrganization.IsFooter, objOrganization.WebSite, objOrganization.TaxCode };
        //    List<CostCentersVM> CostCenters = _CostCenterservice.GetCostCenterTransaction(BranchId, Lang, CostCenterId, FromDate, ToDate, _globalshared.YearId_G).ToList();
        //    ReportPDF = ReportsOf7sabat.PrintCostCenterMovementReport(CostCenters, FromDate, ToDate, CostCenterName, infoDoneTasksReport);
        //    string existTemp = HttpContext.Server.MapPath(@"~\TempFiles\");

        //    if (!Directory.Exists(existTemp))
        //    {
        //        Directory.CreateDirectory(existTemp);
        //    }
        //    //File  
        //    string FileName = "PDFFile_" + DateTime.Now.Ticks.ToString() + ".pdf";
        //    string FilePath = HttpContext.Server.MapPath(@"~\TempFiles\") + FileName;

        //    //create and set PdfReader  
        //    System.IO.File.WriteAllBytes(FilePath, ReportPDF);
        //    //return file 
        //    string FilePathReturn = @"TempFiles/" + FileName;
        //    return Content(FilePathReturn);
        //}



        //public IActionResult Getxml()
        //{
        //    string pathToPdf = HttpContext.Server.MapPath(@"~\TempFiles\invoice.pdf");

        //   // string pathToPdf = @"~\TempFiles\PDFFile_637769022684578318.pdf";
        //    string pathToXml = System.IO.Path.ChangeExtension(pathToPdf, ".xml");

        //    // Convert PDF file to XML file.
        //    SautinSoft.PdfFocus f = new SautinSoft.PdfFocus();

        //    // This property is necessary only for registered version.
        //    //f.Serial = "XXXXXXXXXXX";

        //    // Let's convert only tables to XML and skip all textual data.
        //    f.XmlOptions.ConvertNonTabularDataToSpreadsheet = false;

        //    f.OpenPdf(pathToPdf);

        //    if (f.PageCount > 0)
        //    {
        //        int result = f.ToXml(pathToXml);

        //        //Show HTML document in browser
        //        if (result == 0)
        //        {
        //            System.Diagnostics.Process.Start(pathToXml);
        //        }
        //    }
        //    return Ok("");
        //}

        //public IActionResult PrintInvoicePDF(int? InvoiceId)
        //{`
        //    if (SuperId.HasValue)
        //    {
        //        string StampUrl = "";
        //        var Supervision2 = _Pro_SupervisionDetailsService.GetAllSupervisionDetailsBySuperId(SuperId).ToList();
        //        if (Supervision2 != null || Supervision2.Count() > 0)
        //        {
        //            var Phase = _Pro_Super_PhasesService.GetAllSuper_Phases("").Where(x => x.PhaseId == Supervision2[0].PhaseId).FirstOrDefault();
        //            ViewData["Phase"] = Phase;

        //            if (Supervision2.FirstOrDefault().ReceviedUserStampUrl != "")
        //            {
        //                StampUrl = _usersService.DecryptValue1(Supervision2.FirstOrDefault().ReceviedUserStampUrl);
        //                string ImageIn = Server.MapPath("~" + StampUrl);
        //                string ImageOut = ImageIn.Replace("\\Encrypted", "");
        //                bool flag = RijndaelHelper.DecryptFile(ImageIn, ImageOut);
        //                if (flag)
        //                {
        //                    //Not encrypted
        //                    string[] splitStr = ImageIn.Split('\\');
        //                    string Serverurl = @"http://" + Request.Url.Authority;
        //                    StampUrl = Serverurl + @"/Uploads/Users/" + splitStr[splitStr.Length - 1];
        //                }
        //                if (!System.IO.File.Exists(ImageOut))
        //                    StampUrl = "";

        //            }
        //            int orgId = _BranchesService.GetOrganizationId(BranchId);
        //            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId);
        //            if (objOrganization != null)
        //                ViewData["Org"] = objOrganization;
        //            else
        //                ViewData["Org"] = null;
        //        }
        //        else
        //        {
        //            ViewData["Phase"] = null;
        //        }
        //        ViewData["StampUrl"] = StampUrl;
        //        ViewData["Supervision"] = Supervision2;
        //    }
        //    else
        //    {

        //        ViewData["Phase"] = null;
        //        ViewData["Org"] = null;
        //        ViewData["Supervision"] = null;
        //    }
        //    return PartialView("_SupervsionPrint");
        //}
        //public static string ConvertNumToString(string Num)
        //{
        //    Bayanateck.TameerPro.DataModel.Utilities util = new Bayanateck.TameerPro.DataModel.Utilities(Num);
        //    if (util.GetNumberAr() == " ")
        //    {
        //        NumberToText numberToText = new NumberToText();
        //        return (numberToText.EnglishNumToText(Num) + " ريال فقط ");
        //    }
        //    return (util.GetNumberAr());
        //}
        [HttpGet("ConvertNumToString")]
        public static string ConvertNumToString(string Num)
        {
            // Utilities util = new Utilities(Num);
            // if (util.GetNumberAr() == " ")
            // {
            //    NumberToText numberToText = new NumberToText();
            //    return Ok(numberToText.EnglishNumToText(Num) + " ريال فقط ");
            // }
            //return Ok(util.GetNumberAr());

            CurrencyInfo _currencyInfo = new CurrencyInfo(CurrencyInfo.Currencies.SaudiArabia);
            ToWord toWord = new ToWord(Convert.ToDecimal(Num), _currencyInfo);
            return (toWord.ConvertToArabic());


        }
        [HttpGet("ConvertToWord_NEW")]

        public static string ConvertToWord_NEW(string Num)
        {
            CurrencyInfo _currencyInfo = new CurrencyInfo(CurrencyInfo.Currencies.SaudiArabia);
            ToWord toWord = new ToWord(Convert.ToDecimal(Num), _currencyInfo);
            return toWord.ConvertToArabic();
        }
        [HttpGet("InvTest_PDF")]
        public IActionResult InvTest_PDF(int? InvoiceId, int? TempCheck)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            try
            {
                var url = Path.Combine("Email/testPrint.html");
                var org = _organizationsservice.GetOrganizationDataLogin(_globalshared.Lang_G).Result;
                var file = Path.Combine("~") + org.LogoUrl;
                var body = InvoicePopulate("test", "mohammeddawoud", "headertest", "footertest", url, org?.NameAr ?? "tgrebe");
                return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = body });
            }
            catch (Exception ex)
            {
                return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "فشل في الطباعة" });
            }

        }
        [HttpGet("InvoicePopulate")]
        public string InvoicePopulate(string bodytxt, string fullname, string header, string footer, string url, string orgname)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(url))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{FullName}", fullname);
            body = body.Replace("{Body}", bodytxt);
            body = body.Replace("{Header}", header);
            body = body.Replace("{Footer}", footer);
            body = body.Replace("{orgname}", orgname);
            return body;
        }
        public class Invoice_PDF
        {
            public InvoicesVM? InvoicesVM_VD { get; set; }
            public CustomerVM? CustomerVM_VD { get; set; }
            public Acc_SuppliersVM? SuppliersVM_VD { get; set; }          
            public string? costCenterNam_VD { get; set; }
            public List<VoucherDetailsVM>? VoucherDetailsVM_VD { get; set; }
            public OrganizationsVM? Org_VD { get; set; }
            public BranchesVM? Branch_VD { get; set; }
            public string? ValueNumString { get; set; }
            public string? QRCodeString_VD { get; set; }
            public bool? OrgIsRequired_VD { get; set; }
            public int? TempCheck { get; set; }
        }
        [HttpGet("ChangeInvoice_PDF")]
        public IActionResult ChangeInvoice_PDF(int? InvoiceId, int? TempCheck)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            Invoice_PDF _invoice_PDF = new Invoice_PDF();

            if (InvoiceId.HasValue)
            {
                InvoicesVM InvoicesVM = _voucherService.GetVoucherById(InvoiceId ?? 0).Result;
                CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, _globalshared.Lang_G).Result;

                var costCenterNam = "";
                if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
                {
                    try
                    {
                        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
                    }
                    catch (Exception)
                    {
                        costCenterNam = "بدون";
                    }
                }
                else
                {
                    costCenterNam = "بدون";
                }
                //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
                string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

                List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(InvoiceId).Result.ToList();
                int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
                _invoice_PDF.Org_VD = objOrganization;

                var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
                var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(_globalshared.BranchId_G).Result.OrgDataIsRequired;
                if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;
                foreach (var item in VoucherDetailsVM)
                {
                    var servicesPriOffer = _servicesPriceOfferService.GetServicesPriceVouByParentIdAndInvoiceId(item.ServicesPriceId, item.InvoiceId).Result.ToList();
                    item.ServicesPricesOffer = servicesPriOffer;
                }


                _invoice_PDF.Branch_VD = objBranch;
                _invoice_PDF.CustomerVM_VD = CustomerVM;
                _invoice_PDF.InvoicesVM_VD = InvoicesVM;
                _invoice_PDF.costCenterNam_VD = costCenterNam;
                _invoice_PDF.VoucherDetailsVM_VD = VoucherDetailsVM;
                _invoice_PDF.ValueNumString = ValueNumString;
                _invoice_PDF.OrgIsRequired_VD = OrgIsRequired;
                _invoice_PDF.TempCheck = TempCheck;


                string Time_V = DateTime.Now.ToString("hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                string FullDate = InvoicesVM.Date + " " + Time_V;
                
                DateTime parsedDate;
                bool isValidDate = DateTime.TryParse(FullDate, out parsedDate);
                if (isValidDate)
                    Console.WriteLine(parsedDate);
                else
                    parsedDate = DateTime.Now;

                var SupplierName = objOrganization.NameAr.TrimStart();
                SupplierName = SupplierName.TrimEnd();
                try
                {
                    string ImgReturn = "";
                    TLVCls tlv = new TLVCls(SupplierName, objOrganization.TaxCode, parsedDate, (double)InvoicesVM.TotalValue, (double)InvoicesVM.TaxAmount);

                    using (Bitmap bitMap = tlv.toQrCode())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] byteImage = ms.ToArray();
                            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                        }
                    }
                    _invoice_PDF.QRCodeString_VD = ImgReturn;

                }
                catch (Exception ex)
                {
                    _invoice_PDF.QRCodeString_VD = null;
                }


              
            }
            else
            {
                _invoice_PDF.Org_VD = null;
                _invoice_PDF.CustomerVM_VD = null;
                _invoice_PDF.InvoicesVM_VD = null;
                _invoice_PDF.costCenterNam_VD = null;
                _invoice_PDF.VoucherDetailsVM_VD = null;
                _invoice_PDF.ValueNumString = null;
                _invoice_PDF.QRCodeString_VD = null;
                _invoice_PDF.Branch_VD = null;
            }
            return Ok(_invoice_PDF);
        }

        [HttpGet("ChangeInvoice_PDFCredit")]
        public ActionResult ChangeInvoice_PDFCredit(int? InvoiceId, int? TempCheck)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            Invoice_PDF _invoice_PDF = new Invoice_PDF();
            if (InvoiceId.HasValue)
            {
                DataTable voucher = new DataTable();
                if (TempCheck == 29)
                    voucher = _voucherService.ReceiptCashingPayingNoti(InvoiceId ?? 0, Con).Result;
                else if (TempCheck == 30)
                    voucher = _voucherService.ReceiptCashingPayingNotiDepit(InvoiceId ?? 0, Con).Result;
                else
                    voucher = _voucherService.ReceiptCashingPayingNoti(InvoiceId ?? 0, Con).Result;

                var InvId2 = Convert.ToInt32(voucher.Rows[0]["InvoiceId"].ToString());
                List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(InvId2).Result.ToList();


                InvoicesVM InvoicesVM = _voucherService.GetVoucherById(InvId2).Result;
                CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, _globalshared.Lang_G).Result;

                var costCenterNam = "";
                if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
                {
                    try
                    {
                        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
                    }
                    catch (Exception)
                    {
                        costCenterNam = "بدون";
                    }
                }
                else
                {
                    costCenterNam = "بدون";
                }
                string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());


                int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
                _invoice_PDF.Org_VD = objOrganization;



                var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
                var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(_globalshared.BranchId_G).Result.OrgDataIsRequired;
                if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;

                foreach (var item in VoucherDetailsVM)
                {
                    var servicesPriOffer = _servicesPriceOfferService.GetServicesPriceVouByParentIdAndInvoiceId(item.ServicesPriceId, item.InvoiceId).Result.ToList();
                    item.ServicesPricesOffer = servicesPriOffer;
                }

                _invoice_PDF.Branch_VD = objBranch;
                _invoice_PDF.CustomerVM_VD = CustomerVM;
                _invoice_PDF.InvoicesVM_VD = InvoicesVM;
                _invoice_PDF.costCenterNam_VD = costCenterNam;
                _invoice_PDF.VoucherDetailsVM_VD = VoucherDetailsVM;
                _invoice_PDF.ValueNumString = ValueNumString;
                _invoice_PDF.OrgIsRequired_VD = OrgIsRequired;
                _invoice_PDF.TempCheck = TempCheck;


                string Time_V = DateTime.Now.ToString("hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                string FullDate = InvoicesVM.Date + " " + Time_V;

                DateTime parsedDate;
                bool isValidDate = DateTime.TryParse(FullDate, out parsedDate);
                if (isValidDate)
                    Console.WriteLine(parsedDate);
                else
                    parsedDate = DateTime.Now;

                var SupplierName = objOrganization.NameAr.TrimStart();
                SupplierName = SupplierName.TrimEnd();
                try
                {
                    string ImgReturn = "";
                    TLVCls tlv = new TLVCls(SupplierName, objOrganization.TaxCode, parsedDate, (double)InvoicesVM.TotalValue, (double)InvoicesVM.TaxAmount);

                    using (Bitmap bitMap = tlv.toQrCode())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] byteImage = ms.ToArray();
                            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                        }
                    }
                    _invoice_PDF.QRCodeString_VD = ImgReturn;

                }
                catch (Exception ex)
                {
                    _invoice_PDF.QRCodeString_VD = null;
                }
            }
            else
            {
                _invoice_PDF.Org_VD = null;
                _invoice_PDF.CustomerVM_VD = null;
                _invoice_PDF.InvoicesVM_VD = null;
                _invoice_PDF.costCenterNam_VD = null;
                _invoice_PDF.VoucherDetailsVM_VD = null;
                _invoice_PDF.ValueNumString = null;
                _invoice_PDF.QRCodeString_VD = null;
                _invoice_PDF.Branch_VD = null;
            }
            return Ok(_invoice_PDF);
        }



        [HttpGet("ChangeInvoice_PDFCreditPurchase")]

        public IActionResult ChangeInvoice_PDFCreditPurchase(int? InvoiceId, int? TempCheck)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            Invoice_PDF _invoice_PDF = new Invoice_PDF();

            if (InvoiceId.HasValue)
            {
                var InvId2 = _voucherService.GetVoucherByIdNotiDepit_Purchase(InvoiceId ?? 0).InvoiceId;
                List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceIdPurchase(InvId2).Result.ToList();
                InvoicesVM InvoicesVM = _voucherService.GetVoucherByIdPurchase(InvId2).Result;
                Acc_SuppliersVM SuppliersVM = _acc_SuppliersService.GetSupplierByID(InvoicesVM.SupplierId ?? 0).Result;

                var costCenterNam = "";
                if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
                {
                    try
                    {
                        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
                    }
                    catch (Exception)
                    {
                        costCenterNam = "بدون";
                    }
                }
                else
                {
                    costCenterNam = "بدون";
                }
                string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());
                int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
                _invoice_PDF.Org_VD = objOrganization;

                var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
                var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(_globalshared.BranchId_G).Result.OrgDataIsRequired;
                if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;

                _invoice_PDF.Branch_VD = objBranch;
                _invoice_PDF.SuppliersVM_VD = SuppliersVM;
                _invoice_PDF.InvoicesVM_VD = InvoicesVM;
                _invoice_PDF.costCenterNam_VD = costCenterNam;
                _invoice_PDF.VoucherDetailsVM_VD = VoucherDetailsVM;
                _invoice_PDF.ValueNumString = ValueNumString;
                _invoice_PDF.OrgIsRequired_VD = OrgIsRequired;
                _invoice_PDF.TempCheck = TempCheck;

                string Time_V = DateTime.Now.ToString("hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                string FullDate = InvoicesVM.Date + " " + Time_V;

                DateTime parsedDate;
                bool isValidDate = DateTime.TryParse(FullDate, out parsedDate);
                if (isValidDate)
                    Console.WriteLine(parsedDate);
                else
                    parsedDate = DateTime.Now;

                var SupplierName = SuppliersVM.NameAr.TrimStart();
                SupplierName = SupplierName.TrimEnd();
                try
                {
                    string ImgReturn = "";
                    TLVCls tlv = new TLVCls(SupplierName, objOrganization.TaxCode, parsedDate, (double)InvoicesVM.TotalValue, (double)InvoicesVM.TaxAmount);
                    using (Bitmap bitMap = tlv.toQrCode())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] byteImage = ms.ToArray();
                            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                        }
                    }
                    _invoice_PDF.QRCodeString_VD = ImgReturn;
                    _invoice_PDF.QRCodeString_VD = ImgReturn;

                }
                catch (Exception ex)
                {
                    _invoice_PDF.QRCodeString_VD = null;
                }
            }
            else
            {
                _invoice_PDF.Org_VD = null;
                _invoice_PDF.SuppliersVM_VD = null;
                _invoice_PDF.InvoicesVM_VD = null;
                _invoice_PDF.costCenterNam_VD = null;
                _invoice_PDF.VoucherDetailsVM_VD = null;
                _invoice_PDF.ValueNumString = null;
                _invoice_PDF.QRCodeString_VD = null;
                _invoice_PDF.Branch_VD = null;
            }
            return Ok(_invoice_PDF);
        }

        [HttpGet("ChangePurchase_PDF")]
        public IActionResult ChangePurchase_PDF(int? InvoiceId, int? TempCheck)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            Invoice_PDF _invoice_PDF = new Invoice_PDF();

            if (InvoiceId.HasValue)
            {
                InvoicesVM InvoicesVM = _voucherService.GetVoucherByIdPurchase(InvoiceId ?? 0).Result;
                Acc_SuppliersVM SuppliersVM = _acc_SuppliersService.GetSupplierByID(InvoicesVM.SupplierId ?? 0).Result;

                var costCenterNam = "";
                if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
                {
                    try
                    {
                        costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
                    }
                    catch (Exception)
                    {
                        costCenterNam = "بدون";
                    }
                }
                else
                {
                    costCenterNam = "بدون";
                }
                //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
                string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

                List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceIdPurchase(InvoiceId).Result.ToList();
                int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
                _invoice_PDF.Org_VD = objOrganization;


                var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
                var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(_globalshared.BranchId_G).Result.OrgDataIsRequired;
                if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;

                _invoice_PDF.Branch_VD = objBranch;
                _invoice_PDF.SuppliersVM_VD = SuppliersVM;
                _invoice_PDF.InvoicesVM_VD = InvoicesVM;
                _invoice_PDF.costCenterNam_VD = costCenterNam;
                _invoice_PDF.VoucherDetailsVM_VD = VoucherDetailsVM;
                _invoice_PDF.ValueNumString = ValueNumString;
                _invoice_PDF.OrgIsRequired_VD = OrgIsRequired;
                _invoice_PDF.TempCheck = TempCheck;


                string Time_V = DateTime.Now.ToString("hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                string FullDate = InvoicesVM.Date + " " + Time_V;

                DateTime parsedDate;
                bool isValidDate = DateTime.TryParse(FullDate, out parsedDate);
                if (isValidDate)
                    Console.WriteLine(parsedDate);
                else
                    parsedDate = DateTime.Now;

                var SupplierName = SuppliersVM.NameAr.TrimStart();
                SupplierName = SupplierName.TrimEnd();
                try
                {
                    string ImgReturn = "";
                    TLVCls tlv = new TLVCls(SupplierName, objOrganization.TaxCode, parsedDate, (double)InvoicesVM.TotalValue, (double)InvoicesVM.TaxAmount);
                    using (Bitmap bitMap = tlv.toQrCode())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] byteImage = ms.ToArray();
                            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                        }
                    }
                    _invoice_PDF.QRCodeString_VD = ImgReturn;

                }
                catch (Exception ex)
                {
                    _invoice_PDF.QRCodeString_VD = null;
                }
            }
            else
            {
                _invoice_PDF.Org_VD = null;
                _invoice_PDF.SuppliersVM_VD = null;
                _invoice_PDF.InvoicesVM_VD = null;
                _invoice_PDF.costCenterNam_VD = null;
                _invoice_PDF.VoucherDetailsVM_VD = null;
                _invoice_PDF.ValueNumString = null;
                _invoice_PDF.QRCodeString_VD = null;
                _invoice_PDF.Branch_VD = null;
            }
            return Ok(_invoice_PDF);
        }


        //    [HttpPost]
        //    [ValidateInput(false)]
        //    public void Export2(string GridHtml)
        //    {
        //        try
        //        {
        //            string middleFile = Server.MapPath(@"~\Uploads\Drafts\Grid.pdf");
        //            string finalFile = Server.MapPath(@"~\Uploads\Drafts\final.pdf");
        //            string htmlFile = Server.MapPath(@"~\Test\Supervision.cshtml");

        //            PdfWriter writer = new PdfWriter(middleFile);
        //            PdfDocument pdfDocument = new PdfDocument(writer);

        //            pdfDocument.SetDefaultPageSize(new PageSize(PageSize.A4));

        //            //string header = "pdfHtml Header and footer example using page-events";
        //            //Header headerHandler = new Header(header);
        //            Footer footerHandler = new Footer();
        //            FontProvider fontProvider = new DefaultFontProvider(false, false, false);
        //            FontProgram fontProgram = FontProgramFactory.CreateFont(Environment.GetEnvironmentVariable("windir") + @"\fonts\ARIAL.TTF");
        //            fontProvider.AddFont(fontProgram);
        //            //pdfDocument.AddEventHandler(PdfDocumentEvent.START_PAGE, headerHandler);
        //            pdfDocument.AddEventHandler(PdfDocumentEvent.END_PAGE, footerHandler);

        //            // Base URI is required to resolve the path to source files
        //            ConverterProperties converterProperties = new ConverterProperties();
        //            converterProperties.SetFontProvider(fontProvider);
        //            HtmlConverter.ConvertToDocument(GridHtml, pdfDocument, converterProperties);

        //            // Write the total number of pages to the placeholder
        //            footerHandler.WriteTotal(pdfDocument);
        //            pdfDocument.Close();
        //        }
        //        catch (Exception ex)
        //        {
        //            var me = ex.Message;
        //        }

        //    }



        //    public class TextFooterEventHandler : IEventHandler
        //    {
        //        protected iText.Layout.Document doc;

        //        public TextFooterEventHandler(iText.Layout.Document doc)
        //        {
        //            this.doc = doc;
        //        }


        //        public void HandleEvent(Event currentEvent)
        //        {
        //            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
        //            Rectangle pageSize = docEvent.GetPage().GetPageSize();

        //            float coordX = ((pageSize.GetLeft() + doc.GetLeftMargin())
        //                             + (pageSize.GetRight() - doc.GetRightMargin())) / 2;
        //            float headerY = pageSize.GetTop() - doc.GetTopMargin() + 10;
        //            float footerY = doc.GetBottomMargin();

        //            //PdfCanvas pdfCanvas = new PdfCanvas(docEvent.GetPage());
        //            //pdfCanvas.AddXObject(placeholder, x + space, y - descent);
        //            //pdfCanvas.Release();

        //            Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);
        //            canvas

        //                // If the exception has been thrown, the font variable is not initialized.
        //                // Therefore null will be set and iText will use the default font - Helvetica

        //                .ShowTextAligned("this is a footer", coordX, footerY, TextAlignment.CENTER)
        //                .Close();
        //        }
        //    }

        //    // Header event handler
        //    public class Header : IEventHandler
        //    {
        //        private string header;

        //        public Header(string header)
        //        {
        //            this.header = header;
        //        }

        //        public virtual void HandleEvent(Event @event)
        //        {
        //            PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
        //            PdfDocument pdf = docEvent.GetDocument();

        //            PdfPage page = docEvent.GetPage();
        //            Rectangle pageSize = page.GetPageSize();
        //            string headerStr = @"<header id='header'>
        //    <table style='width:100%'>
        //        <tr>
        //            <td style='width:30%'>
        //                <div class='col-md-4'>
        //                    <div class='img-logo'>
        //                        <img src='https://tameercloud.com/Uploads/Users/logo.png' alt='logo' style='height:50px;width:120px;'>
        //                    </div>
        //                </div>
        //            </td>
        //            <td style='width:40%'>
        //                <div class='col-md-4'></div>
        //            </td>
        //            <td style='width:30%'>
        //                <div class='col-md-4'></div>
        //            </td>
        //        </tr>
        //    </table>
        //    <table style='width:100%'>
        //        <tr>
        //            <td style='width:30%'>
        //                <div class='col-md-4 '>
        //                    <div class='head-green'></div>
        //                </div>
        //            </td>
        //            <td style='width:40%'>
        //                <div class='col-md-4 up '>
        //                    <div class='title '>
        //                        @if (phase != null)
        //                        {
        //                            <p class='text-center largeFont'>@phase.Note </p>
        //                            <p dir='ltr' class='text-center largeFont'>@phase.NameAr </p>
        //                        }
        //                    </div>
        //                </div>
        //            </td>
        //            <td style='width:30%'>
        //                <div class='col-md-4 head-green '></div>
        //            </td>
        //        </tr>
        //    </table>
        //</header>";

        //            Canvas canvas = new Canvas(new PdfCanvas(page), pageSize);
        //            IList<IElement> headerElements = HtmlConverter.ConvertToElements(headerStr);
        //            foreach (IElement headerElement in headerElements)
        //            {
        //                // Making sure we are adding blocks to canvas
        //                if ((IBlockElement)headerElement != null)
        //                {
        //                    canvas.Add((IBlockElement)headerElement);
        //                }

        //                canvas.SetFontSize(8);

        //                // Write text at position
        //                //canvas.ShowTextAligned(header,
        //                //    pageSize.GetWidth() / 2,
        //                //    pageSize.GetTop() - 30, TextAlignment.CENTER);
        //                canvas.Close();
        //            }
        //        }
        //    }

        //    // Footer event handler
        //    public class Footer : IEventHandler
        //    {
        //        protected PdfFormXObject placeholder;
        //        protected float side = 20;
        //        protected float x = 300;
        //        protected float y = 25;
        //        protected float space = 5.5f;
        //        protected float descent = 3;

        //        public Footer()
        //        {
        //            placeholder = new PdfFormXObject(new Rectangle(0, 0, side, side));
        //        }

        //        public virtual void HandleEvent(Event @event)
        //        {
        //            PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
        //            PdfDocument pdf = docEvent.GetDocument();
        //            PdfPage page = docEvent.GetPage();
        //            int pageNumber = pdf.GetPageNumber(page);
        //            Rectangle pageSize = page.GetPageSize();
        //            x = pageSize.GetLeft() + 1f;
        //            FontProvider fontProvider = new DefaultFontProvider(false, false, false);
        //            FontProgram fontProgram = FontProgramFactory.CreateFont(Environment.GetEnvironmentVariable("windir") + @"\fonts\ARIAL.TTF");
        //            fontProvider.AddFont(fontProgram);


        //            // Creates drawing canvas
        //            PdfCanvas pdfCanvas = new PdfCanvas(page);
        //            Canvas canvas = new Canvas(pdfCanvas, pageSize);
        //            canvas.SetFontProvider(fontProvider);

        //            Paragraph p = new Paragraph()
        //                .Add(" Page ")
        //                .Add(pageNumber.ToString())
        //                .Add(" of ");
        //            //p.SetFontSize(8);
        //            canvas.ShowTextAligned(p, x + 5f, y, TextAlignment.LEFT);
        //            canvas.Close();

        //            // Create placeholder object to write number of pages
        //            pdfCanvas.AddXObjectAt(placeholder, x + 60f, y - descent);
        //            pdfCanvas.Release();
        //        }

        //        public void WriteTotal(PdfDocument pdf)
        //        {
        //            Canvas canvas = new Canvas(placeholder, pdf);
        //            canvas.ShowTextAligned(pdf.GetNumberOfPages().ToString(),
        //                0, descent, TextAlignment.LEFT);
        //            canvas.Close();
        //        }
        //    }

        [HttpGet("GenerateRandomNo")]
        public int GenerateRandomNo()
        {
            int _min = 100000;
            int _max = 999999;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max);
        }




        // Convert HTML TO XML
        [HttpGet("ConvertHTMLTOXML")]
        public void ConvertHTMLTOXML()
        {
            Spire.Doc.Document doc = new Spire.Doc.Document();
            doc.LoadFromFile("Sample.html");
            doc.SaveToFile("test.xml", FileFormat.Xml);
        }

        //END



        /// <summary>
        /// /////////////////////////////////////////////////////////////  Zatca Integration Region  ///////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="invoiceid"></param>
        /// <returns></returns>
        /// 
        ////فاتورة مبسطة
        //public IActionResult btn_SimplifiedInvoice_Click(int? invoiceid)
        //{



        //    string FileName = "";
        //    string xmlFile2 = "";
        //    if (invoiceid.HasValue)
        //    {
        //        DataTable voucher = _voucherService.ReceiptCashingPaying((int)invoiceid, Con);

        //        InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0);
        //        CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, Lang);

        //        var costCenterNam = "";
        //        if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
        //        {
        //            try
        //            {
        //                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //            }
        //            catch (Exception)
        //            {
        //                costCenterNam = "بدون";
        //            }
        //        }
        //        else
        //        {
        //            costCenterNam = "بدون";
        //        }
        //        //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
        //        string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

        //        List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).ToList();
        //        int orgId = _BranchesService.GetOrganizationId(BranchId);
        //        var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //        if (objOrganization != null)
        //            ViewData["Org_VD"] = objOrganization;
        //        else
        //            ViewData["Org_VD"] = null;


        //        var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //        var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(BranchId).OrgDataIsRequired;



        //        DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
        //        string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
        //        var SupplierName = objOrganization.NameAr.TrimStart();
        //        SupplierName = SupplierName.TrimEnd();
        //        string qrBarcodeHash = QRCodeEncoder.encode(
        //            new Seller_Inv(SupplierName),
        //            new TaxNumber_Inv(objOrganization.TaxCode),
        //            new InvoiceDate_Inv(ActionDate),
        //            new TotalAmount_Inv(InvoicesVM.TotalValue.ToString()),
        //            new TaxAmount_Inv(InvoicesVM.TaxAmount.ToString())
        //        );

        //        //string qrcode = "";
        //        //try
        //        //{
        //        //    string ImgReturn = "";

        //        //    QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrBarcodeHash, QRCodeGenerator.ECCLevel.Q, true);
        //        //    QRCode qrCode = new QRCode(qrCodeData);
        //        //    using (Bitmap bitMap = qrCode.GetGraphic(20))
        //        //    {
        //        //        using (MemoryStream ms = new MemoryStream())
        //        //        {
        //        //            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        //            byte[] byteImage = ms.ToArray();
        //        //            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
        //        //            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
        //        //        }
        //        //    }
        //        //    qrcode = ImgReturn;

        //        //}
        //        //catch (Exception ex)
        //        //{



        //        //}


        //        if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null)
        //        {

        //            string BankAccount1 = "لا يوجد";
        //            string BankAccount2 = "لا يوجد";

        //            if (InvoicesVM.printBankAccount == true)
        //            {
        //                BankAccount1 = "لا يوجد";
        //                BankAccount2 = "لا يوجد";
        //            }
        //            else
        //            {
        //                BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
        //                BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
        //            }


        //            decimal totalDetailsPrice = 0;
        //            decimal totalDetailsTax = 0;
        //            decimal totalDiscount = 0;
        //            decimal percentage = 0;
        //            decimal Diff = 0;
        //            int Counter = 0;

        //            percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
        //            totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

        //            decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
        //            decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
        //            decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
        //            decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
        //            string remainder = (_totalVal - _paidValud).ToString();
        //            decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
        //            decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
        //            if (discountPercentage == default(decimal) && discountValue != default(decimal))
        //            {
        //                discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
        //            }
        //            if (discountPercentage != default(decimal) && discountValue == default(decimal))
        //            {
        //                discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
        //                //_totalVal = _totalVal - discountValue;
        //            }
        //            decimal netVal = 0;
        //            if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
        //            }
        //            else
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value;
        //            }




        //            string Address1 = "";
        //            string BuildingNumber1 = "";
        //            string StreetName1 = "";
        //            string Neighborhood1 = "";
        //            string CityName1 = "";
        //            string Country1 = "";
        //            string PostalCode1 = "";
        //            string ExternalPhone1 = "";
        //            string TaxCode1 = "";

        //            if (OrgIsRequired == true)
        //            {
        //                Address1 = objOrganization.Address;
        //                BuildingNumber1 = objOrganization.BuildingNumber;
        //                StreetName1 = objOrganization.StreetName;
        //                Neighborhood1 = objOrganization.Neighborhood;
        //                CityName1 = objOrganization.CityName;
        //                Country1 = objOrganization.Country;
        //                PostalCode1 = objOrganization.PostalCode;
        //                ExternalPhone1 = objOrganization.ExternalPhone;
        //                TaxCode1 = objOrganization.TaxCode;

        //            }
        //            else
        //            {
        //                Address1 = objBranch.Address;
        //                BuildingNumber1 = objBranch.BuildingNumber;
        //                StreetName1 = objBranch.StreetName;
        //                Neighborhood1 = objBranch.Neighborhood;
        //                CityName1 = objBranch.CityName;
        //                Country1 = objBranch.Country;
        //                PostalCode1 = objBranch.PostalCodeFinal;
        //                ExternalPhone1 = objBranch.ExternalPhone;
        //                TaxCode1 = objBranch.TaxCode;

        //            }

        //            //for information
        //            // VAT Category (O) "Not subject to VAT" (O)  غير خاضع لضريبة القيمة المضافة لابد ان يكون نسبة الضريبة صفر
        //            // VAT Category (E)   معفى من ضريبة القيمة المضافة نسبة الضريبة هتكون صفر ولابد من ذكر سبب الاعفاء :TaxExemptionReason
        //            // VAT Category (S)   ضريبة القيمة المضافة لابد من كتابة النسبة وتكون اكبر من صفر
        //            // VAT Category (Z)   Zero rated goods  البضائع الخاضعة للنسبة الصفرية 

        //            //PaymentMeansCode
        //            //10 In cash
        //            //30 Credit
        //            //42 Payment to bank account
        //            //48 Bank card
        //            //1 Instrument not defined(Free text)




        //            UBLXML ubl = new UBLXML();
        //            Invoice inv = new Invoice();
        //            Result res = new Result();

        //            inv.ID = InvoicesVM.InvoiceNumber.ToString(); // مثال SME00010
        //            inv.UUID = Guid.NewGuid().ToString();
        //            inv.IssueDate = InvoicesVM.Date.ToString();
        //            inv.IssueTime = "10:59:55";
        //            //
        //            //فاتورة  
        //            //383 اشعار مدين
        //            //381 اشعار دائن
        //            inv.invoiceTypeCode.id = 388;
        //            //inv.invoiceTypeCode.Name based on format NNPNESB
        //            //NN 01 فاتورة عادية
        //            //NN 02 فاتورة مبسطة
        //            //P فى حالة فاتورة لطرف ثالث نكتب 1 فى الحالة الاخرى نكتب 0
        //            //N فى حالة فاتورة اسمية نكتب 1 وفى الحالة الاخرى نكتب 0
        //            // E فى حالة فاتورة للصادرات نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //S فى حالة فاتورة ملخصة نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //B فى حالة فاتورة ذاتية نكتب 1
        //            //B فى حالة ان الفاتورة صادرات=1 لايمكن ان تكون الفاتورة ذاتية =1
        //            //
        //            inv.invoiceTypeCode.Name = "0200000";
        //            inv.DocumentCurrencyCode = "SAR";
        //            inv.TaxCurrencyCode = "SAR";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            //inv.billingReference.InvoiceDocumentReferenceID = "123654";
        //            // قيمة عداد الفاتورة
        //            inv.AdditionalDocumentReferenceICV.UUID = (int)InvoicesVM.InvoiceNumber; // لابد ان يكون ارقام فقط
        //                                                                                     //فى حالة فاتورة مبسطة وفاتورة ملخصة هانكتب تاريخ التسليم واخر تاريخ التسليم
        //                                                                                     // inv.delivery.ActualDeliveryDate = "2022-10-22";
        //                                                                                     // inv.delivery.LatestDeliveryDate = "2022-10-23";
        //                                                                                     //
        //                                                                                     //بيانات الدفع 
        //                                                                                     // اكواد معين
        //                                                                                     // اختيارى كود الدفع
        //            inv.paymentmeans.PaymentMeansCode = "10";//اختيارى
        //            inv.paymentmeans.InstructionNote = "Payment Notes"; //اجبارى فى الاشعارات
        //                                                                // inv.paymentmeans.payeefinancialaccount.ID = "";//اختيارى
        //                                                                // inv.paymentmeans.payeefinancialaccount.paymentnote = "Payment by credit";//اختيارى

        //            //بيانات البائع 
        //            inv.SupplierParty.partyIdentification.ID = "123456"; // رقم السجل التجارى الخاض بالبائع
        //            inv.SupplierParty.partyIdentification.schemeID = "CRN"; //رقم السجل التجارى
        //            inv.SupplierParty.postalAddress.StreetName = StreetName1;// اجبارى
        //            inv.SupplierParty.postalAddress.AdditionalStreetName = ""; //اختيارى
        //            inv.SupplierParty.postalAddress.BuildingNumber = BuildingNumber1; // اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.SupplierParty.postalAddress.CityName = CityName1; //اسم المدينة
        //            inv.SupplierParty.postalAddress.PostalZone = PostalCode1;//الرقم البريدي
        //            inv.SupplierParty.postalAddress.CountrySubentity = CityName1;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.SupplierParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.SupplierParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.SupplierParty.partyLegalEntity.RegistrationName = objOrganization.NameAr; // اسم الشركة المسجل فى الهيئة
        //            inv.SupplierParty.partyTaxScheme.CompanyID = "301121971100003";//TaxCode1;// رقم التسجيل الضريبي

        //            // بيانات المشترى
        //            inv.CustomerParty.partyIdentification.ID = "123456";// رقم السجل التجارى الخاض بالمشترى
        //            inv.CustomerParty.partyIdentification.schemeID = "NAT";//رقم السجل التجارى
        //            inv.CustomerParty.postalAddress.StreetName = CustomerVM.StreetName;// اجبارى
        //            inv.CustomerParty.postalAddress.AdditionalStreetName = "";//اختيارى
        //            inv.CustomerParty.postalAddress.BuildingNumber = CustomerVM.BuildingNumber;// اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.CustomerParty.postalAddress.CityName = CustomerVM.CityName; //اسم المدينة
        //            inv.CustomerParty.postalAddress.PostalZone = CustomerVM.PostalCodeFinal;//الرقم البريدي
        //            inv.CustomerParty.postalAddress.CountrySubentity = CustomerVM.CityName;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.CustomerParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.CustomerParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.CustomerParty.partyLegalEntity.RegistrationName = CustomerVM.CustomerNameAr;// اسم الشركة المسجل فى الهيئة
        //            inv.CustomerParty.partyTaxScheme.CompanyID = "301121972500003";// CustomerVM.CommercialRegister;// رقم التسجيل الضريبي



        //            AllowanceCharge allowancecharge = new AllowanceCharge();

        //            allowancecharge.Amount = 0;
        //            allowancecharge.AllowanceChargeReason = "discount"; //سبب الخصم
        //            allowancecharge.taxCategory.ID = "S";// كود الضريبة
        //            allowancecharge.taxCategory.Percent = 15;// نسبة الضريبة
        //            inv.allowanceCharges.Add(allowancecharge);

        //            // فى حالة فى اكتر من منتج فى الفاتورة هانعمل ليست من invoiceline مثال الكود التالى
        //            foreach (var item in VoucherDetailsVM)
        //            {

        //                Counter = Counter + 1;
        //                Diff = 0;
        //                totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
        //                totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
        //                Diff = Convert.ToDecimal(item.Amount * item.Qty);


        //                InvoiceLine invline = new InvoiceLine();
        //                invline.InvoiceQuantity = (int)item.Qty;

        //                invline.item.classifiedTaxCategory.ID = "S";// كود الضريبة
        //                invline.item.classifiedTaxCategory.Percent = 15;// نسبة الضريبة

        //                //if the price is including vat set EncludingVat=true;
        //                if (item.TaxType == 3)
        //                {
        //                    invline.price.EncludingVat = true;
        //                }
        //                else
        //                {
        //                    invline.price.EncludingVat = false;

        //                }

        //                invline.item.Name = item.ServicesPriceName;
        //                invline.price.PriceAmount = (float)item.Amount;

        //                invline.price.allowanceCharge.AllowanceChargeReason = "discount"; //سبب الخصم على مستوى المنتج
        //                invline.price.allowanceCharge.Amount = 0;//قيم الخصم


        //                invline.taxTotal.TaxSubtotal.taxCategory.ID = "S";//كود الضريبة
        //                invline.taxTotal.TaxSubtotal.taxCategory.Percent = 15;//نسبة الضريبة
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Private healthcare to citizen";
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-HEA";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "";

        //                inv.InvoiceLines.Add(invline);
        //            }





        //            res = ubl.GenerateInvoiceXML(inv, Server.MapPath("~"));

        //            if (res.IsValid)
        //            {
        //                Invoices invoiceupdate = new Invoices();
        //                invoiceupdate.InvoiceId = (int)invoiceid;
        //                invoiceupdate.InvoiceHash = res.InvoiceHash;
        //                invoiceupdate.SingedXML = res.SingedXML;
        //                invoiceupdate.EncodedInvoice = res.EncodedInvoice;
        //                invoiceupdate.ZatcaUUID = res.UUID;
        //                invoiceupdate.QRCode = res.QRCode;
        //                invoiceupdate.PIH = res.PIH;
        //                invoiceupdate.SingedXMLFileName = res.SingedXMLFileName;

        //                _voucherService.UpdateInvoiceWithZatcaData(invoiceupdate, _globalshared.UserId_G, _globalshared.BranchId_G);

        //                //القيم التالية تحتاج ان تحفظها فى سطر الفاتورة فى قاعدة البيانات الخاصة بكم  كي تكون مرجع لكم لاحقاً
        //                //MessageBox.Show(res.InvoiceHash);
        //                //MessageBox.Show(res.SingedXML);
        //                //MessageBox.Show(res.EncodedInvoice);
        //                //MessageBox.Show(res.UUID);
        //                //MessageBox.Show(res.QRCode);
        //                //MessageBox.Show(res.PIH);
        //                //MessageBox.Show(res.SingedXMLFileName);
        //            }
        //            else
        //            {
        //                //MessageBox.Show(res.ErrorMessage);
        //                return Ok(res.ErrorMessage);
        //                //return;
        //            }


        //            ApiRequestLogic apireqlogic = new ApiRequestLogic();
        //            ComplianceCsrResponse tokenresponse = new ComplianceCsrResponse();
        //            InvoiceReportingRequest invrequestbody = new InvoiceReportingRequest();
        //            tokenresponse = apireqlogic.GetComplianceCSIDAPI("12345", "", Server.MapPath("~"));
        //            //tokenresponse1 = apireqlogic.GetProductionCSIDAPI(tokenresponse.RequestId, tokenresponse.BinarySecurityToken, tokenresponse.Secret); ;
        //            if (string.IsNullOrEmpty(tokenresponse.ErrorMessage))
        //            {
        //                //MessageBox.Show(tokenresponse.BinarySecurityToken);
        //                invrequestbody.invoice = res.EncodedInvoice;
        //                invrequestbody.invoiceHash = res.InvoiceHash;
        //                invrequestbody.uuid = res.UUID;
        //                InvoiceReportingResponse invoicereportingmodel = apireqlogic.CallComplianceInvoiceAPI(tokenresponse.BinarySecurityToken, tokenresponse.Secret, invrequestbody);
        //                if (string.IsNullOrEmpty(invoicereportingmodel.ErrorMessage))
        //                {
        //                    return Ok("");
        //                    //REPORTED =>Passed Successfuly
        //                }
        //                else
        //                {
        //                    return Ok("");
        //                }
        //            }
        //            else
        //            {
        //                return Ok("");
        //            }
        //        }
        //    }
        //    return Ok("");

        //}

        ////فاتورة عادية
        //public IActionResult btn_StandardInvoice_Click(int? invoiceid)
        //{



        //    string FileName = "";
        //    string xmlFile2 = "";
        //    if (invoiceid.HasValue)
        //    {
        //        DataTable voucher = _voucherService.ReceiptCashingPaying((int)invoiceid, Con);

        //        InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0);
        //        CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, Lang);

        //        var costCenterNam = "";
        //        if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
        //        {
        //            try
        //            {
        //                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //            }
        //            catch (Exception)
        //            {
        //                costCenterNam = "بدون";
        //            }
        //        }
        //        else
        //        {
        //            costCenterNam = "بدون";
        //        }
        //        //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
        //        string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

        //        List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).ToList();
        //        int orgId = _BranchesService.GetOrganizationId(BranchId);
        //        var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //        if (objOrganization != null)
        //            ViewData["Org_VD"] = objOrganization;
        //        else
        //            ViewData["Org_VD"] = null;


        //        var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //        var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(BranchId).OrgDataIsRequired;
                        //if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;



        //        DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
        //        string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
        //        var SupplierName = objOrganization.NameAr.TrimStart();
        //        SupplierName = SupplierName.TrimEnd();
        //        string qrBarcodeHash = QRCodeEncoder.encode(
        //            new Seller_Inv(SupplierName),
        //            new TaxNumber_Inv(objOrganization.TaxCode),
        //            new InvoiceDate_Inv(ActionDate),
        //            new TotalAmount_Inv(InvoicesVM.TotalValue.ToString()),
        //            new TaxAmount_Inv(InvoicesVM.TaxAmount.ToString())
        //        );

        //        //string qrcode = "";
        //        //try
        //        //{
        //        //    string ImgReturn = "";

        //        //    QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrBarcodeHash, QRCodeGenerator.ECCLevel.Q, true);
        //        //    QRCode qrCode = new QRCode(qrCodeData);
        //        //    using (Bitmap bitMap = qrCode.GetGraphic(20))
        //        //    {
        //        //        using (MemoryStream ms = new MemoryStream())
        //        //        {
        //        //            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        //            byte[] byteImage = ms.ToArray();
        //        //            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
        //        //            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
        //        //        }
        //        //    }
        //        //    qrcode = ImgReturn;

        //        //}
        //        //catch (Exception ex)
        //        //{



        //        //}


        //        if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null)
        //        {

        //            string BankAccount1 = "لا يوجد";
        //            string BankAccount2 = "لا يوجد";

        //            if (InvoicesVM.printBankAccount == true)
        //            {
        //                BankAccount1 = "لا يوجد";
        //                BankAccount2 = "لا يوجد";
        //            }
        //            else
        //            {
        //                BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
        //                BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
        //            }


        //            decimal totalDetailsPrice = 0;
        //            decimal totalDetailsTax = 0;
        //            decimal totalDiscount = 0;
        //            decimal percentage = 0;
        //            decimal Diff = 0;
        //            int Counter = 0;

        //            percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
        //            totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

        //            decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
        //            decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
        //            decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
        //            decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
        //            string remainder = (_totalVal - _paidValud).ToString();
        //            decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
        //            decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
        //            if (discountPercentage == default(decimal) && discountValue != default(decimal))
        //            {
        //                discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
        //            }
        //            if (discountPercentage != default(decimal) && discountValue == default(decimal))
        //            {
        //                discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
        //                //_totalVal = _totalVal - discountValue;
        //            }
        //            decimal netVal = 0;
        //            if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
        //            }
        //            else
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value;
        //            }




        //            string Address1 = "";
        //            string BuildingNumber1 = "";
        //            string StreetName1 = "";
        //            string Neighborhood1 = "";
        //            string CityName1 = "";
        //            string Country1 = "";
        //            string PostalCode1 = "";
        //            string ExternalPhone1 = "";
        //            string TaxCode1 = "";

        //            if (OrgIsRequired == true)
        //            {
        //                Address1 = objOrganization.Address;
        //                BuildingNumber1 = objOrganization.BuildingNumber;
        //                StreetName1 = objOrganization.StreetName;
        //                Neighborhood1 = objOrganization.Neighborhood;
        //                CityName1 = objOrganization.CityName;
        //                Country1 = objOrganization.Country;
        //                PostalCode1 = objOrganization.PostalCode;
        //                ExternalPhone1 = objOrganization.ExternalPhone;
        //                TaxCode1 = objOrganization.TaxCode;

        //            }
        //            else
        //            {
        //                Address1 = objBranch.Address;
        //                BuildingNumber1 = objBranch.BuildingNumber;
        //                StreetName1 = objBranch.StreetName;
        //                Neighborhood1 = objBranch.Neighborhood;
        //                CityName1 = objBranch.CityName;
        //                Country1 = objBranch.Country;
        //                PostalCode1 = objBranch.PostalCodeFinal;
        //                ExternalPhone1 = objBranch.ExternalPhone;
        //                TaxCode1 = objBranch.TaxCode;

        //            }

        //            //for information
        //            // VAT Category (O) "Not subject to VAT" (O)  غير خاضع لضريبة القيمة المضافة لابد ان يكون نسبة الضريبة صفر
        //            // VAT Category (E)   معفى من ضريبة القيمة المضافة نسبة الضريبة هتكون صفر ولابد من ذكر سبب الاعفاء :TaxExemptionReason
        //            // VAT Category (S)   ضريبة القيمة المضافة لابد من كتابة النسبة وتكون اكبر من صفر
        //            // VAT Category (Z)   Zero rated goods  البضائع الخاضعة للنسبة الصفرية 

        //            //PaymentMeansCode
        //            //10 In cash
        //            //30 Credit
        //            //42 Payment to bank account
        //            //48 Bank card
        //            //1 Instrument not defined(Free text)




        //            UBLXML ubl = new UBLXML();
        //            Invoice inv = new Invoice();
        //            Result res = new Result();

        //            inv.ID = InvoicesVM.InvoiceNumber.ToString(); // مثال SME00010
        //            inv.UUID = Guid.NewGuid().ToString();
        //            inv.IssueDate = InvoicesVM.Date.ToString();
        //            inv.IssueTime = "10:59:55";
        //            //
        //            //فاتورة  
        //            //383 اشعار مدين
        //            //381 اشعار دائن
        //            inv.invoiceTypeCode.id = 388;
        //            //inv.invoiceTypeCode.Name based on format NNPNESB
        //            //NN 01 فاتورة عادية
        //            //NN 02 فاتورة مبسطة
        //            //P فى حالة فاتورة لطرف ثالث نكتب 1 فى الحالة الاخرى نكتب 0
        //            //N فى حالة فاتورة اسمية نكتب 1 وفى الحالة الاخرى نكتب 0
        //            // E فى حالة فاتورة للصادرات نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //S فى حالة فاتورة ملخصة نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //B فى حالة فاتورة ذاتية نكتب 1
        //            //B فى حالة ان الفاتورة صادرات=1 لايمكن ان تكون الفاتورة ذاتية =1
        //            //
        //            inv.invoiceTypeCode.Name = "0100000";
        //            inv.DocumentCurrencyCode = "SAR";
        //            inv.TaxCurrencyCode = "SAR";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            //inv.billingReference.InvoiceDocumentReferenceID = "123654";
        //            // قيمة عداد الفاتورة
        //            inv.AdditionalDocumentReferenceICV.UUID = (int)InvoicesVM.InvoiceNumber; // لابد ان يكون ارقام فقط
        //                                                                                     //فى حالة فاتورة مبسطة وفاتورة ملخصة هانكتب تاريخ التسليم واخر تاريخ التسليم
        //                                                                                     // inv.delivery.ActualDeliveryDate = "2022-10-22";
        //                                                                                     // inv.delivery.LatestDeliveryDate = "2022-10-23";
        //                                                                                     //
        //                                                                                     //بيانات الدفع 
        //                                                                                     // اكواد معين
        //                                                                                     // اختيارى كود الدفع
        //            inv.paymentmeans.PaymentMeansCode = "10";//اختيارى
        //            inv.paymentmeans.InstructionNote = "Payment Notes"; //اجبارى فى الاشعارات
        //                                                                // inv.paymentmeans.payeefinancialaccount.ID = "";//اختيارى
        //                                                                // inv.paymentmeans.payeefinancialaccount.paymentnote = "Payment by credit";//اختيارى

        //            //بيانات البائع 
        //            inv.SupplierParty.partyIdentification.ID = "123456"; // رقم السجل التجارى الخاض بالبائع
        //            inv.SupplierParty.partyIdentification.schemeID = "CRN"; //رقم السجل التجارى
        //            inv.SupplierParty.postalAddress.StreetName = StreetName1;// اجبارى
        //            inv.SupplierParty.postalAddress.AdditionalStreetName = ""; //اختيارى
        //            inv.SupplierParty.postalAddress.BuildingNumber = BuildingNumber1; // اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.SupplierParty.postalAddress.CityName = CityName1; //اسم المدينة
        //            inv.SupplierParty.postalAddress.PostalZone = PostalCode1;//الرقم البريدي
        //            inv.SupplierParty.postalAddress.CountrySubentity = CityName1;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.SupplierParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.SupplierParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.SupplierParty.partyLegalEntity.RegistrationName = objOrganization.NameAr; // اسم الشركة المسجل فى الهيئة
        //            inv.SupplierParty.partyTaxScheme.CompanyID = "301121971100003";//TaxCode1;// رقم التسجيل الضريبي

        //            // بيانات المشترى
        //            inv.CustomerParty.partyIdentification.ID = "123456";// رقم السجل التجارى الخاض بالمشترى
        //            inv.CustomerParty.partyIdentification.schemeID = "NAT";//رقم السجل التجارى
        //            inv.CustomerParty.postalAddress.StreetName = CustomerVM.StreetName;// اجبارى
        //            inv.CustomerParty.postalAddress.AdditionalStreetName = "";//اختيارى
        //            inv.CustomerParty.postalAddress.BuildingNumber = CustomerVM.BuildingNumber;// اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.CustomerParty.postalAddress.CityName = CustomerVM.CityName; //اسم المدينة
        //            inv.CustomerParty.postalAddress.PostalZone = CustomerVM.PostalCodeFinal;//الرقم البريدي
        //            inv.CustomerParty.postalAddress.CountrySubentity = CustomerVM.CityName;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.CustomerParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.CustomerParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.CustomerParty.partyLegalEntity.RegistrationName = CustomerVM.CustomerNameAr;// اسم الشركة المسجل فى الهيئة
        //            inv.CustomerParty.partyTaxScheme.CompanyID = "301121972500003";// CustomerVM.CommercialRegister;// رقم التسجيل الضريبي



        //            AllowanceCharge allowancecharge = new AllowanceCharge();

        //            allowancecharge.Amount = 0;
        //            allowancecharge.AllowanceChargeReason = "discount"; //سبب الخصم
        //            allowancecharge.taxCategory.ID = "S";// كود الضريبة
        //            allowancecharge.taxCategory.Percent = 15;// نسبة الضريبة
        //            inv.allowanceCharges.Add(allowancecharge);

        //            // فى حالة فى اكتر من منتج فى الفاتورة هانعمل ليست من invoiceline مثال الكود التالى
        //            foreach (var item in VoucherDetailsVM)
        //            {

        //                Counter = Counter + 1;
        //                Diff = 0;
        //                totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
        //                totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
        //                Diff = Convert.ToDecimal(item.Amount * item.Qty);


        //                InvoiceLine invline = new InvoiceLine();
        //                invline.InvoiceQuantity = (int)item.Qty;

        //                invline.item.classifiedTaxCategory.ID = "S";// كود الضريبة
        //                invline.item.classifiedTaxCategory.Percent = 15;// نسبة الضريبة

        //                //if the price is including vat set EncludingVat=true;
        //                if (item.TaxType == 3)
        //                {
        //                    invline.price.EncludingVat = true;
        //                }
        //                else
        //                {
        //                    invline.price.EncludingVat = false;

        //                }

        //                invline.item.Name = item.ServicesPriceName;
        //                invline.price.PriceAmount = (float)item.Amount;

        //                invline.price.allowanceCharge.AllowanceChargeReason = "discount"; //سبب الخصم على مستوى المنتج
        //                invline.price.allowanceCharge.Amount = 0;//قيم الخصم


        //                invline.taxTotal.TaxSubtotal.taxCategory.ID = "S";//كود الضريبة
        //                invline.taxTotal.TaxSubtotal.taxCategory.Percent = 15;//نسبة الضريبة
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Private healthcare to citizen";
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-HEA";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "";

        //                inv.InvoiceLines.Add(invline);
        //            }





        //            res = ubl.GenerateInvoiceXML(inv, Server.MapPath("~"));

        //            if (res.IsValid)
        //            {

        //                Invoices invoiceupdate = new Invoices();
        //                invoiceupdate.InvoiceId = (int)invoiceid;
        //                invoiceupdate.InvoiceHash = res.InvoiceHash;
        //                invoiceupdate.SingedXML = res.SingedXML;
        //                invoiceupdate.EncodedInvoice = res.EncodedInvoice;
        //                invoiceupdate.ZatcaUUID = res.UUID;
        //                invoiceupdate.QRCode = res.QRCode;
        //                invoiceupdate.PIH = res.PIH;
        //                invoiceupdate.SingedXMLFileName = res.SingedXMLFileName;

        //                _voucherService.UpdateInvoiceWithZatcaData(invoiceupdate, _globalshared.UserId_G, _globalshared.BranchId_G);

        //                //القيم التالية تحتاج ان تحفظها فى سطر الفاتورة فى قاعدة البيانات الخاصة بكم  كي تكون مرجع لكم لاحقاً
        //                //MessageBox.Show(res.InvoiceHash);
        //                //MessageBox.Show(res.SingedXML);
        //                //MessageBox.Show(res.EncodedInvoice);
        //                //MessageBox.Show(res.UUID);
        //                //MessageBox.Show(res.QRCode);
        //                //MessageBox.Show(res.PIH);
        //                //MessageBox.Show(res.SingedXMLFileName);
        //            }
        //            else
        //            {
        //                //MessageBox.Show(res.ErrorMessage);
        //                return Ok(res.ErrorMessage);
        //                //return;
        //            }


        //            ApiRequestLogic apireqlogic = new ApiRequestLogic();
        //            ComplianceCsrResponse tokenresponse = new ComplianceCsrResponse();
        //            InvoiceReportingRequest invrequestbody = new InvoiceReportingRequest();
        //            tokenresponse = apireqlogic.GetComplianceCSIDAPI("12345", "", Server.MapPath("~"));
        //            //tokenresponse1 = apireqlogic.GetProductionCSIDAPI(tokenresponse.RequestId, tokenresponse.BinarySecurityToken, tokenresponse.Secret); ;
        //            if (string.IsNullOrEmpty(tokenresponse.ErrorMessage))
        //            {
        //                //MessageBox.Show(tokenresponse.BinarySecurityToken);
        //                invrequestbody.invoice = res.EncodedInvoice;
        //                invrequestbody.invoiceHash = res.InvoiceHash;
        //                invrequestbody.uuid = res.UUID;
        //                InvoiceReportingResponse invoicereportingmodel = apireqlogic.CallComplianceInvoiceAPI(tokenresponse.BinarySecurityToken, tokenresponse.Secret, invrequestbody);
        //                if (string.IsNullOrEmpty(invoicereportingmodel.ErrorMessage))
        //                {
        //                    return Ok("");
        //                    //REPORTED =>Passed Successfuly
        //                }
        //                else
        //                {
        //                    return Ok("");
        //                }
        //            }
        //            else
        //            {
        //                return Ok("");
        //            }
        //        }
        //    }
        //    return Ok("");

        //}

        ////اشعار مدين فاتورة مبسطة
        //public IActionResult btn_SimplifiedDebitNote_Click(int? invoiceid)
        //{



        //    string FileName = "";
        //    string xmlFile2 = "";
        //    if (invoiceid.HasValue)
        //    {
        //        DataTable voucher = _voucherService.ReceiptCashingPaying((int)invoiceid, Con);

        //        InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0);
        //        CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, Lang);

        //        var costCenterNam = "";
        //        if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
        //        {
        //            try
        //            {
        //                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //            }
        //            catch (Exception)
        //            {
        //                costCenterNam = "بدون";
        //            }
        //        }
        //        else
        //        {
        //            costCenterNam = "بدون";
        //        }
        //        //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
        //        string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

        //        List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).ToList();
        //        int orgId = _BranchesService.GetOrganizationId(BranchId);
        //        var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //        if (objOrganization != null)
        //            ViewData["Org_VD"] = objOrganization;
        //        else
        //            ViewData["Org_VD"] = null;


        //        var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //        var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(BranchId).OrgDataIsRequired;
                        //if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;



        //        DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
        //        string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
        //        var SupplierName = objOrganization.NameAr.TrimStart();
        //        SupplierName = SupplierName.TrimEnd();
        //        string qrBarcodeHash = QRCodeEncoder.encode(
        //            new Seller_Inv(SupplierName),
        //            new TaxNumber_Inv(objOrganization.TaxCode),
        //            new InvoiceDate_Inv(ActionDate),
        //            new TotalAmount_Inv(InvoicesVM.TotalValue.ToString()),
        //            new TaxAmount_Inv(InvoicesVM.TaxAmount.ToString())
        //        );

        //        //string qrcode = "";
        //        //try
        //        //{
        //        //    string ImgReturn = "";

        //        //    QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrBarcodeHash, QRCodeGenerator.ECCLevel.Q, true);
        //        //    QRCode qrCode = new QRCode(qrCodeData);
        //        //    using (Bitmap bitMap = qrCode.GetGraphic(20))
        //        //    {
        //        //        using (MemoryStream ms = new MemoryStream())
        //        //        {
        //        //            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        //            byte[] byteImage = ms.ToArray();
        //        //            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
        //        //            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
        //        //        }
        //        //    }
        //        //    qrcode = ImgReturn;

        //        //}
        //        //catch (Exception ex)
        //        //{



        //        //}


        //        if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null)
        //        {

        //            string BankAccount1 = "لا يوجد";
        //            string BankAccount2 = "لا يوجد";

        //            if (InvoicesVM.printBankAccount == true)
        //            {
        //                BankAccount1 = "لا يوجد";
        //                BankAccount2 = "لا يوجد";
        //            }
        //            else
        //            {
        //                BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
        //                BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
        //            }


        //            decimal totalDetailsPrice = 0;
        //            decimal totalDetailsTax = 0;
        //            decimal totalDiscount = 0;
        //            decimal percentage = 0;
        //            decimal Diff = 0;
        //            int Counter = 0;

        //            percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
        //            totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

        //            decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
        //            decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
        //            decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
        //            decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
        //            string remainder = (_totalVal - _paidValud).ToString();
        //            decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
        //            decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
        //            if (discountPercentage == default(decimal) && discountValue != default(decimal))
        //            {
        //                discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
        //            }
        //            if (discountPercentage != default(decimal) && discountValue == default(decimal))
        //            {
        //                discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
        //                //_totalVal = _totalVal - discountValue;
        //            }
        //            decimal netVal = 0;
        //            if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
        //            }
        //            else
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value;
        //            }




        //            string Address1 = "";
        //            string BuildingNumber1 = "";
        //            string StreetName1 = "";
        //            string Neighborhood1 = "";
        //            string CityName1 = "";
        //            string Country1 = "";
        //            string PostalCode1 = "";
        //            string ExternalPhone1 = "";
        //            string TaxCode1 = "";

        //            if (OrgIsRequired == true)
        //            {
        //                Address1 = objOrganization.Address;
        //                BuildingNumber1 = objOrganization.BuildingNumber;
        //                StreetName1 = objOrganization.StreetName;
        //                Neighborhood1 = objOrganization.Neighborhood;
        //                CityName1 = objOrganization.CityName;
        //                Country1 = objOrganization.Country;
        //                PostalCode1 = objOrganization.PostalCode;
        //                ExternalPhone1 = objOrganization.ExternalPhone;
        //                TaxCode1 = objOrganization.TaxCode;

        //            }
        //            else
        //            {
        //                Address1 = objBranch.Address;
        //                BuildingNumber1 = objBranch.BuildingNumber;
        //                StreetName1 = objBranch.StreetName;
        //                Neighborhood1 = objBranch.Neighborhood;
        //                CityName1 = objBranch.CityName;
        //                Country1 = objBranch.Country;
        //                PostalCode1 = objBranch.PostalCodeFinal;
        //                ExternalPhone1 = objBranch.ExternalPhone;
        //                TaxCode1 = objBranch.TaxCode;

        //            }

        //            //for information
        //            // VAT Category (O) "Not subject to VAT" (O)  غير خاضع لضريبة القيمة المضافة لابد ان يكون نسبة الضريبة صفر
        //            // VAT Category (E)   معفى من ضريبة القيمة المضافة نسبة الضريبة هتكون صفر ولابد من ذكر سبب الاعفاء :TaxExemptionReason
        //            // VAT Category (S)   ضريبة القيمة المضافة لابد من كتابة النسبة وتكون اكبر من صفر
        //            // VAT Category (Z)   Zero rated goods  البضائع الخاضعة للنسبة الصفرية 

        //            //PaymentMeansCode
        //            //10 In cash
        //            //30 Credit
        //            //42 Payment to bank account
        //            //48 Bank card
        //            //1 Instrument not defined(Free text)




        //            UBLXML ubl = new UBLXML();
        //            Invoice inv = new Invoice();
        //            Result res = new Result();

        //            inv.ID = InvoicesVM.InvoiceNumber.ToString(); // مثال SME00010
        //            inv.UUID = Guid.NewGuid().ToString();
        //            inv.IssueDate = InvoicesVM.Date.ToString();
        //            inv.IssueTime = "10:59:55";
        //            //
        //            //فاتورة  
        //            //383 اشعار مدين
        //            //381 اشعار دائن
        //            inv.invoiceTypeCode.id = 383;
        //            //inv.invoiceTypeCode.Name based on format NNPNESB
        //            //NN 01 فاتورة عادية
        //            //NN 02 فاتورة مبسطة
        //            //P فى حالة فاتورة لطرف ثالث نكتب 1 فى الحالة الاخرى نكتب 0
        //            //N فى حالة فاتورة اسمية نكتب 1 وفى الحالة الاخرى نكتب 0
        //            // E فى حالة فاتورة للصادرات نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //S فى حالة فاتورة ملخصة نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //B فى حالة فاتورة ذاتية نكتب 1
        //            //B فى حالة ان الفاتورة صادرات=1 لايمكن ان تكون الفاتورة ذاتية =1
        //            //
        //            inv.invoiceTypeCode.Name = "0200000";
        //            inv.DocumentCurrencyCode = "SAR";
        //            inv.TaxCurrencyCode = "SAR";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            //inv.billingReference.InvoiceDocumentReferenceID = "123654";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            inv.billingReference.InvoiceDocumentReferenceID = InvoicesVM.InvoiceNumber.ToString();
        //            // قيمة عداد الفاتورة
        //            inv.AdditionalDocumentReferenceICV.UUID = (int)InvoicesVM.InvoiceNumber; // لابد ان يكون ارقام فقط
        //                                                                                     //فى حالة فاتورة مبسطة وفاتورة ملخصة هانكتب تاريخ التسليم واخر تاريخ التسليم
        //                                                                                     // inv.delivery.ActualDeliveryDate = "2022-10-22";
        //                                                                                     // inv.delivery.LatestDeliveryDate = "2022-10-23";
        //                                                                                     //
        //                                                                                     //بيانات الدفع 
        //                                                                                     // اكواد معين
        //                                                                                     // اختيارى كود الدفع
        //            inv.paymentmeans.PaymentMeansCode = "10";//اختيارى
        //            inv.paymentmeans.InstructionNote = "Payment Notes"; //اجبارى فى الاشعارات
        //                                                                // inv.paymentmeans.payeefinancialaccount.ID = "";//اختيارى
        //                                                                // inv.paymentmeans.payeefinancialaccount.paymentnote = "Payment by credit";//اختيارى

        //            //بيانات البائع 
        //            inv.SupplierParty.partyIdentification.ID = "123456"; // رقم السجل التجارى الخاض بالبائع
        //            inv.SupplierParty.partyIdentification.schemeID = "CRN"; //رقم السجل التجارى
        //            inv.SupplierParty.postalAddress.StreetName = StreetName1;// اجبارى
        //            inv.SupplierParty.postalAddress.AdditionalStreetName = ""; //اختيارى
        //            inv.SupplierParty.postalAddress.BuildingNumber = BuildingNumber1; // اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.SupplierParty.postalAddress.CityName = CityName1; //اسم المدينة
        //            inv.SupplierParty.postalAddress.PostalZone = PostalCode1;//الرقم البريدي
        //            inv.SupplierParty.postalAddress.CountrySubentity = CityName1;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.SupplierParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.SupplierParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.SupplierParty.partyLegalEntity.RegistrationName = objOrganization.NameAr; // اسم الشركة المسجل فى الهيئة
        //            inv.SupplierParty.partyTaxScheme.CompanyID = "301121971100003";//TaxCode1;// رقم التسجيل الضريبي

        //            // بيانات المشترى
        //            inv.CustomerParty.partyIdentification.ID = "123456";// رقم السجل التجارى الخاض بالمشترى
        //            inv.CustomerParty.partyIdentification.schemeID = "NAT";//رقم السجل التجارى
        //            inv.CustomerParty.postalAddress.StreetName = CustomerVM.StreetName;// اجبارى
        //            inv.CustomerParty.postalAddress.AdditionalStreetName = "";//اختيارى
        //            inv.CustomerParty.postalAddress.BuildingNumber = CustomerVM.BuildingNumber;// اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.CustomerParty.postalAddress.CityName = CustomerVM.CityName; //اسم المدينة
        //            inv.CustomerParty.postalAddress.PostalZone = CustomerVM.PostalCodeFinal;//الرقم البريدي
        //            inv.CustomerParty.postalAddress.CountrySubentity = CustomerVM.CityName;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.CustomerParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.CustomerParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.CustomerParty.partyLegalEntity.RegistrationName = CustomerVM.CustomerNameAr;// اسم الشركة المسجل فى الهيئة
        //            inv.CustomerParty.partyTaxScheme.CompanyID = "301121972500003";// CustomerVM.CommercialRegister;// رقم التسجيل الضريبي



        //            AllowanceCharge allowancecharge = new AllowanceCharge();

        //            allowancecharge.Amount = 0;
        //            allowancecharge.AllowanceChargeReason = "discount"; //سبب الخصم
        //            allowancecharge.taxCategory.ID = "S";// كود الضريبة
        //            allowancecharge.taxCategory.Percent = 15;// نسبة الضريبة
        //            inv.allowanceCharges.Add(allowancecharge);

        //            // فى حالة فى اكتر من منتج فى الفاتورة هانعمل ليست من invoiceline مثال الكود التالى
        //            foreach (var item in VoucherDetailsVM)
        //            {

        //                Counter = Counter + 1;
        //                Diff = 0;
        //                totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
        //                totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
        //                Diff = Convert.ToDecimal(item.Amount * item.Qty);


        //                InvoiceLine invline = new InvoiceLine();
        //                invline.InvoiceQuantity = (int)item.Qty;

        //                invline.item.classifiedTaxCategory.ID = "S";// كود الضريبة
        //                invline.item.classifiedTaxCategory.Percent = 15;// نسبة الضريبة

        //                //if the price is including vat set EncludingVat=true;
        //                if (item.TaxType == 3)
        //                {
        //                    invline.price.EncludingVat = true;
        //                }
        //                else
        //                {
        //                    invline.price.EncludingVat = false;

        //                }

        //                invline.item.Name = item.ServicesPriceName;
        //                invline.price.PriceAmount = (float)item.Amount;

        //                invline.price.allowanceCharge.AllowanceChargeReason = "discount"; //سبب الخصم على مستوى المنتج
        //                invline.price.allowanceCharge.Amount = 0;//قيم الخصم


        //                invline.taxTotal.TaxSubtotal.taxCategory.ID = "S";//كود الضريبة
        //                invline.taxTotal.TaxSubtotal.taxCategory.Percent = 15;//نسبة الضريبة
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Private healthcare to citizen";
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-HEA";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "";

        //                inv.InvoiceLines.Add(invline);
        //            }





        //            res = ubl.GenerateInvoiceXML(inv, Server.MapPath("~"));

        //            if (res.IsValid)
        //            {

        //                Invoices invoiceupdate = new Invoices();
        //                invoiceupdate.InvoiceId = (int)invoiceid;
        //                invoiceupdate.InvoiceHash = res.InvoiceHash;
        //                invoiceupdate.SingedXML = res.SingedXML;
        //                invoiceupdate.EncodedInvoice = res.EncodedInvoice;
        //                invoiceupdate.ZatcaUUID = res.UUID;
        //                invoiceupdate.QRCode = res.QRCode;
        //                invoiceupdate.PIH = res.PIH;
        //                invoiceupdate.SingedXMLFileName = res.SingedXMLFileName;

        //                _voucherService.UpdateInvoiceWithZatcaData(invoiceupdate, _globalshared.UserId_G, _globalshared.BranchId_G);

        //                //القيم التالية تحتاج ان تحفظها فى سطر الفاتورة فى قاعدة البيانات الخاصة بكم  كي تكون مرجع لكم لاحقاً
        //                //MessageBox.Show(res.InvoiceHash);
        //                //MessageBox.Show(res.SingedXML);
        //                //MessageBox.Show(res.EncodedInvoice);
        //                //MessageBox.Show(res.UUID);
        //                //MessageBox.Show(res.QRCode);
        //                //MessageBox.Show(res.PIH);
        //                //MessageBox.Show(res.SingedXMLFileName);
        //            }
        //            else
        //            {
        //                //MessageBox.Show(res.ErrorMessage);
        //                return Ok(res.ErrorMessage);
        //                //return;
        //            }


        //            ApiRequestLogic apireqlogic = new ApiRequestLogic();
        //            ComplianceCsrResponse tokenresponse = new ComplianceCsrResponse();
        //            InvoiceReportingRequest invrequestbody = new InvoiceReportingRequest();
        //            tokenresponse = apireqlogic.GetComplianceCSIDAPI("12345", "", Server.MapPath("~"));
        //            //tokenresponse1 = apireqlogic.GetProductionCSIDAPI(tokenresponse.RequestId, tokenresponse.BinarySecurityToken, tokenresponse.Secret); ;
        //            if (string.IsNullOrEmpty(tokenresponse.ErrorMessage))
        //            {
        //                //MessageBox.Show(tokenresponse.BinarySecurityToken);
        //                invrequestbody.invoice = res.EncodedInvoice;
        //                invrequestbody.invoiceHash = res.InvoiceHash;
        //                invrequestbody.uuid = res.UUID;
        //                InvoiceReportingResponse invoicereportingmodel = apireqlogic.CallComplianceInvoiceAPI(tokenresponse.BinarySecurityToken, tokenresponse.Secret, invrequestbody);
        //                if (string.IsNullOrEmpty(invoicereportingmodel.ErrorMessage))
        //                {
        //                    return Ok("");
        //                    //REPORTED =>Passed Successfuly
        //                }
        //                else
        //                {
        //                    return Ok("");
        //                }
        //            }
        //            else
        //            {
        //                return Ok("");
        //            }
        //        }
        //    }
        //    return Ok("");

        //}


        ////اشعار مدين فاتورة عادية
        //public IActionResult btn_StandaredDebitNote_Click(int? invoiceid)
        //{



        //    string FileName = "";
        //    string xmlFile2 = "";
        //    if (invoiceid.HasValue)
        //    {
        //        DataTable voucher = _voucherService.ReceiptCashingPaying((int)invoiceid, Con);

        //        InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0);
        //        CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, Lang);

        //        var costCenterNam = "";
        //        if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
        //        {
        //            try
        //            {
        //                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //            }
        //            catch (Exception)
        //            {
        //                costCenterNam = "بدون";
        //            }
        //        }
        //        else
        //        {
        //            costCenterNam = "بدون";
        //        }
        //        //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
        //        string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

        //        List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).ToList();
        //        int orgId = _BranchesService.GetOrganizationId(BranchId);
        //        var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //        if (objOrganization != null)
        //            ViewData["Org_VD"] = objOrganization;
        //        else
        //            ViewData["Org_VD"] = null;


        //        var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //        var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(BranchId).OrgDataIsRequired;
                        //if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;



        //        DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
        //        string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
        //        var SupplierName = objOrganization.NameAr.TrimStart();
        //        SupplierName = SupplierName.TrimEnd();
        //        string qrBarcodeHash = QRCodeEncoder.encode(
        //            new Seller_Inv(SupplierName),
        //            new TaxNumber_Inv(objOrganization.TaxCode),
        //            new InvoiceDate_Inv(ActionDate),
        //            new TotalAmount_Inv(InvoicesVM.TotalValue.ToString()),
        //            new TaxAmount_Inv(InvoicesVM.TaxAmount.ToString())
        //        );

        //        //string qrcode = "";
        //        //try
        //        //{
        //        //    string ImgReturn = "";

        //        //    QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrBarcodeHash, QRCodeGenerator.ECCLevel.Q, true);
        //        //    QRCode qrCode = new QRCode(qrCodeData);
        //        //    using (Bitmap bitMap = qrCode.GetGraphic(20))
        //        //    {
        //        //        using (MemoryStream ms = new MemoryStream())
        //        //        {
        //        //            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        //            byte[] byteImage = ms.ToArray();
        //        //            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
        //        //            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
        //        //        }
        //        //    }
        //        //    qrcode = ImgReturn;

        //        //}
        //        //catch (Exception ex)
        //        //{



        //        //}


        //        if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null)
        //        {

        //            string BankAccount1 = "لا يوجد";
        //            string BankAccount2 = "لا يوجد";

        //            if (InvoicesVM.printBankAccount == true)
        //            {
        //                BankAccount1 = "لا يوجد";
        //                BankAccount2 = "لا يوجد";
        //            }
        //            else
        //            {
        //                BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
        //                BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
        //            }


        //            decimal totalDetailsPrice = 0;
        //            decimal totalDetailsTax = 0;
        //            decimal totalDiscount = 0;
        //            decimal percentage = 0;
        //            decimal Diff = 0;
        //            int Counter = 0;

        //            percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
        //            totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

        //            decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
        //            decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
        //            decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
        //            decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
        //            string remainder = (_totalVal - _paidValud).ToString();
        //            decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
        //            decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
        //            if (discountPercentage == default(decimal) && discountValue != default(decimal))
        //            {
        //                discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
        //            }
        //            if (discountPercentage != default(decimal) && discountValue == default(decimal))
        //            {
        //                discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
        //                //_totalVal = _totalVal - discountValue;
        //            }
        //            decimal netVal = 0;
        //            if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
        //            }
        //            else
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value;
        //            }




        //            string Address1 = "";
        //            string BuildingNumber1 = "";
        //            string StreetName1 = "";
        //            string Neighborhood1 = "";
        //            string CityName1 = "";
        //            string Country1 = "";
        //            string PostalCode1 = "";
        //            string ExternalPhone1 = "";
        //            string TaxCode1 = "";

        //            if (OrgIsRequired == true)
        //            {
        //                Address1 = objOrganization.Address;
        //                BuildingNumber1 = objOrganization.BuildingNumber;
        //                StreetName1 = objOrganization.StreetName;
        //                Neighborhood1 = objOrganization.Neighborhood;
        //                CityName1 = objOrganization.CityName;
        //                Country1 = objOrganization.Country;
        //                PostalCode1 = objOrganization.PostalCode;
        //                ExternalPhone1 = objOrganization.ExternalPhone;
        //                TaxCode1 = objOrganization.TaxCode;

        //            }
        //            else
        //            {
        //                Address1 = objBranch.Address;
        //                BuildingNumber1 = objBranch.BuildingNumber;
        //                StreetName1 = objBranch.StreetName;
        //                Neighborhood1 = objBranch.Neighborhood;
        //                CityName1 = objBranch.CityName;
        //                Country1 = objBranch.Country;
        //                PostalCode1 = objBranch.PostalCodeFinal;
        //                ExternalPhone1 = objBranch.ExternalPhone;
        //                TaxCode1 = objBranch.TaxCode;

        //            }

        //            //for information
        //            // VAT Category (O) "Not subject to VAT" (O)  غير خاضع لضريبة القيمة المضافة لابد ان يكون نسبة الضريبة صفر
        //            // VAT Category (E)   معفى من ضريبة القيمة المضافة نسبة الضريبة هتكون صفر ولابد من ذكر سبب الاعفاء :TaxExemptionReason
        //            // VAT Category (S)   ضريبة القيمة المضافة لابد من كتابة النسبة وتكون اكبر من صفر
        //            // VAT Category (Z)   Zero rated goods  البضائع الخاضعة للنسبة الصفرية 

        //            //PaymentMeansCode
        //            //10 In cash
        //            //30 Credit
        //            //42 Payment to bank account
        //            //48 Bank card
        //            //1 Instrument not defined(Free text)




        //            UBLXML ubl = new UBLXML();
        //            Invoice inv = new Invoice();
        //            Result res = new Result();

        //            inv.ID = InvoicesVM.InvoiceNumber.ToString(); // مثال SME00010
        //            inv.UUID = Guid.NewGuid().ToString();
        //            inv.IssueDate = InvoicesVM.Date.ToString();
        //            inv.IssueTime = "10:59:55";
        //            //
        //            //فاتورة  
        //            //383 اشعار مدين
        //            //381 اشعار دائن
        //            inv.invoiceTypeCode.id = 383;
        //            //inv.invoiceTypeCode.Name based on format NNPNESB
        //            //NN 01 فاتورة عادية
        //            //NN 02 فاتورة مبسطة
        //            //P فى حالة فاتورة لطرف ثالث نكتب 1 فى الحالة الاخرى نكتب 0
        //            //N فى حالة فاتورة اسمية نكتب 1 وفى الحالة الاخرى نكتب 0
        //            // E فى حالة فاتورة للصادرات نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //S فى حالة فاتورة ملخصة نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //B فى حالة فاتورة ذاتية نكتب 1
        //            //B فى حالة ان الفاتورة صادرات=1 لايمكن ان تكون الفاتورة ذاتية =1
        //            //
        //            inv.invoiceTypeCode.Name = "0100000";
        //            inv.DocumentCurrencyCode = "SAR";
        //            inv.TaxCurrencyCode = "SAR";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            //inv.billingReference.InvoiceDocumentReferenceID = "123654";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            inv.billingReference.InvoiceDocumentReferenceID = InvoicesVM.InvoiceNumber.ToString();
        //            // قيمة عداد الفاتورة
        //            inv.AdditionalDocumentReferenceICV.UUID = (int)InvoicesVM.InvoiceNumber; // لابد ان يكون ارقام فقط
        //                                                                                     //فى حالة فاتورة مبسطة وفاتورة ملخصة هانكتب تاريخ التسليم واخر تاريخ التسليم
        //                                                                                     // inv.delivery.ActualDeliveryDate = "2022-10-22";
        //                                                                                     // inv.delivery.LatestDeliveryDate = "2022-10-23";
        //                                                                                     //
        //                                                                                     //بيانات الدفع 
        //                                                                                     // اكواد معين
        //                                                                                     // اختيارى كود الدفع
        //            inv.paymentmeans.PaymentMeansCode = "10";//اختيارى
        //            inv.paymentmeans.InstructionNote = "Payment Notes"; //اجبارى فى الاشعارات
        //                                                                // inv.paymentmeans.payeefinancialaccount.ID = "";//اختيارى
        //                                                                // inv.paymentmeans.payeefinancialaccount.paymentnote = "Payment by credit";//اختيارى

        //            //بيانات البائع 
        //            inv.SupplierParty.partyIdentification.ID = "123456"; // رقم السجل التجارى الخاض بالبائع
        //            inv.SupplierParty.partyIdentification.schemeID = "CRN"; //رقم السجل التجارى
        //            inv.SupplierParty.postalAddress.StreetName = StreetName1;// اجبارى
        //            inv.SupplierParty.postalAddress.AdditionalStreetName = ""; //اختيارى
        //            inv.SupplierParty.postalAddress.BuildingNumber = BuildingNumber1; // اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.SupplierParty.postalAddress.CityName = CityName1; //اسم المدينة
        //            inv.SupplierParty.postalAddress.PostalZone = PostalCode1;//الرقم البريدي
        //            inv.SupplierParty.postalAddress.CountrySubentity = CityName1;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.SupplierParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.SupplierParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.SupplierParty.partyLegalEntity.RegistrationName = objOrganization.NameAr; // اسم الشركة المسجل فى الهيئة
        //            inv.SupplierParty.partyTaxScheme.CompanyID = "301121971100003";//TaxCode1;// رقم التسجيل الضريبي

        //            // بيانات المشترى
        //            inv.CustomerParty.partyIdentification.ID = "123456";// رقم السجل التجارى الخاض بالمشترى
        //            inv.CustomerParty.partyIdentification.schemeID = "NAT";//رقم السجل التجارى
        //            inv.CustomerParty.postalAddress.StreetName = CustomerVM.StreetName;// اجبارى
        //            inv.CustomerParty.postalAddress.AdditionalStreetName = "";//اختيارى
        //            inv.CustomerParty.postalAddress.BuildingNumber = CustomerVM.BuildingNumber;// اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.CustomerParty.postalAddress.CityName = CustomerVM.CityName; //اسم المدينة
        //            inv.CustomerParty.postalAddress.PostalZone = CustomerVM.PostalCodeFinal;//الرقم البريدي
        //            inv.CustomerParty.postalAddress.CountrySubentity = CustomerVM.CityName;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.CustomerParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.CustomerParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.CustomerParty.partyLegalEntity.RegistrationName = CustomerVM.CustomerNameAr;// اسم الشركة المسجل فى الهيئة
        //            inv.CustomerParty.partyTaxScheme.CompanyID = "301121972500003";// CustomerVM.CommercialRegister;// رقم التسجيل الضريبي



        //            AllowanceCharge allowancecharge = new AllowanceCharge();

        //            allowancecharge.Amount = 0;
        //            allowancecharge.AllowanceChargeReason = "discount"; //سبب الخصم
        //            allowancecharge.taxCategory.ID = "S";// كود الضريبة
        //            allowancecharge.taxCategory.Percent = 15;// نسبة الضريبة
        //            inv.allowanceCharges.Add(allowancecharge);

        //            // فى حالة فى اكتر من منتج فى الفاتورة هانعمل ليست من invoiceline مثال الكود التالى
        //            foreach (var item in VoucherDetailsVM)
        //            {

        //                Counter = Counter + 1;
        //                Diff = 0;
        //                totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
        //                totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
        //                Diff = Convert.ToDecimal(item.Amount * item.Qty);


        //                InvoiceLine invline = new InvoiceLine();
        //                invline.InvoiceQuantity = (int)item.Qty;

        //                invline.item.classifiedTaxCategory.ID = "S";// كود الضريبة
        //                invline.item.classifiedTaxCategory.Percent = 15;// نسبة الضريبة

        //                //if the price is including vat set EncludingVat=true;
        //                if (item.TaxType == 3)
        //                {
        //                    invline.price.EncludingVat = true;
        //                }
        //                else
        //                {
        //                    invline.price.EncludingVat = false;

        //                }

        //                invline.item.Name = item.ServicesPriceName;
        //                invline.price.PriceAmount = (float)item.Amount;

        //                invline.price.allowanceCharge.AllowanceChargeReason = "discount"; //سبب الخصم على مستوى المنتج
        //                invline.price.allowanceCharge.Amount = 0;//قيم الخصم


        //                invline.taxTotal.TaxSubtotal.taxCategory.ID = "S";//كود الضريبة
        //                invline.taxTotal.TaxSubtotal.taxCategory.Percent = 15;//نسبة الضريبة
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Private healthcare to citizen";
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-HEA";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "";

        //                inv.InvoiceLines.Add(invline);
        //            }





        //            res = ubl.GenerateInvoiceXML(inv, Server.MapPath("~"));

        //            if (res.IsValid)
        //            {

        //                Invoices invoiceupdate = new Invoices();
        //                invoiceupdate.InvoiceId = (int)invoiceid;
        //                invoiceupdate.InvoiceHash = res.InvoiceHash;
        //                invoiceupdate.SingedXML = res.SingedXML;
        //                invoiceupdate.EncodedInvoice = res.EncodedInvoice;
        //                invoiceupdate.ZatcaUUID = res.UUID;
        //                invoiceupdate.QRCode = res.QRCode;
        //                invoiceupdate.PIH = res.PIH;
        //                invoiceupdate.SingedXMLFileName = res.SingedXMLFileName;

        //                _voucherService.UpdateInvoiceWithZatcaData(invoiceupdate, _globalshared.UserId_G, _globalshared.BranchId_G);

        //                //القيم التالية تحتاج ان تحفظها فى سطر الفاتورة فى قاعدة البيانات الخاصة بكم  كي تكون مرجع لكم لاحقاً
        //                //MessageBox.Show(res.InvoiceHash);
        //                //MessageBox.Show(res.SingedXML);
        //                //MessageBox.Show(res.EncodedInvoice);
        //                //MessageBox.Show(res.UUID);
        //                //MessageBox.Show(res.QRCode);
        //                //MessageBox.Show(res.PIH);
        //                //MessageBox.Show(res.SingedXMLFileName);
        //            }
        //            else
        //            {
        //                //MessageBox.Show(res.ErrorMessage);
        //                return Ok(res.ErrorMessage);
        //                //return;
        //            }


        //            ApiRequestLogic apireqlogic = new ApiRequestLogic();
        //            ComplianceCsrResponse tokenresponse = new ComplianceCsrResponse();
        //            InvoiceReportingRequest invrequestbody = new InvoiceReportingRequest();
        //            tokenresponse = apireqlogic.GetComplianceCSIDAPI("12345", "", Server.MapPath("~"));
        //            //tokenresponse1 = apireqlogic.GetProductionCSIDAPI(tokenresponse.RequestId, tokenresponse.BinarySecurityToken, tokenresponse.Secret); ;
        //            if (string.IsNullOrEmpty(tokenresponse.ErrorMessage))
        //            {
        //                //MessageBox.Show(tokenresponse.BinarySecurityToken);
        //                invrequestbody.invoice = res.EncodedInvoice;
        //                invrequestbody.invoiceHash = res.InvoiceHash;
        //                invrequestbody.uuid = res.UUID;
        //                InvoiceReportingResponse invoicereportingmodel = apireqlogic.CallComplianceInvoiceAPI(tokenresponse.BinarySecurityToken, tokenresponse.Secret, invrequestbody);
        //                if (string.IsNullOrEmpty(invoicereportingmodel.ErrorMessage))
        //                {
        //                    return Ok("");
        //                    //REPORTED =>Passed Successfuly
        //                }
        //                else
        //                {
        //                    return Ok("");
        //                }
        //            }
        //            else
        //            {
        //                return Ok("");
        //            }
        //        }
        //    }
        //    return Ok("");

        //}

        ////اشعار دائن فاتورة مبسطة
        //public IActionResult btn_SimplifiedCreditNote_Click(int? invoiceid)
        //{



        //    string FileName = "";
        //    string xmlFile2 = "";
        //    if (invoiceid.HasValue)
        //    {
        //        DataTable voucher = _voucherService.ReceiptCashingPaying((int)invoiceid, Con);

        //        InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0);
        //        CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, Lang);

        //        var costCenterNam = "";
        //        if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
        //        {
        //            try
        //            {
        //                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //            }
        //            catch (Exception)
        //            {
        //                costCenterNam = "بدون";
        //            }
        //        }
        //        else
        //        {
        //            costCenterNam = "بدون";
        //        }
        //        //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
        //        string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

        //        List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).ToList();
        //        int orgId = _BranchesService.GetOrganizationId(BranchId);
        //        var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //        if (objOrganization != null)
        //            ViewData["Org_VD"] = objOrganization;
        //        else
        //            ViewData["Org_VD"] = null;


        //        var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //        var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(BranchId).OrgDataIsRequired;
                       // if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;



        //        DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
        //        string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
        //        var SupplierName = objOrganization.NameAr.TrimStart();
        //        SupplierName = SupplierName.TrimEnd();
        //        string qrBarcodeHash = QRCodeEncoder.encode(
        //            new Seller_Inv(SupplierName),
        //            new TaxNumber_Inv(objOrganization.TaxCode),
        //            new InvoiceDate_Inv(ActionDate),
        //            new TotalAmount_Inv(InvoicesVM.TotalValue.ToString()),
        //            new TaxAmount_Inv(InvoicesVM.TaxAmount.ToString())
        //        );

        //        //string qrcode = "";
        //        //try
        //        //{
        //        //    string ImgReturn = "";

        //        //    QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrBarcodeHash, QRCodeGenerator.ECCLevel.Q, true);
        //        //    QRCode qrCode = new QRCode(qrCodeData);
        //        //    using (Bitmap bitMap = qrCode.GetGraphic(20))
        //        //    {
        //        //        using (MemoryStream ms = new MemoryStream())
        //        //        {
        //        //            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        //            byte[] byteImage = ms.ToArray();
        //        //            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
        //        //            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
        //        //        }
        //        //    }
        //        //    qrcode = ImgReturn;

        //        //}
        //        //catch (Exception ex)
        //        //{



        //        //}


        //        if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null)
        //        {

        //            string BankAccount1 = "لا يوجد";
        //            string BankAccount2 = "لا يوجد";

        //            if (InvoicesVM.printBankAccount == true)
        //            {
        //                BankAccount1 = "لا يوجد";
        //                BankAccount2 = "لا يوجد";
        //            }
        //            else
        //            {
        //                BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
        //                BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
        //            }


        //            decimal totalDetailsPrice = 0;
        //            decimal totalDetailsTax = 0;
        //            decimal totalDiscount = 0;
        //            decimal percentage = 0;
        //            decimal Diff = 0;
        //            int Counter = 0;

        //            percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
        //            totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

        //            decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
        //            decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
        //            decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
        //            decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
        //            string remainder = (_totalVal - _paidValud).ToString();
        //            decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
        //            decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
        //            if (discountPercentage == default(decimal) && discountValue != default(decimal))
        //            {
        //                discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
        //            }
        //            if (discountPercentage != default(decimal) && discountValue == default(decimal))
        //            {
        //                discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
        //                //_totalVal = _totalVal - discountValue;
        //            }
        //            decimal netVal = 0;
        //            if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
        //            }
        //            else
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value;
        //            }




        //            string Address1 = "";
        //            string BuildingNumber1 = "";
        //            string StreetName1 = "";
        //            string Neighborhood1 = "";
        //            string CityName1 = "";
        //            string Country1 = "";
        //            string PostalCode1 = "";
        //            string ExternalPhone1 = "";
        //            string TaxCode1 = "";

        //            if (OrgIsRequired == true)
        //            {
        //                Address1 = objOrganization.Address;
        //                BuildingNumber1 = objOrganization.BuildingNumber;
        //                StreetName1 = objOrganization.StreetName;
        //                Neighborhood1 = objOrganization.Neighborhood;
        //                CityName1 = objOrganization.CityName;
        //                Country1 = objOrganization.Country;
        //                PostalCode1 = objOrganization.PostalCode;
        //                ExternalPhone1 = objOrganization.ExternalPhone;
        //                TaxCode1 = objOrganization.TaxCode;

        //            }
        //            else
        //            {
        //                Address1 = objBranch.Address;
        //                BuildingNumber1 = objBranch.BuildingNumber;
        //                StreetName1 = objBranch.StreetName;
        //                Neighborhood1 = objBranch.Neighborhood;
        //                CityName1 = objBranch.CityName;
        //                Country1 = objBranch.Country;
        //                PostalCode1 = objBranch.PostalCodeFinal;
        //                ExternalPhone1 = objBranch.ExternalPhone;
        //                TaxCode1 = objBranch.TaxCode;

        //            }

        //            //for information
        //            // VAT Category (O) "Not subject to VAT" (O)  غير خاضع لضريبة القيمة المضافة لابد ان يكون نسبة الضريبة صفر
        //            // VAT Category (E)   معفى من ضريبة القيمة المضافة نسبة الضريبة هتكون صفر ولابد من ذكر سبب الاعفاء :TaxExemptionReason
        //            // VAT Category (S)   ضريبة القيمة المضافة لابد من كتابة النسبة وتكون اكبر من صفر
        //            // VAT Category (Z)   Zero rated goods  البضائع الخاضعة للنسبة الصفرية 

        //            //PaymentMeansCode
        //            //10 In cash
        //            //30 Credit
        //            //42 Payment to bank account
        //            //48 Bank card
        //            //1 Instrument not defined(Free text)




        //            UBLXML ubl = new UBLXML();
        //            Invoice inv = new Invoice();
        //            Result res = new Result();

        //            inv.ID = InvoicesVM.InvoiceNumber.ToString(); // مثال SME00010
        //            inv.UUID = Guid.NewGuid().ToString();
        //            inv.IssueDate = InvoicesVM.Date.ToString();
        //            inv.IssueTime = "10:59:55";
        //            //
        //            //فاتورة  
        //            //383 اشعار مدين
        //            //381 اشعار دائن
        //            inv.invoiceTypeCode.id = 381;
        //            //inv.invoiceTypeCode.Name based on format NNPNESB
        //            //NN 01 فاتورة عادية
        //            //NN 02 فاتورة مبسطة
        //            //P فى حالة فاتورة لطرف ثالث نكتب 1 فى الحالة الاخرى نكتب 0
        //            //N فى حالة فاتورة اسمية نكتب 1 وفى الحالة الاخرى نكتب 0
        //            // E فى حالة فاتورة للصادرات نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //S فى حالة فاتورة ملخصة نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //B فى حالة فاتورة ذاتية نكتب 1
        //            //B فى حالة ان الفاتورة صادرات=1 لايمكن ان تكون الفاتورة ذاتية =1
        //            //
        //            inv.invoiceTypeCode.Name = "0200000";
        //            inv.DocumentCurrencyCode = "SAR";
        //            inv.TaxCurrencyCode = "SAR";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            //inv.billingReference.InvoiceDocumentReferenceID = "123654";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            inv.billingReference.InvoiceDocumentReferenceID = InvoicesVM.InvoiceNumber.ToString();
        //            // قيمة عداد الفاتورة
        //            inv.AdditionalDocumentReferenceICV.UUID = (int)InvoicesVM.InvoiceNumber; // لابد ان يكون ارقام فقط
        //                                                                                     //فى حالة فاتورة مبسطة وفاتورة ملخصة هانكتب تاريخ التسليم واخر تاريخ التسليم
        //                                                                                     // inv.delivery.ActualDeliveryDate = "2022-10-22";
        //                                                                                     // inv.delivery.LatestDeliveryDate = "2022-10-23";
        //                                                                                     //
        //                                                                                     //بيانات الدفع 
        //                                                                                     // اكواد معين
        //                                                                                     // اختيارى كود الدفع
        //            inv.paymentmeans.PaymentMeansCode = "10";//اختيارى
        //            inv.paymentmeans.InstructionNote = "Payment Notes"; //اجبارى فى الاشعارات
        //                                                                // inv.paymentmeans.payeefinancialaccount.ID = "";//اختيارى
        //                                                                // inv.paymentmeans.payeefinancialaccount.paymentnote = "Payment by credit";//اختيارى

        //            //بيانات البائع 
        //            inv.SupplierParty.partyIdentification.ID = "123456"; // رقم السجل التجارى الخاض بالبائع
        //            inv.SupplierParty.partyIdentification.schemeID = "CRN"; //رقم السجل التجارى
        //            inv.SupplierParty.postalAddress.StreetName = StreetName1;// اجبارى
        //            inv.SupplierParty.postalAddress.AdditionalStreetName = ""; //اختيارى
        //            inv.SupplierParty.postalAddress.BuildingNumber = BuildingNumber1; // اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.SupplierParty.postalAddress.CityName = CityName1; //اسم المدينة
        //            inv.SupplierParty.postalAddress.PostalZone = PostalCode1;//الرقم البريدي
        //            inv.SupplierParty.postalAddress.CountrySubentity = CityName1;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.SupplierParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.SupplierParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.SupplierParty.partyLegalEntity.RegistrationName = objOrganization.NameAr; // اسم الشركة المسجل فى الهيئة
        //            inv.SupplierParty.partyTaxScheme.CompanyID = "301121971100003";//TaxCode1;// رقم التسجيل الضريبي

        //            // بيانات المشترى
        //            inv.CustomerParty.partyIdentification.ID = "123456";// رقم السجل التجارى الخاض بالمشترى
        //            inv.CustomerParty.partyIdentification.schemeID = "NAT";//رقم السجل التجارى
        //            inv.CustomerParty.postalAddress.StreetName = CustomerVM.StreetName;// اجبارى
        //            inv.CustomerParty.postalAddress.AdditionalStreetName = "";//اختيارى
        //            inv.CustomerParty.postalAddress.BuildingNumber = CustomerVM.BuildingNumber;// اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.CustomerParty.postalAddress.CityName = CustomerVM.CityName; //اسم المدينة
        //            inv.CustomerParty.postalAddress.PostalZone = CustomerVM.PostalCodeFinal;//الرقم البريدي
        //            inv.CustomerParty.postalAddress.CountrySubentity = CustomerVM.CityName;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.CustomerParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.CustomerParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.CustomerParty.partyLegalEntity.RegistrationName = CustomerVM.CustomerNameAr;// اسم الشركة المسجل فى الهيئة
        //            inv.CustomerParty.partyTaxScheme.CompanyID = "301121972500003";// CustomerVM.CommercialRegister;// رقم التسجيل الضريبي



        //            AllowanceCharge allowancecharge = new AllowanceCharge();

        //            allowancecharge.Amount = 0;
        //            allowancecharge.AllowanceChargeReason = "discount"; //سبب الخصم
        //            allowancecharge.taxCategory.ID = "S";// كود الضريبة
        //            allowancecharge.taxCategory.Percent = 15;// نسبة الضريبة
        //            inv.allowanceCharges.Add(allowancecharge);

        //            // فى حالة فى اكتر من منتج فى الفاتورة هانعمل ليست من invoiceline مثال الكود التالى
        //            foreach (var item in VoucherDetailsVM)
        //            {

        //                Counter = Counter + 1;
        //                Diff = 0;
        //                totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
        //                totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
        //                Diff = Convert.ToDecimal(item.Amount * item.Qty);


        //                InvoiceLine invline = new InvoiceLine();
        //                invline.InvoiceQuantity = (int)item.Qty;

        //                invline.item.classifiedTaxCategory.ID = "S";// كود الضريبة
        //                invline.item.classifiedTaxCategory.Percent = 15;// نسبة الضريبة

        //                //if the price is including vat set EncludingVat=true;
        //                if (item.TaxType == 3)
        //                {
        //                    invline.price.EncludingVat = true;
        //                }
        //                else
        //                {
        //                    invline.price.EncludingVat = false;

        //                }

        //                invline.item.Name = item.ServicesPriceName;
        //                invline.price.PriceAmount = (float)item.Amount;

        //                invline.price.allowanceCharge.AllowanceChargeReason = "discount"; //سبب الخصم على مستوى المنتج
        //                invline.price.allowanceCharge.Amount = 0;//قيم الخصم


        //                invline.taxTotal.TaxSubtotal.taxCategory.ID = "S";//كود الضريبة
        //                invline.taxTotal.TaxSubtotal.taxCategory.Percent = 15;//نسبة الضريبة
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Private healthcare to citizen";
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-HEA";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "";

        //                inv.InvoiceLines.Add(invline);
        //            }





        //            res = ubl.GenerateInvoiceXML(inv, Server.MapPath("~"));

        //            if (res.IsValid)
        //            {

        //                Invoices invoiceupdate = new Invoices();
        //                invoiceupdate.InvoiceId = (int)invoiceid;
        //                invoiceupdate.InvoiceHash = res.InvoiceHash;
        //                invoiceupdate.SingedXML = res.SingedXML;
        //                invoiceupdate.EncodedInvoice = res.EncodedInvoice;
        //                invoiceupdate.ZatcaUUID = res.UUID;
        //                invoiceupdate.QRCode = res.QRCode;
        //                invoiceupdate.PIH = res.PIH;
        //                invoiceupdate.SingedXMLFileName = res.SingedXMLFileName;

        //                _voucherService.UpdateInvoiceWithZatcaData(invoiceupdate, _globalshared.UserId_G, _globalshared.BranchId_G);

        //                //القيم التالية تحتاج ان تحفظها فى سطر الفاتورة فى قاعدة البيانات الخاصة بكم  كي تكون مرجع لكم لاحقاً
        //                //MessageBox.Show(res.InvoiceHash);
        //                //MessageBox.Show(res.SingedXML);
        //                //MessageBox.Show(res.EncodedInvoice);
        //                //MessageBox.Show(res.UUID);
        //                //MessageBox.Show(res.QRCode);
        //                //MessageBox.Show(res.PIH);
        //                //MessageBox.Show(res.SingedXMLFileName);
        //            }
        //            else
        //            {
        //                //MessageBox.Show(res.ErrorMessage);
        //                return Ok(res.ErrorMessage);
        //                //return;
        //            }


        //            ApiRequestLogic apireqlogic = new ApiRequestLogic();
        //            ComplianceCsrResponse tokenresponse = new ComplianceCsrResponse();
        //            InvoiceReportingRequest invrequestbody = new InvoiceReportingRequest();
        //            tokenresponse = apireqlogic.GetComplianceCSIDAPI("12345", "", Server.MapPath("~"));
        //            //tokenresponse1 = apireqlogic.GetProductionCSIDAPI(tokenresponse.RequestId, tokenresponse.BinarySecurityToken, tokenresponse.Secret); ;
        //            if (string.IsNullOrEmpty(tokenresponse.ErrorMessage))
        //            {
        //                //MessageBox.Show(tokenresponse.BinarySecurityToken);
        //                invrequestbody.invoice = res.EncodedInvoice;
        //                invrequestbody.invoiceHash = res.InvoiceHash;
        //                invrequestbody.uuid = res.UUID;
        //                InvoiceReportingResponse invoicereportingmodel = apireqlogic.CallComplianceInvoiceAPI(tokenresponse.BinarySecurityToken, tokenresponse.Secret, invrequestbody);
        //                if (string.IsNullOrEmpty(invoicereportingmodel.ErrorMessage))
        //                {
        //                    return Ok("");
        //                    //REPORTED =>Passed Successfuly
        //                }
        //                else
        //                {
        //                    return Ok("");
        //                }
        //            }
        //            else
        //            {
        //                return Ok("");
        //            }
        //        }
        //    }
        //    return Ok("");

        //}

        ////اشعار دائن فاتورة عادية
        //public IActionResult btn_StandardCreditNote_Click(int? invoiceid)
        //{



        //    string FileName = "";
        //    string xmlFile2 = "";
        //    if (invoiceid.HasValue)
        //    {
        //        DataTable voucher = _voucherService.ReceiptCashingPaying((int)invoiceid, Con);

        //        InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0);
        //        CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, Lang);

        //        var costCenterNam = "";
        //        if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
        //        {
        //            try
        //            {
        //                costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).NameAr;
        //            }
        //            catch (Exception)
        //            {
        //                costCenterNam = "بدون";
        //            }
        //        }
        //        else
        //        {
        //            costCenterNam = "بدون";
        //        }
        //        //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
        //        string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

        //        List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).ToList();
        //        int orgId = _BranchesService.GetOrganizationId(BranchId);
        //        var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId);
        //        if (objOrganization != null)
        //            ViewData["Org_VD"] = objOrganization;
        //        else
        //            ViewData["Org_VD"] = null;


        //        var objBranch = _BranchesService.GetBranchByBranchId(Lang, _globalshared.BranchId_G).FirstOrDefault();
        //        var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(BranchId).OrgDataIsRequired;

                       // if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;


        //        DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
        //        string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
        //        var SupplierName = objOrganization.NameAr.TrimStart();
        //        SupplierName = SupplierName.TrimEnd();
        //        string qrBarcodeHash = QRCodeEncoder.encode(
        //            new Seller_Inv(SupplierName),
        //            new TaxNumber_Inv(objOrganization.TaxCode),
        //            new InvoiceDate_Inv(ActionDate),
        //            new TotalAmount_Inv(InvoicesVM.TotalValue.ToString()),
        //            new TaxAmount_Inv(InvoicesVM.TaxAmount.ToString())
        //        );

        //        //string qrcode = "";
        //        //try
        //        //{
        //        //    string ImgReturn = "";

        //        //    QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrBarcodeHash, QRCodeGenerator.ECCLevel.Q, true);
        //        //    QRCode qrCode = new QRCode(qrCodeData);
        //        //    using (Bitmap bitMap = qrCode.GetGraphic(20))
        //        //    {
        //        //        using (MemoryStream ms = new MemoryStream())
        //        //        {
        //        //            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        //            byte[] byteImage = ms.ToArray();
        //        //            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
        //        //            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
        //        //        }
        //        //    }
        //        //    qrcode = ImgReturn;

        //        //}
        //        //catch (Exception ex)
        //        //{



        //        //}


        //        if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null)
        //        {

        //            string BankAccount1 = "لا يوجد";
        //            string BankAccount2 = "لا يوجد";

        //            if (InvoicesVM.printBankAccount == true)
        //            {
        //                BankAccount1 = "لا يوجد";
        //                BankAccount2 = "لا يوجد";
        //            }
        //            else
        //            {
        //                BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
        //                BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
        //            }


        //            decimal totalDetailsPrice = 0;
        //            decimal totalDetailsTax = 0;
        //            decimal totalDiscount = 0;
        //            decimal percentage = 0;
        //            decimal Diff = 0;
        //            int Counter = 0;

        //            percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
        //            totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

        //            decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
        //            decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
        //            decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
        //            decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
        //            string remainder = (_totalVal - _paidValud).ToString();
        //            decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
        //            decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
        //            if (discountPercentage == default(decimal) && discountValue != default(decimal))
        //            {
        //                discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
        //            }
        //            if (discountPercentage != default(decimal) && discountValue == default(decimal))
        //            {
        //                discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
        //                //_totalVal = _totalVal - discountValue;
        //            }
        //            decimal netVal = 0;
        //            if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
        //            }
        //            else
        //            {
        //                netVal = InvoicesVM.InvoiceValue.Value;
        //            }




        //            string Address1 = "";
        //            string BuildingNumber1 = "";
        //            string StreetName1 = "";
        //            string Neighborhood1 = "";
        //            string CityName1 = "";
        //            string Country1 = "";
        //            string PostalCode1 = "";
        //            string ExternalPhone1 = "";
        //            string TaxCode1 = "";

        //            if (OrgIsRequired == true)
        //            {
        //                Address1 = objOrganization.Address;
        //                BuildingNumber1 = objOrganization.BuildingNumber;
        //                StreetName1 = objOrganization.StreetName;
        //                Neighborhood1 = objOrganization.Neighborhood;
        //                CityName1 = objOrganization.CityName;
        //                Country1 = objOrganization.Country;
        //                PostalCode1 = objOrganization.PostalCode;
        //                ExternalPhone1 = objOrganization.ExternalPhone;
        //                TaxCode1 = objOrganization.TaxCode;

        //            }
        //            else
        //            {
        //                Address1 = objBranch.Address;
        //                BuildingNumber1 = objBranch.BuildingNumber;
        //                StreetName1 = objBranch.StreetName;
        //                Neighborhood1 = objBranch.Neighborhood;
        //                CityName1 = objBranch.CityName;
        //                Country1 = objBranch.Country;
        //                PostalCode1 = objBranch.PostalCodeFinal;
        //                ExternalPhone1 = objBranch.ExternalPhone;
        //                TaxCode1 = objBranch.TaxCode;

        //            }

        //            //for information
        //            // VAT Category (O) "Not subject to VAT" (O)  غير خاضع لضريبة القيمة المضافة لابد ان يكون نسبة الضريبة صفر
        //            // VAT Category (E)   معفى من ضريبة القيمة المضافة نسبة الضريبة هتكون صفر ولابد من ذكر سبب الاعفاء :TaxExemptionReason
        //            // VAT Category (S)   ضريبة القيمة المضافة لابد من كتابة النسبة وتكون اكبر من صفر
        //            // VAT Category (Z)   Zero rated goods  البضائع الخاضعة للنسبة الصفرية 

        //            //PaymentMeansCode
        //            //10 In cash
        //            //30 Credit
        //            //42 Payment to bank account
        //            //48 Bank card
        //            //1 Instrument not defined(Free text)




        //            UBLXML ubl = new UBLXML();
        //            Invoice inv = new Invoice();
        //            Result res = new Result();

        //            inv.ID = InvoicesVM.InvoiceNumber.ToString(); // مثال SME00010
        //            inv.UUID = Guid.NewGuid().ToString();
        //            inv.IssueDate = InvoicesVM.Date.ToString();
        //            inv.IssueTime = "10:59:55";
        //            //
        //            //فاتورة  
        //            //383 اشعار مدين
        //            //381 اشعار دائن
        //            inv.invoiceTypeCode.id = 381;
        //            //inv.invoiceTypeCode.Name based on format NNPNESB
        //            //NN 01 فاتورة عادية
        //            //NN 02 فاتورة مبسطة
        //            //P فى حالة فاتورة لطرف ثالث نكتب 1 فى الحالة الاخرى نكتب 0
        //            //N فى حالة فاتورة اسمية نكتب 1 وفى الحالة الاخرى نكتب 0
        //            // E فى حالة فاتورة للصادرات نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //S فى حالة فاتورة ملخصة نكتب 1 وفى الحالة الاخرى نكتب 0
        //            //B فى حالة فاتورة ذاتية نكتب 1
        //            //B فى حالة ان الفاتورة صادرات=1 لايمكن ان تكون الفاتورة ذاتية =1
        //            //
        //            inv.invoiceTypeCode.Name = "0100000";
        //            inv.DocumentCurrencyCode = "SAR";
        //            inv.TaxCurrencyCode = "SAR";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            //inv.billingReference.InvoiceDocumentReferenceID = "123654";
        //            // فى حالة ان اشعار دائن او مدين فقط هانكتب رقم الفاتورة اللى اصدرنا الاشعار ليها
        //            inv.billingReference.InvoiceDocumentReferenceID = InvoicesVM.InvoiceNumber.ToString();
        //            // قيمة عداد الفاتورة
        //            inv.AdditionalDocumentReferenceICV.UUID = (int)InvoicesVM.InvoiceNumber; // لابد ان يكون ارقام فقط
        //                                                                                     //فى حالة فاتورة مبسطة وفاتورة ملخصة هانكتب تاريخ التسليم واخر تاريخ التسليم
        //                                                                                     // inv.delivery.ActualDeliveryDate = "2022-10-22";
        //                                                                                     // inv.delivery.LatestDeliveryDate = "2022-10-23";
        //                                                                                     //
        //                                                                                     //بيانات الدفع 
        //                                                                                     // اكواد معين
        //                                                                                     // اختيارى كود الدفع
        //            inv.paymentmeans.PaymentMeansCode = "10";//اختيارى
        //            inv.paymentmeans.InstructionNote = "Payment Notes"; //اجبارى فى الاشعارات
        //                                                                // inv.paymentmeans.payeefinancialaccount.ID = "";//اختيارى
        //                                                                // inv.paymentmeans.payeefinancialaccount.paymentnote = "Payment by credit";//اختيارى

        //            //بيانات البائع 
        //            inv.SupplierParty.partyIdentification.ID = "123456"; // رقم السجل التجارى الخاض بالبائع
        //            inv.SupplierParty.partyIdentification.schemeID = "CRN"; //رقم السجل التجارى
        //            inv.SupplierParty.postalAddress.StreetName = StreetName1;// اجبارى
        //            inv.SupplierParty.postalAddress.AdditionalStreetName = ""; //اختيارى
        //            inv.SupplierParty.postalAddress.BuildingNumber = BuildingNumber1; // اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.SupplierParty.postalAddress.CityName = CityName1; //اسم المدينة
        //            inv.SupplierParty.postalAddress.PostalZone = PostalCode1;//الرقم البريدي
        //            inv.SupplierParty.postalAddress.CountrySubentity = CityName1;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.SupplierParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.SupplierParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.SupplierParty.partyLegalEntity.RegistrationName = objOrganization.NameAr; // اسم الشركة المسجل فى الهيئة
        //            inv.SupplierParty.partyTaxScheme.CompanyID = "301121971100003";//TaxCode1;// رقم التسجيل الضريبي

        //            // بيانات المشترى
        //            inv.CustomerParty.partyIdentification.ID = "123456";// رقم السجل التجارى الخاض بالمشترى
        //            inv.CustomerParty.partyIdentification.schemeID = "NAT";//رقم السجل التجارى
        //            inv.CustomerParty.postalAddress.StreetName = CustomerVM.StreetName;// اجبارى
        //            inv.CustomerParty.postalAddress.AdditionalStreetName = "";//اختيارى
        //            inv.CustomerParty.postalAddress.BuildingNumber = CustomerVM.BuildingNumber;// اجبارى رقم المبنى
        //            inv.SupplierParty.postalAddress.PlotIdentification = "9833";//اختيارى رقم القطعة
        //            inv.CustomerParty.postalAddress.CityName = CustomerVM.CityName; //اسم المدينة
        //            inv.CustomerParty.postalAddress.PostalZone = CustomerVM.PostalCodeFinal;//الرقم البريدي
        //            inv.CustomerParty.postalAddress.CountrySubentity = CustomerVM.CityName;//اسم المحافظة او المدينة مثال (مكة) اختيارى
        //            inv.CustomerParty.postalAddress.CitySubdivisionName = "";// اسم المنطقة او الحى 
        //            inv.CustomerParty.postalAddress.country.IdentificationCode = "SA";
        //            inv.CustomerParty.partyLegalEntity.RegistrationName = CustomerVM.CustomerNameAr;// اسم الشركة المسجل فى الهيئة
        //            inv.CustomerParty.partyTaxScheme.CompanyID = "301121972500003";// CustomerVM.CommercialRegister;// رقم التسجيل الضريبي



        //            AllowanceCharge allowancecharge = new AllowanceCharge();

        //            allowancecharge.Amount = 0;
        //            allowancecharge.AllowanceChargeReason = "discount"; //سبب الخصم
        //            allowancecharge.taxCategory.ID = "S";// كود الضريبة
        //            allowancecharge.taxCategory.Percent = 15;// نسبة الضريبة
        //            inv.allowanceCharges.Add(allowancecharge);

        //            // فى حالة فى اكتر من منتج فى الفاتورة هانعمل ليست من invoiceline مثال الكود التالى
        //            foreach (var item in VoucherDetailsVM)
        //            {

        //                Counter = Counter + 1;
        //                Diff = 0;
        //                totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
        //                totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
        //                Diff = Convert.ToDecimal(item.Amount * item.Qty);


        //                InvoiceLine invline = new InvoiceLine();
        //                invline.InvoiceQuantity = (int)item.Qty;

        //                invline.item.classifiedTaxCategory.ID = "S";// كود الضريبة
        //                invline.item.classifiedTaxCategory.Percent = 15;// نسبة الضريبة

        //                //if the price is including vat set EncludingVat=true;
        //                if (item.TaxType == 3)
        //                {
        //                    invline.price.EncludingVat = true;
        //                }
        //                else
        //                {
        //                    invline.price.EncludingVat = false;

        //                }

        //                invline.item.Name = item.ServicesPriceName;
        //                invline.price.PriceAmount = (float)item.Amount;

        //                invline.price.allowanceCharge.AllowanceChargeReason = "discount"; //سبب الخصم على مستوى المنتج
        //                invline.price.allowanceCharge.Amount = 0;//قيم الخصم


        //                invline.taxTotal.TaxSubtotal.taxCategory.ID = "S";//كود الضريبة
        //                invline.taxTotal.TaxSubtotal.taxCategory.Percent = 15;//نسبة الضريبة
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Private healthcare to citizen";
        //                                                                      //invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-HEA";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "";
        //                invline.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "";

        //                inv.InvoiceLines.Add(invline);
        //            }





        //            res = ubl.GenerateInvoiceXML(inv, Server.MapPath("~"));

        //            if (res.IsValid)
        //            {
        //                Invoices invoiceupdate = new Invoices();
        //                invoiceupdate.InvoiceId = (int)invoiceid;
        //                invoiceupdate.InvoiceHash = res.InvoiceHash;
        //                invoiceupdate.SingedXML = res.SingedXML;
        //                invoiceupdate.EncodedInvoice = res.EncodedInvoice;
        //                invoiceupdate.ZatcaUUID = res.UUID;
        //                invoiceupdate.QRCode = res.QRCode;
        //                invoiceupdate.PIH = res.PIH;
        //                invoiceupdate.SingedXMLFileName = res.SingedXMLFileName;

        //                _voucherService.UpdateInvoiceWithZatcaData(invoiceupdate, _globalshared.UserId_G, _globalshared.BranchId_G);

        //                //القيم التالية تحتاج ان تحفظها فى سطر الفاتورة فى قاعدة البيانات الخاصة بكم  كي تكون مرجع لكم لاحقاً
        //                //MessageBox.Show(res.InvoiceHash);
        //                //MessageBox.Show(res.SingedXML);
        //                //MessageBox.Show(res.EncodedInvoice);
        //                //MessageBox.Show(res.UUID);
        //                //MessageBox.Show(res.QRCode);
        //                //MessageBox.Show(res.PIH);
        //                //MessageBox.Show(res.SingedXMLFileName);
        //            }
        //            else
        //            {
        //                //MessageBox.Show(res.ErrorMessage);
        //                return Ok(res.ErrorMessage);
        //                //return;
        //            }


        //            ApiRequestLogic apireqlogic = new ApiRequestLogic();
        //            ComplianceCsrResponse tokenresponse = new ComplianceCsrResponse();
        //            InvoiceReportingRequest invrequestbody = new InvoiceReportingRequest();
        //            tokenresponse = apireqlogic.GetComplianceCSIDAPI("12345", "", Server.MapPath("~"));
        //            //tokenresponse1 = apireqlogic.GetProductionCSIDAPI(tokenresponse.RequestId, tokenresponse.BinarySecurityToken, tokenresponse.Secret); ;
        //            if (string.IsNullOrEmpty(tokenresponse.ErrorMessage))
        //            {
        //                //MessageBox.Show(tokenresponse.BinarySecurityToken);
        //                invrequestbody.invoice = res.EncodedInvoice;
        //                invrequestbody.invoiceHash = res.InvoiceHash;
        //                invrequestbody.uuid = res.UUID;
        //                InvoiceReportingResponse invoicereportingmodel = apireqlogic.CallComplianceInvoiceAPI(tokenresponse.BinarySecurityToken, tokenresponse.Secret, invrequestbody);
        //                if (string.IsNullOrEmpty(invoicereportingmodel.ErrorMessage))
        //                {
        //                    return Ok("");
        //                    //REPORTED =>Passed Successfuly
        //                }
        //                else
        //                {
        //                    return Ok("");
        //                }
        //            }
        //            else
        //            {
        //                return Ok("");
        //            }
        //        }
        //    }
        //    return Ok("");

        //}




        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="invoiceid"></param>
        /// <returns></returns>


        [HttpGet("createxmlublstandard")]

        public string createxmlublstandard(int? invoiceid)
        {
            try
            {
                string FileName = "";
                string xmlFile2 = "";
                if (invoiceid.HasValue)
                {
                    InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0).Result;
                    CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, _globalshared.Lang_G).Result;

                    var costCenterNam = "";
                    if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
                    {
                        try
                        {
                            costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
                        }
                        catch (Exception)
                        {
                            costCenterNam = "بدون";
                        }
                    }
                    else
                    {
                        costCenterNam = "بدون";
                    }
                    //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
                    string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

                    List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).Result.ToList();
                    int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                    var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
                    //if (objOrganization != null)
                    //    ViewData["Org_VD"] = objOrganization;
                    //else
                    //    ViewData["Org_VD"] = null;


                    var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
                    var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(_globalshared.BranchId_G).Result.OrgDataIsRequired;
                    if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;



                    DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
                    string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                    var SupplierName = objOrganization.NameAr.TrimStart();
                    SupplierName = SupplierName.TrimEnd();

                    string qrcode = "";
                    try
                    {
                        string ImgReturn = "";

                        TLVCls tlv = new TLVCls(SupplierName, objOrganization.TaxCode, Date, (double)InvoicesVM.TotalValue, (double)InvoicesVM.TaxAmount);

                        using (Bitmap bitMap = tlv.toQrCode())
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                byte[] byteImage = ms.ToArray();
                                System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                                ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                            }
                        }                       
                        qrcode = ImgReturn;

                    }
                    catch (Exception ex)
                    {



                    }







                    if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null && qrcode != null)
                    {

                        string BankAccount1 = "لا يوجد";
                        string BankAccount2 = "لا يوجد";

                        if (InvoicesVM.printBankAccount == true)
                        {
                            BankAccount1 = "لا يوجد";
                            BankAccount2 = "لا يوجد";
                        }
                        else
                        {
                            BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
                            BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
                        }


                        decimal totalDetailsPrice = 0;
                        decimal totalDetailsTax = 0;
                        decimal totalDiscount = 0;
                        decimal percentage = 0;
                        decimal Diff = 0;
                        int Counter = 0;

                        percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
                        totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

                        decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
                        decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
                        decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
                        decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
                        string remainder = (_totalVal - _paidValud).ToString();
                        decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
                        decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
                        if (discountPercentage == default(decimal) && discountValue != default(decimal))
                        {
                            discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
                        }
                        if (discountPercentage != default(decimal) && discountValue == default(decimal))
                        {
                            discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
                            //_totalVal = _totalVal - discountValue;
                        }
                        decimal netVal = 0;
                        if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
                        {
                            netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
                        }
                        else
                        {
                            netVal = InvoicesVM.InvoiceValue.Value;
                        }



                        XmlDocument xdoc = new XmlDocument();
                        var xmlstappath = Path.Combine(@"~\Helpers\XMLFile1.xml");

                        xdoc.Load(xmlstappath);

                        string Address1 = "";
                        string BuildingNumber1 = "";
                        string StreetName1 = "";
                        string Neighborhood1 = "";
                        string CityName1 = "";
                        string Country1 = "";
                        string PostalCode1 = "";
                        string ExternalPhone1 = "";
                        string TaxCode1 = "";
                        if (OrgIsRequired == true)
                        {
                            Address1 = objOrganization.Address;
                            BuildingNumber1 = objOrganization.BuildingNumber;
                            StreetName1 = objOrganization.StreetName;
                            Neighborhood1 = objOrganization.Neighborhood;
                            CityName1 = objOrganization.CityName;
                            Country1 = objOrganization.Country;
                            PostalCode1 = objOrganization.PostalCode;
                            ExternalPhone1 = objOrganization.ExternalPhone;
                            TaxCode1 = objOrganization.TaxCode;

                        }
                        else
                        {
                            Address1 = objBranch.Address;
                            BuildingNumber1 = objBranch.BuildingNumber;
                            StreetName1 = objBranch.StreetName;
                            Neighborhood1 = objBranch.Neighborhood;
                            CityName1 = objBranch.CityName;
                            Country1 = objBranch.Country;
                            PostalCode1 = objBranch.PostalCodeFinal;
                            ExternalPhone1 = objBranch.ExternalPhone;
                            TaxCode1 = objBranch.TaxCode;

                        }

                        /////////////////////////////////////////////////////////////////////////////seller///////////////////////////
                        ///

                        var xmlstappath2 = Path.Combine(@"~\Helpers\XMLFile1.xml");


                        XDocument doc = XDocument.Load(xmlstappath2);
                        XElement invoice = (XElement)doc.Descendants().Where(x => x.Name.LocalName == "Invoice").FirstOrDefault();

                        XNamespace cacNs = invoice.GetNamespaceOfPrefix("cac");
                        XNamespace cbcNs = invoice.GetNamespaceOfPrefix("cbc");

                        ////////////////////////////////////////////////////INVOICE DATA////////////////////////////////////////////////////////////////////////
                        XElement ProfileId = new XElement(cbcNs + "ProfileID");
                        ProfileId.Value = "reporting:1.0";
                        invoice.Add(ProfileId);
                        XElement InvoiceNo = new XElement(cbcNs + "ID");
                        InvoiceNo.Value = InvoicesVM.InvoiceNumber.ToString();
                        invoice.Add(InvoiceNo);
                        XElement UUID = new XElement(cbcNs + "UUID");
                        UUID.Value = Guid.NewGuid().ToString();
                        invoice.Add(UUID);
                        XElement IssueDate = new XElement(cbcNs + "IssueDate");
                        IssueDate.Value = InvoicesVM.Date.ToString();
                        invoice.Add(IssueDate);
                        XElement IssueTime = new XElement(cbcNs + "IssueTime");
                        IssueTime.Value = "09:32:12";
                        invoice.Add(IssueTime);


                        XElement InvoiceTypeCode = new XElement(cbcNs + "InvoiceTypeCode");
                        InvoiceTypeCode.Value = "388";
                        invoice.Add(InvoiceTypeCode);

                        XElement DocumentCurrencyCode = new XElement(cbcNs + "DocumentCurrencyCode");
                        DocumentCurrencyCode.Value = "SAR";
                        invoice.Add(DocumentCurrencyCode);

                        XElement TaxCurrencyCode = new XElement(cbcNs + "TaxCurrencyCode");
                        TaxCurrencyCode.Value = "SAR";
                        invoice.Add(TaxCurrencyCode);

                        XElement AdditionalDocumentReference = new XElement(cacNs + "AdditionalDocumentReference");
                        invoice.Add(AdditionalDocumentReference);

                        XElement ID = new XElement(cbcNs + "ID");
                        ID.Value = "";
                        AdditionalDocumentReference.Add(ID);

                        XElement UUID2 = new XElement(cbcNs + "UUID");
                        UUID2.Value = InvoicesVM.InvoiceNumber.ToString();
                        AdditionalDocumentReference.Add(UUID2);


                        //XElement QRCodeString_VD = new XElement(cbcNs + "QRCodeString_VD");
                        //QRCodeString_VD.Value = qrcode.ToString();
                        //invoice.Add(QRCodeString_VD);





                        /////////////////////////////////////////////////////SUPLIER///////////////////////////////////////////////////////////////////////

                        XElement seller = new XElement(cacNs + "AccountingSupplierParty");
                        invoice.Add(seller);

                        XElement Party = new XElement(cacNs + "Party");

                        seller.Add(Party);
                        XElement PartyIdentification = new XElement(cacNs + "PartyIdentification");

                        Party.Add(PartyIdentification);


                        XElement sellerID = new XElement(cbcNs + "ID");
                        sellerID.Value = objOrganization.NameAr.ToString();
                        PartyIdentification.Add(sellerID);

                        XElement PostalAddressstxt = new XElement(cacNs + "PostalAddress");

                        Party.Add(PostalAddressstxt);


                        XElement StreetNametxt = new XElement(cbcNs + "StreetName");
                        StreetNametxt.Value = StreetName1;
                        PostalAddressstxt.Add(StreetNametxt);

                        XElement BuildingNumbertxt = new XElement(cbcNs + "BuildingNumber");
                        BuildingNumbertxt.Value = BuildingNumber1;
                        PostalAddressstxt.Add(BuildingNumbertxt);


                        XElement PlotIdentificationtxt = new XElement(cbcNs + "PlotIdentification");
                        PlotIdentificationtxt.Value = PostalCode1;
                        PostalAddressstxt.Add(PlotIdentificationtxt);


                        XElement CitySubdivisionNametxt = new XElement(cbcNs + "CitySubdivisionName");
                        CitySubdivisionNametxt.Value = CityName1;
                        PostalAddressstxt.Add(CitySubdivisionNametxt);


                        XElement CityNametxt = new XElement(cbcNs + "CityName");
                        CityNametxt.Value = CityName1;
                        PostalAddressstxt.Add(CityNametxt);


                        XElement PostalZonetxt = new XElement(cbcNs + "PostalZone");
                        PostalZonetxt.Value = PostalCode1;
                        PostalAddressstxt.Add(PostalZonetxt);

                        XElement Countrytxt = new XElement(cacNs + "Country");

                        PostalAddressstxt.Add(Countrytxt);


                        XElement IdentificationCode = new XElement(cbcNs + "IdentificationCode");
                        IdentificationCode.Value = "SA";
                        Countrytxt.Add(IdentificationCode);


                        XElement PartyTaxScheme = new XElement(cacNs + "PartyTaxScheme");

                        Party.Add(PartyTaxScheme);

                        XElement CompanyID = new XElement(cbcNs + "CompanyID");
                        CompanyID.Value = "";
                        PartyTaxScheme.Add(CompanyID);

                        XElement TaxScheme = new XElement(cacNs + "PartyTaxScheme");

                        PartyTaxScheme.Add(TaxScheme);

                        XElement PartyTaxSchemeID = new XElement(cbcNs + "ID");
                        PartyTaxSchemeID.Value = "VAT";
                        TaxScheme.Add(PartyTaxSchemeID);

                        XElement PartyLegalEntity = new XElement(cacNs + "PartyLegalEntity");

                        Party.Add(PartyLegalEntity);

                        XElement RegistrationName = new XElement(cbcNs + "RegistrationName");
                        RegistrationName.Value = objOrganization.NameAr.ToString();
                        PartyLegalEntity.Add(RegistrationName);





                        //////////////////////////////////////////////////////////////////////       customer                //////////////////////////////////////////////////////////////////////////  




                        XElement customer = new XElement(cacNs + "AccountingCustomerParty");
                        invoice.Add(customer);

                        XElement CParty = new XElement(cacNs + "Party");
                        customer.Add(CParty);



                        XElement CPostalAddressstxt = new XElement(cacNs + "PostalAddress");
                        CParty.Add(CPostalAddressstxt);


                        XElement CStreetNametxt = new XElement(cbcNs + "StreetName");
                        CStreetNametxt.Value = CustomerVM.StreetName.ToString();
                        CPostalAddressstxt.Add(CStreetNametxt);


                        XElement CCitySubdivisionNametxt = new XElement(cbcNs + "CitySubdivisionName");
                        CCitySubdivisionNametxt.Value = CustomerVM.CityName.ToString();
                        CPostalAddressstxt.Add(CCitySubdivisionNametxt);


                        XElement CCountrytxt = new XElement(cacNs + "Country");

                        CPostalAddressstxt.Add(CCountrytxt);


                        XElement CIdentificationCode = new XElement(cbcNs + "IdentificationCode");
                        CIdentificationCode.Value = "SA";
                        CCountrytxt.Add(CIdentificationCode);


                        XElement CPartyTaxScheme = new XElement(cacNs + "PartyTaxScheme");

                        CParty.Add(CPartyTaxScheme);



                        XElement CTaxScheme = new XElement(cacNs + "PartyTaxScheme");

                        CPartyTaxScheme.Add(CTaxScheme);

                        XElement CPartyTaxSchemeID = new XElement(cbcNs + "ID");
                        CPartyTaxSchemeID.Value = "VAT";
                        CTaxScheme.Add(CPartyTaxSchemeID);

                        XElement CPartyLegalEntity = new XElement(cacNs + "PartyLegalEntity");

                        CParty.Add(CPartyLegalEntity);

                        XElement CRegistrationName = new XElement(cbcNs + "RegistrationName");
                        CRegistrationName.Value = CustomerVM.CustomerNameAr.ToString();
                        CPartyLegalEntity.Add(CRegistrationName);



                        ////////////////////////////////////////////////////////////////////////       Invoice Details                /////////////////////  
                        /////
                        ///


                        XElement PaymentMeans = new XElement(cacNs + "PaymentMeans");

                        invoice.Add(PaymentMeans);

                        XElement PaymentMeansCode = new XElement(cbcNs + "PaymentMeansCode");
                        PaymentMeansCode.Value = "VAT";
                        PaymentMeans.Add(PaymentMeansCode);


                        XElement AllowanceCharge = new XElement(cacNs + "AllowanceCharge");

                        invoice.Add(AllowanceCharge);

                        XElement ChargeIndicator = new XElement(cbcNs + "ChargeIndicator");
                        ChargeIndicator.Value = "false";
                        AllowanceCharge.Add(ChargeIndicator);

                        XElement Amount = new XElement(cbcNs + "Amount");
                        Amount.Value = "false";
                        AllowanceCharge.Add(Amount);

                        XElement TaxCategory = new XElement(cacNs + "TaxCategory");
                        AllowanceCharge.Add(TaxCategory);


                        XElement TaxCategoryID = new XElement(cbcNs + "ID");
                        TaxCategoryID.Value = "S";
                        TaxCategory.Add(TaxCategoryID);

                        XElement Percent = new XElement(cbcNs + "Percent");
                        Percent.Value = "15";
                        TaxCategory.Add(Percent);

                        XElement TaxScheme2 = new XElement(cacNs + "TaxScheme");
                        AllowanceCharge.Add(TaxScheme2);

                        XElement TaxSchemeID = new XElement(cbcNs + "ID");
                        TaxSchemeID.Value = "VAT";
                        TaxScheme2.Add(TaxSchemeID);




                        ///////////////////////////////////////////////////////////total////////////////////////////////////////////////////////////////////////
                        XElement TaxTotal = new XElement(cacNs + "TaxTotal");

                        invoice.Add(TaxTotal);

                        XElement TaxAmount = new XElement(cbcNs + "TaxAmount");
                        TaxAmount.Value = InvoicesVM.TaxAmount.ToString();
                        TaxTotal.Add(TaxAmount);


                        XElement TaxTotal2 = new XElement(cacNs + "TaxTotal");

                        invoice.Add(TaxTotal2);

                        XElement TaxAmount2 = new XElement(cbcNs + "TaxAmount");
                        TaxAmount2.Value = InvoicesVM.TaxAmount.ToString();
                        TaxTotal2.Add(TaxAmount2);

                        XElement TaxSubtotal = new XElement(cacNs + "TaxSubtotal");

                        invoice.Add(TaxSubtotal);

                        XElement TaxableAmount = new XElement(cbcNs + "TaxableAmount");
                        TaxableAmount.Value = InvoicesVM.TaxAmount.ToString();
                        TaxSubtotal.Add(TaxableAmount);

                        XElement TaxAmount3 = new XElement(cbcNs + "TaxAmount");
                        TaxAmount3.Value = InvoicesVM.TaxAmount.ToString();
                        TaxSubtotal.Add(TaxAmount3);


                        XElement TaxCategory2 = new XElement(cacNs + "TaxCategory");

                        TaxSubtotal.Add(TaxCategory2);


                        XElement TaxCategory2ID = new XElement(cbcNs + "ID");
                        TaxCategory2ID.Value = "s";
                        TaxCategory2.Add(TaxCategory2ID);

                        XElement TaxCategory2Percent = new XElement(cbcNs + "Percent");
                        TaxCategory2Percent.Value = "15";
                        TaxCategory2.Add(TaxCategory2Percent);


                        XElement TaxScheme3 = new XElement(cacNs + "TaxScheme");

                        TaxCategory2.Add(TaxScheme3);

                        XElement TaxScheme3ID = new XElement(cbcNs + "ID");
                        TaxScheme3ID.Value = "VAT";
                        TaxScheme3.Add(TaxScheme3ID);

                        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        ///

                        XElement LegalMonetaryTotal = new XElement(cacNs + "LegalMonetaryTotal");
                        invoice.Add(LegalMonetaryTotal);

                        XElement LineExtensionAmount = new XElement(cbcNs + "LineExtensionAmount");
                        LineExtensionAmount.Value = InvoicesVM.TaxAmount.ToString();
                        LegalMonetaryTotal.Add(LineExtensionAmount);
                        XElement TaxExclusiveAmount = new XElement(cbcNs + "TaxExclusiveAmount");
                        TaxExclusiveAmount.Value = InvoicesVM.TaxAmount.ToString();
                        LegalMonetaryTotal.Add(TaxExclusiveAmount);
                        XElement TaxInclusiveAmount = new XElement(cbcNs + "TaxInclusiveAmount");
                        TaxInclusiveAmount.Value = InvoicesVM.TaxAmount.ToString();
                        LegalMonetaryTotal.Add(TaxInclusiveAmount);
                        XElement AllowanceTotalAmount = new XElement(cbcNs + "AllowanceTotalAmount");
                        AllowanceTotalAmount.Value = InvoicesVM.TaxAmount.ToString();
                        LegalMonetaryTotal.Add(AllowanceTotalAmount);
                        XElement PrepaidAmount = new XElement(cbcNs + "PrepaidAmount");
                        PrepaidAmount.Value = InvoicesVM.TaxAmount.ToString();
                        LegalMonetaryTotal.Add(PrepaidAmount);
                        XElement PayableAmount = new XElement(cbcNs + "PayableAmount");
                        PayableAmount.Value = InvoicesVM.TaxAmount.ToString();
                        LegalMonetaryTotal.Add(PayableAmount);
                        ///////////////////////////////////////////////////////////////////////////////////invoice line/////////////////////////////////



                        foreach (var item in VoucherDetailsVM)
                        {

                            Counter = Counter + 1;
                            Diff = 0;
                            totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
                            totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
                            Diff = Convert.ToDecimal(item.Amount * item.Qty);


                            XElement InvoiceLine = new XElement(cacNs + "InvoiceLine");
                            invoice.Add(InvoiceLine);


                            XElement InvoiceLineID = new XElement(cbcNs + "ID");
                            InvoiceLineID.Value = Counter.ToString();
                            InvoiceLine.Add(InvoiceLineID);

                            XElement InvoicedQuantity = new XElement(cbcNs + "InvoicedQuantity");
                            InvoicedQuantity.Value = item.Qty.ToString();
                            InvoiceLine.Add(InvoicedQuantity);

                            XElement InvoiceLineExtensionAmount = new XElement(cbcNs + "LineExtensionAmount");
                            InvoiceLineExtensionAmount.Value = item.Amount.ToString();
                            InvoiceLine.Add(InvoiceLineExtensionAmount);

                            XElement Invoicetotal = new XElement(cacNs + "TaxTotal");
                            InvoiceLine.Add(Invoicetotal);

                            XElement invoiceTaxAmount = new XElement(cbcNs + "TaxAmount");
                            invoiceTaxAmount.Value = item.TaxAmount.ToString();
                            Invoicetotal.Add(invoiceTaxAmount);

                            XElement RoundingAmount = new XElement(cbcNs + "RoundingAmount");
                            RoundingAmount.Value = item.ServicesPriceName;
                            Invoicetotal.Add(RoundingAmount);

                            XElement Item = new XElement(cacNs + "Item");
                            InvoiceLine.Add(Item);


                            XElement Name = new XElement(cbcNs + "Name");
                            Name.Value = item.ServicesPriceName.ToString();
                            Item.Add(Name);


                            XElement ClassifiedTaxCategory = new XElement(cacNs + "ClassifiedTaxCategory");
                            Item.Add(ClassifiedTaxCategory);


                            XElement ItemID = new XElement(cbcNs + "ID");
                            ItemID.Value = "S";
                            Item.Add(ItemID);

                            XElement ItemPercent = new XElement(cbcNs + "Percent");
                            ItemPercent.Value = "15";
                            ClassifiedTaxCategory.Add(ItemPercent);


                            XElement ItemTaxScheme = new XElement(cacNs + "TaxScheme");
                            ClassifiedTaxCategory.Add(ItemTaxScheme);



                            XElement ItemTaxSchemeID = new XElement(cbcNs + "ID");
                            ItemTaxSchemeID.Value = item.ServicesPriceName;
                            ItemTaxScheme.Add(ItemTaxSchemeID);


                            XElement Price = new XElement(cacNs + "Price");
                            InvoiceLine.Add(Price);

                            XElement PriceAmount = new XElement(cbcNs + "PriceAmount");
                            PriceAmount.Value = item.Amount.ToString();
                            Price.Add(PriceAmount);



                            XElement PriceAllowanceCharge = new XElement(cacNs + "AllowanceCharge");
                            Price.Add(PriceAllowanceCharge);

                            XElement PriceChargeIndicator = new XElement(cbcNs + "ChargeIndicator");
                            PriceChargeIndicator.Value = item.Amount.ToString();
                            PriceAllowanceCharge.Add(PriceChargeIndicator);

                            XElement AllowanceChargeReason = new XElement(cbcNs + "AllowanceChargeReason");
                            AllowanceChargeReason.Value = item.Amount.ToString();
                            PriceAllowanceCharge.Add(AllowanceChargeReason);

                            XElement AllowanceChargeAmount = new XElement(cbcNs + "Amount");
                            AllowanceChargeAmount.Value = item.Amount.ToString();
                            PriceAllowanceCharge.Add(AllowanceChargeAmount);

                            //XElement Qtytxt = new XElement(cbcNs + "Qty");
                            //Qtytxt.Value = item.Qty.ToString();
                            //InvoiceDetails.Add(Qtytxt);
                            //XElement Pricetxt = new XElement(cbcNs + "Price");
                            //Pricetxt.Value = item.Amount.ToString();
                            //InvoiceDetails.Add(Pricetxt);
                            //XElement Nettxt = new XElement(cbcNs + "Net");
                            //Nettxt.Value = Diff.ToString();
                            //InvoiceDetails.Add(Nettxt);
                            //XElement VATtxt = new XElement(cbcNs + "VAT");
                            //VATtxt.Value = totalDetailsTax.ToString();
                            //InvoiceDetails.Add(VATtxt);
                            //XElement NetVATtxt = new XElement(cbcNs + "NetVAT");
                            //NetVATtxt.Value = item.TotalAmount.ToString();
                            //InvoiceDetails.Add(NetVATtxt);

                        }


                        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /////
                        ///


                        //XElement InvoiceDetails2 = new XElement(cacNs + "InvoiceDetails2");
                        //invoice.Add(InvoiceDetails2);


                        //XElement Totaltxt = new XElement(cbcNs + "Total");
                        //Totaltxt.Value = InvoicesVM.InvoiceValue.ToString();
                        //InvoiceDetails2.Add(Totaltxt);
                        //XElement Discountpercentagetxt = new XElement(cbcNs + "Discountpercentage");
                        //Discountpercentagetxt.Value = String.Format("{0:0.00}", discountPercentage);
                        //InvoiceDetails2.Add(Discountpercentagetxt);

                        //XElement Discountvaltxt = new XElement(cbcNs + "Discounttxt");
                        //Discountvaltxt.Value = String.Format("{0:0.00}", totalDiscount);
                        //InvoiceDetails2.Add(Discountvaltxt);

                        //XElement Nettxt2 = new XElement(cbcNs + "Net");
                        //Nettxt2.Value = String.Format("{0:0.00}", netVal);
                        //InvoiceDetails2.Add(Nettxt2);
                        //XElement VATtxt2 = new XElement(cbcNs + "VAT");
                        //VATtxt2.Value = InvoicesVM.TaxAmount.ToString();
                        //InvoiceDetails2.Add(VATtxt2);
                        //XElement Amount = new XElement(cbcNs + "Amount");
                        //Amount.Value = _totalVal.ToString();
                        //InvoiceDetails2.Add(Amount);
                        //XElement Paid = new XElement(cbcNs + "Paid");
                        //Paid.Value = _paidValud.ToString();
                        //InvoiceDetails2.Add(Paid);
                        //XElement Remaining = new XElement(cbcNs + "Remaining");
                        //Remaining.Value = remainder.ToString();
                        //InvoiceDetails2.Add(Remaining);

                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /////

                        //XElement Banckaccount = new XElement(cacNs + "BankAccounts");
                        //invoice.Add(Banckaccount);
                        //XElement bank1 = new XElement(cbcNs + "BankAccounts1");
                        //bank1.Value = BankAccount1;
                        //Banckaccount.Add(bank1);

                        //XElement bank2 = new XElement(cbcNs + "BankAccounts2");
                        //bank2.Value = BankAccount2;
                        //Banckaccount.Add(bank2);

                        //    XmlNodeList BankAccounts1txt = xdoc.GetElementsByTagName("cbc:BankAccounts1");
                        //XmlNodeList BankAccounts2ltxt = xdoc.GetElementsByTagName("cbc:BankAccounts2");


                        //foreach (XmlNode aDateNode in BankAccounts1txt)
                        //{
                        //    aDateNode.InnerText = BankAccount1;
                        //}
                        //foreach (XmlNode aDateNode in BankAccounts2ltxt)
                        //{
                        //    aDateNode.InnerText = BankAccount2;
                        //}


                        FileName = "Invoice_" + DateTime.Now.Ticks.ToString() + ".xml";
                        xmlFile2 = Path.Combine("~/Invoice/" + FileName + "");
                        doc.Save(xmlFile2);





                    }
                    //var TempVal = Server.MapPath("~/Invoice/" + FileName);
                    //var Signed_TempVal = Server.MapPath("~/Invoice/" + "Signed_" + FileName);
                    //SignedXML(TempVal, Signed_TempVal);
                }
                return FileName;
            }
            catch (Exception ex)
            {
                return "";
            }
        }


        [HttpGet("createxmlublsimplified")]
        public IActionResult createxmlublsimplified(int? invoiceid)
        {
            try
            {
                string FileName = "";
                string xmlFile2 = "";
                if (invoiceid.HasValue)
                {
                    DataTable voucher = _voucherService.ReceiptCashingPaying((int)invoiceid, Con).Result;

                    InvoicesVM InvoicesVM = _voucherService.GetVoucherById(invoiceid ?? 0).Result;
                    CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, _globalshared.Lang_G).Result;

                    var costCenterNam = "";
                    if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
                    {
                        try
                        {
                            costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
                        }
                        catch (Exception)
                        {
                            costCenterNam = "بدون";
                        }
                    }
                    else
                    {
                        costCenterNam = "بدون";
                    }
                    //string ValueNumString2 = ConvertNumToString(InvoicesVM.TotalValue.ToString());
                    string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());

                    List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(invoiceid).Result.ToList();
                    int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                    var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
                    //if (objOrganization != null)
                    //    ViewData["Org_VD"] = objOrganization;
                    //else
                    //    ViewData["Org_VD"] = null;


                    var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
                    var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(_globalshared.BranchId_G).Result.OrgDataIsRequired;
                    if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;



                    DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
                    string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                    var SupplierName = objOrganization.NameAr.TrimStart();
                    SupplierName = SupplierName.TrimEnd();

                    string qrcode = "";
                    try
                    {
                        string ImgReturn = "";

                        TLVCls tlv = new TLVCls(SupplierName, objOrganization.TaxCode, Date, (double)InvoicesVM.TotalValue, (double)InvoicesVM.TaxAmount);

                        using (Bitmap bitMap = tlv.toQrCode())
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                byte[] byteImage = ms.ToArray();
                                System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                                ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                            }
                        }
                        qrcode = ImgReturn;

                    }
                    catch (Exception ex)
                    {



                    }







                    if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null && qrcode != null)
                    {

                        string BankAccount1 = "لا يوجد";
                        string BankAccount2 = "لا يوجد";

                        if (InvoicesVM.printBankAccount == true)
                        {
                            BankAccount1 = "لا يوجد";
                            BankAccount2 = "لا يوجد";
                        }
                        else
                        {
                            BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
                            BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
                        }


                        decimal totalDetailsPrice = 0;
                        decimal totalDetailsTax = 0;
                        decimal totalDiscount = 0;
                        decimal percentage = 0;
                        decimal Diff = 0;
                        int Counter = 0;

                        percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
                        totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

                        decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
                        decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
                        decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
                        decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
                        string remainder = (_totalVal - _paidValud).ToString();
                        decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
                        decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
                        if (discountPercentage == default(decimal) && discountValue != default(decimal))
                        {
                            discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
                        }
                        if (discountPercentage != default(decimal) && discountValue == default(decimal))
                        {
                            discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
                            //_totalVal = _totalVal - discountValue;
                        }
                        decimal netVal = 0;
                        if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
                        {
                            netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
                        }
                        else
                        {
                            netVal = InvoicesVM.InvoiceValue.Value;
                        }



                        XmlDocument xdoc = new XmlDocument();
                        var xmlstappath = Path.Combine(@"~\Helpers\XMLFile1.xml");

                        xdoc.Load(xmlstappath);
                        string Address1 = "";
                        string BuildingNumber1 = "";
                        string StreetName1 = "";
                        string Neighborhood1 = "";
                        string CityName1 = "";
                        string Country1 = "";
                        string PostalCode1 = "";
                        string ExternalPhone1 = "";
                        string TaxCode1 = "";
                        if (OrgIsRequired == true)
                        {
                            Address1 = objOrganization.Address;
                            BuildingNumber1 = objOrganization.BuildingNumber;
                            StreetName1 = objOrganization.StreetName;
                            Neighborhood1 = objOrganization.Neighborhood;
                            CityName1 = objOrganization.CityName;
                            Country1 = objOrganization.Country;
                            PostalCode1 = objOrganization.PostalCode;
                            ExternalPhone1 = objOrganization.ExternalPhone;
                            TaxCode1 = objOrganization.TaxCode;

                        }
                        else
                        {
                            Address1 = objBranch.Address;
                            BuildingNumber1 = objBranch.BuildingNumber;
                            StreetName1 = objBranch.StreetName;
                            Neighborhood1 = objBranch.Neighborhood;
                            CityName1 = objBranch.CityName;
                            Country1 = objBranch.Country;
                            PostalCode1 = objBranch.PostalCodeFinal;
                            ExternalPhone1 = objBranch.ExternalPhone;
                            TaxCode1 = objBranch.TaxCode;

                        }

                        /////////////////////////////////////////////////////////////////////////////seller///////////////////////////
                        ///

                        var xmlstappath2 = Path.Combine(@"~\Helpers\XMLFile1.xml");


                        XDocument doc = XDocument.Load(xmlstappath2); XElement invoice = (XElement)doc.Descendants().Where(x => x.Name.LocalName == "Invoice").FirstOrDefault();

                        XNamespace cacNs = invoice.GetNamespaceOfPrefix("cac");
                        XNamespace cbcNs = invoice.GetNamespaceOfPrefix("cbc");

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        XElement InvoiceNo = new XElement(cbcNs + "InvoiceNo");
                        InvoiceNo.Value = InvoicesVM.InvoiceNumber.ToString();
                        invoice.Add(InvoiceNo);
                        XElement InvoiceDate = new XElement(cbcNs + "InvoiceDate");
                        InvoiceDate.Value = InvoicesVM.Date.ToString();
                        invoice.Add(InvoiceDate);

                        XElement InvoiceType = new XElement(cbcNs + "InvoiceType");
                        InvoiceType.Value = "Semplified";
                        invoice.Add(InvoiceType);
                        XElement QRCodeString_VD = new XElement(cbcNs + "QRCodeString_VD");
                        QRCodeString_VD.Value = qrcode.ToString();
                        invoice.Add(QRCodeString_VD);





                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        XElement seller = new XElement(cacNs + "seller");
                        invoice.Add(seller);

                        XElement sellerNametxt = new XElement(cbcNs + "sellerName");
                        sellerNametxt.Value = objOrganization.NameAr.ToString();
                        seller.Add(sellerNametxt);
                        XElement Addresstxt = new XElement(cbcNs + "Address");
                        Addresstxt.Value = Address1;
                        seller.Add(Addresstxt);


                        XElement VATNumbertxt = new XElement(cbcNs + "VATNumber");
                        VATNumbertxt.Value = TaxCode1;
                        seller.Add(VATNumbertxt);









                        ////////////////////////////////////////////////////////////////////////       Invoice Details                /////////////////////  
                        /////
                        ///




                        foreach (var item in VoucherDetailsVM)
                        {

                            Counter = Counter + 1;
                            Diff = 0;
                            totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
                            totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
                            Diff = Convert.ToDecimal(item.Amount * item.Qty);


                            XElement InvoiceDetails = new XElement(cacNs + "InvoiceDetails");
                            invoice.Add(InvoiceDetails);


                            XElement Notxt = new XElement(cbcNs + "No");
                            Notxt.Value = Counter.ToString();
                            InvoiceDetails.Add(Notxt);
                            XElement ItemNametxt = new XElement(cbcNs + "ItemName");
                            ItemNametxt.Value = item.ServicesPriceName;
                            InvoiceDetails.Add(ItemNametxt);

                            XElement Unittxt = new XElement(cbcNs + "Unit");
                            Unittxt.Value = "خدمة";
                            InvoiceDetails.Add(Unittxt);

                            XElement Qtytxt = new XElement(cbcNs + "Qty");
                            Qtytxt.Value = item.Qty.ToString();
                            InvoiceDetails.Add(Qtytxt);
                            XElement Pricetxt = new XElement(cbcNs + "Price");
                            Pricetxt.Value = item.Amount.ToString();
                            InvoiceDetails.Add(Pricetxt);
                            XElement Nettxt = new XElement(cbcNs + "Net");
                            Nettxt.Value = Diff.ToString();
                            InvoiceDetails.Add(Nettxt);
                            XElement VATtxt = new XElement(cbcNs + "VAT");
                            VATtxt.Value = totalDetailsTax.ToString();
                            InvoiceDetails.Add(VATtxt);
                            XElement NetVATtxt = new XElement(cbcNs + "NetVAT");
                            NetVATtxt.Value = item.TotalAmount.ToString();
                            InvoiceDetails.Add(NetVATtxt);

                        }


                        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /////
                        ///


                        XElement InvoiceDetails2 = new XElement(cacNs + "InvoiceDetails2");
                        invoice.Add(InvoiceDetails2);


                        XElement Totaltxt = new XElement(cbcNs + "Total");
                        Totaltxt.Value = InvoicesVM.InvoiceValue.ToString();
                        InvoiceDetails2.Add(Totaltxt);
                        XElement Discountpercentagetxt = new XElement(cbcNs + "Discountpercentage");
                        Discountpercentagetxt.Value = String.Format("{0:0.00}", discountPercentage);
                        InvoiceDetails2.Add(Discountpercentagetxt);

                        XElement Discountvaltxt = new XElement(cbcNs + "Discounttxt");
                        Discountvaltxt.Value = String.Format("{0:0.00}", totalDiscount);
                        InvoiceDetails2.Add(Discountvaltxt);

                        XElement Nettxt2 = new XElement(cbcNs + "Net");
                        Nettxt2.Value = String.Format("{0:0.00}", netVal);
                        InvoiceDetails2.Add(Nettxt2);
                        XElement VATtxt2 = new XElement(cbcNs + "VAT");
                        VATtxt2.Value = InvoicesVM.TaxAmount.ToString();
                        InvoiceDetails2.Add(VATtxt2);
                        XElement Amount = new XElement(cbcNs + "Amount");
                        Amount.Value = _totalVal.ToString();
                        InvoiceDetails2.Add(Amount);
                        XElement Paid = new XElement(cbcNs + "Paid");
                        Paid.Value = _paidValud.ToString();
                        InvoiceDetails2.Add(Paid);
                        XElement Remaining = new XElement(cbcNs + "Remaining");
                        Remaining.Value = remainder.ToString();
                        InvoiceDetails2.Add(Remaining);

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        ///



                        FileName = "Invoice_" + DateTime.Now.Ticks.ToString() + ".xml";
                        xmlFile2 = Path.Combine("~/Invoice/" + FileName + "");
                        doc.Save(xmlFile2);





                    }
                    //var TempVal = Server.MapPath("~/Invoice/" + FileName);
                    //var Signed_TempVal = Server.MapPath("~/Invoice/" + "Signed_" + FileName);
                    //SignedXML(TempVal, Signed_TempVal);
                }
                return Ok("");
            }
            catch (Exception ex)
            {
                return Ok("");
            }
        }


        [HttpGet("createxmlInvoicecreditdevit")]
        public IActionResult createxmlInvoicecreditdevit(int? InvoiceId, int? TempCheck)
        {
            try
            {
                string FileName = "";
                string xmlFile2 = "";
                if (InvoiceId.HasValue)
                {




                    DataTable voucher = new DataTable();
                    if (TempCheck == 29)
                        voucher = _voucherService.ReceiptCashingPayingNoti(InvoiceId ?? 0, Con).Result;
                    else if (TempCheck == 30)
                        voucher = _voucherService.ReceiptCashingPayingNotiDepit(InvoiceId ?? 0, Con).Result;
                    else
                        voucher = _voucherService.ReceiptCashingPayingNoti(InvoiceId ?? 0, Con).Result;

                    var InvId2 = Convert.ToInt32(voucher.Rows[0]["InvoiceId"].ToString());
                    List<VoucherDetailsVM> VoucherDetailsVM = _voucherService.GetAllDetailsByInvoiceId(InvId2).Result.ToList();


                    InvoicesVM InvoicesVM = _voucherService.GetVoucherById(InvId2).Result;
                    CustomerVM CustomerVM = _customerService.GetCustomersByCustomerIdInvoice(InvoicesVM.CustomerId ?? 0, _globalshared.Lang_G).Result;

                    var costCenterNam = "";
                    if (InvoicesVM.VoucherDetails[0].CostCenterId != null || InvoicesVM.VoucherDetails[0].CostCenterId != 0)
                    {
                        try
                        {
                            costCenterNam = _CostCenterservice.GetCostCenterById(InvoicesVM.VoucherDetails[0].CostCenterId ?? 0).Result.NameAr;
                        }
                        catch (Exception)
                        {
                            costCenterNam = "بدون";
                        }
                    }
                    else
                    {
                        costCenterNam = "بدون";
                    }
                    string ValueNumString = ConvertToWord_NEW(InvoicesVM.TotalValue.ToString());


                    int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                    var objOrganization = _organizationsservice.GetBranchOrganizationDataInvoice(orgId).Result;
                    var objBranch = _BranchesService.GetBranchByBranchId(_globalshared.Lang_G, _globalshared.BranchId_G).Result.FirstOrDefault();
                    var OrgIsRequired = _systemSettingsService.GetSystemSettingsByBranchId(_globalshared.BranchId_G).Result.OrgDataIsRequired;
                    if (OrgIsRequired == true) OrgIsRequired = false; else OrgIsRequired = true;


                    DateTime Date = InvoicesVM.AddDate ?? DateTime.Now;
                    string ActionDate = Date.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                    var SupplierName = objOrganization.NameAr.TrimStart();
                    SupplierName = SupplierName.TrimEnd();

                    string qrcode = "";
                    try
                    {
                        string ImgReturn = "";

                        TLVCls tlv = new TLVCls(SupplierName, objOrganization.TaxCode, Date, (double)InvoicesVM.TotalValue, (double)InvoicesVM.TaxAmount);
                        using (Bitmap bitMap = tlv.toQrCode())
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                byte[] byteImage = ms.ToArray();
                                System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                                ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                            }
                        }
                        qrcode = ImgReturn;
                    }
                    catch (Exception ex)
                    {



                    }



                    if (InvoicesVM != null && CustomerVM != null && costCenterNam != null && VoucherDetailsVM != null && objOrganization != null && objBranch != null && qrcode != null)
                    {

                        string BankAccount1 = "لا يوجد";
                        string BankAccount2 = "لا يوجد";

                        if (InvoicesVM.printBankAccount == true)
                        {
                            BankAccount1 = "لا يوجد";
                            BankAccount2 = "لا يوجد";
                        }
                        else
                        {
                            BankAccount1 = OrgIsRequired == true ? objOrganization.AccountBank : objBranch.AccountBank;
                            BankAccount2 = OrgIsRequired == true ? objOrganization.AccountBank2 : objBranch.AccountBank2;
                        }


                        decimal totalDetailsPrice = 0;
                        decimal totalDetailsTax = 0;
                        decimal totalDiscount = 0;
                        decimal percentage = 0;
                        decimal Diff = 0;
                        int Counter = 0;

                        percentage = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");
                        totalDiscount = decimal.Parse(InvoicesVM.DiscountPercentage != null ? InvoicesVM.DiscountPercentage.ToString() : "0");

                        decimal tax = ((totalDetailsPrice - totalDiscount) * 15) / 100;
                        decimal totalValueOfInvoice = (totalDetailsPrice + tax) - (totalDiscount);
                        decimal _paidValud = (InvoicesVM.PaidValue.HasValue ? InvoicesVM.PaidValue.Value : 0);
                        decimal _totalVal = InvoicesVM.TotalValue.HasValue ? InvoicesVM.TotalValue.Value : 0;
                        string remainder = (_totalVal - _paidValud).ToString();
                        decimal discountPercentage = InvoicesVM.DiscountPercentage.HasValue ? InvoicesVM.DiscountPercentage.Value : default(decimal);
                        decimal discountValue = InvoicesVM.DiscountValue.HasValue ? InvoicesVM.DiscountValue.Value : default(decimal);
                        if (discountPercentage == default(decimal) && discountValue != default(decimal))
                        {
                            discountPercentage = discountValue * 100 / InvoicesVM.InvoiceValue.Value;
                        }
                        if (discountPercentage != default(decimal) && discountValue == default(decimal))
                        {
                            discountValue = (InvoicesVM.InvoiceValue.Value * discountPercentage) / 100;
                            //_totalVal = _totalVal - discountValue;
                        }
                        decimal netVal = 0;
                        if (VoucherDetailsVM.FirstOrDefault().TaxType == 3)
                        {
                            netVal = InvoicesVM.InvoiceValue.Value - InvoicesVM.TaxAmount.Value;
                        }
                        else
                        {
                            netVal = InvoicesVM.InvoiceValue.Value;
                        }




                        XmlDocument xdoc = new XmlDocument();
                        var xmlstappath = Path.Combine(@"~\Helpers\XMLFile1.xml");

                        xdoc.Load(xmlstappath);

                        string Address1 = "";
                        string BuildingNumber1 = "";
                        string StreetName1 = "";
                        string Neighborhood1 = "";
                        string CityName1 = "";
                        string Country1 = "";
                        string PostalCode1 = "";
                        string ExternalPhone1 = "";
                        string TaxCode1 = "";
                        if (OrgIsRequired == true)
                        {
                            Address1 = objOrganization.Address;
                            BuildingNumber1 = objOrganization.BuildingNumber;
                            StreetName1 = objOrganization.StreetName;
                            Neighborhood1 = objOrganization.Neighborhood;
                            CityName1 = objOrganization.CityName;
                            Country1 = objOrganization.Country;
                            PostalCode1 = objOrganization.PostalCode;
                            ExternalPhone1 = objOrganization.ExternalPhone;
                            TaxCode1 = objOrganization.TaxCode;

                        }
                        else
                        {
                            Address1 = objBranch.Address;
                            BuildingNumber1 = objBranch.BuildingNumber;
                            StreetName1 = objBranch.StreetName;
                            Neighborhood1 = objBranch.Neighborhood;
                            CityName1 = objBranch.CityName;
                            Country1 = objBranch.Country;
                            PostalCode1 = objBranch.PostalCodeFinal;
                            ExternalPhone1 = objBranch.ExternalPhone;
                            TaxCode1 = objBranch.TaxCode;

                        }

                        /////////////////////////////////////////////////////////////////////////////seller///////////////////////////
                        ///

                        var xmlstappath2 = Path.Combine(@"~\Helpers\XMLFile1.xml");


                        XDocument doc = XDocument.Load(xmlstappath2);
                        XElement invoice = (XElement)doc.Descendants().Where(x => x.Name.LocalName == "Invoice").FirstOrDefault();

                        XNamespace cacNs = invoice.GetNamespaceOfPrefix("cac");
                        XNamespace cbcNs = invoice.GetNamespaceOfPrefix("cbc");

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        XElement InvoiceNo = new XElement(cbcNs + "InvoiceNo");
                        InvoiceNo.Value = InvoicesVM.InvoiceNumber.ToString();
                        invoice.Add(InvoiceNo);
                        XElement InvoiceDate = new XElement(cbcNs + "InvoiceDate");
                        InvoiceDate.Value = InvoicesVM.Date.ToString();
                        invoice.Add(InvoiceDate);
                        XElement Dateofsupply = new XElement(cbcNs + "Dateofsupply");
                        Dateofsupply.Value = InvoicesVM.Date.ToString();
                        invoice.Add(Dateofsupply);
                        XElement TaxInvoice = new XElement(cbcNs + "TaxInvoice");
                        TaxInvoice.Value = InvoicesVM.PayTypeName.ToString();
                        invoice.Add(TaxInvoice);
                        XElement OrganizationName = new XElement(cbcNs + "OrganizationName");
                        OrganizationName.Value = objOrganization.NameAr.ToString();
                        invoice.Add(OrganizationName);
                        XElement BranchName = new XElement(cbcNs + "BranchName");
                        BranchName.Value = objBranch.NameAr.ToString();
                        invoice.Add(BranchName);
                        if (TempCheck == 29)
                        {
                            XElement InvoiceType = new XElement(cbcNs + "InvoiceType");
                            InvoiceType.Value = "Credit Note";
                            invoice.Add(InvoiceType);
                        }
                        else if (TempCheck == 30)
                        {
                            XElement InvoiceType = new XElement(cbcNs + "InvoiceType");
                            InvoiceType.Value = "Depit Note";
                            invoice.Add(InvoiceType);
                        }
                        XElement QRCodeString_VD = new XElement(cbcNs + "QRCodeString_VD");
                        QRCodeString_VD.Value = qrcode.ToString();
                        invoice.Add(QRCodeString_VD);





                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        XElement seller = new XElement(cacNs + "seller");
                        invoice.Add(seller);

                        XElement sellerNametxt = new XElement(cbcNs + "sellerName");
                        sellerNametxt.Value = objOrganization.NameAr.ToString();
                        seller.Add(sellerNametxt);
                        XElement Addresstxt = new XElement(cbcNs + "Address");
                        Addresstxt.Value = Address1;
                        seller.Add(Addresstxt);


                        XElement BuildingNotxt = new XElement(cbcNs + "BuildingNo");
                        BuildingNotxt.Value = BuildingNumber1;
                        seller.Add(BuildingNotxt);

                        XElement StreetNametxt = new XElement(cbcNs + "StreetName");
                        StreetNametxt.Value = StreetName1;
                        seller.Add(StreetNametxt);
                        XElement AreaNametxt = new XElement(cbcNs + "AreaName");
                        AreaNametxt.Value = Neighborhood1;
                        seller.Add(AreaNametxt);
                        XElement Citytxt = new XElement(cbcNs + "City");
                        Citytxt.Value = CityName1;
                        seller.Add(Citytxt);
                        XElement Countrytxt = new XElement(cbcNs + "Country");
                        Countrytxt.Value = Country1;
                        seller.Add(Countrytxt);
                        XElement PostalCodetxt = new XElement(cbcNs + "PostalCode");
                        PostalCodetxt.Value = PostalCode1;
                        seller.Add(PostalCodetxt);
                        XElement AdditionalNotxt = new XElement(cbcNs + "AdditionalNo");
                        AdditionalNotxt.Value = ExternalPhone1;
                        seller.Add(AdditionalNotxt);
                        XElement VATNumbertxt = new XElement(cbcNs + "VATNumber");
                        VATNumbertxt.Value = TaxCode1;
                        seller.Add(VATNumbertxt);
                        XElement AthoerSellerIDtxt = new XElement(cbcNs + "AthoerSellerID");
                        AthoerSellerIDtxt.Value = "";
                        seller.Add(AthoerSellerIDtxt);





                        //////////////////////////////////////////////////////////////////////       customer                //////////////////////////////////////////////////////////////////////////  
                        ///
                        XElement Customer = new XElement(cacNs + "Customer");
                        invoice.Add(Customer);


                        XElement CNametxt = new XElement(cbcNs + "CustomerName");
                        CNametxt.Value = CustomerVM.CustomerNameAr.ToString();
                        Customer.Add(CNametxt);
                        XElement CAddresstxt = new XElement(cbcNs + "CustomerAddress");
                        CAddresstxt.Value = CustomerVM.CustomerAddress.ToString();
                        Customer.Add(CAddresstxt);

                        XElement CBuildingNo = new XElement(cbcNs + "CustomerBuildingNo");
                        CBuildingNo.Value = CustomerVM.BuildingNumber.ToString();
                        Customer.Add(CBuildingNo);

                        XElement CStreetNametxt = new XElement(cbcNs + "CustomerStreetName");
                        CStreetNametxt.Value = CustomerVM.StreetName.ToString();
                        Customer.Add(CStreetNametxt);
                        XElement CAreaNametxt = new XElement(cbcNs + "CustomerAreaName");
                        CAreaNametxt.Value = CustomerVM.Neighborhood.ToString();
                        Customer.Add(CAreaNametxt);
                        XElement CCitytxt = new XElement(cbcNs + "CustomerCity");
                        CCitytxt.Value = CustomerVM.CityName.ToString();
                        Customer.Add(CCitytxt);
                        XElement CCountry = new XElement(cbcNs + "CustomerCountry");
                        CCountry.Value = CustomerVM.Country.ToString();
                        Customer.Add(CCountry);
                        XElement CPostalCodetxt = new XElement(cbcNs + "CustomerPostalCode");
                        CPostalCodetxt.Value = CustomerVM.PostalCodeFinal.ToString();
                        Customer.Add(CPostalCodetxt);
                        XElement CAdditionalNotxt = new XElement(cbcNs + "CustomerAdditionalNo");
                        CAdditionalNotxt.Value = CustomerVM.ExternalPhone.ToString();
                        Customer.Add(CAdditionalNotxt);
                        XElement CVATNumbertxt = new XElement(cbcNs + "CustomerVATNumber");
                        CVATNumbertxt.Value = "";
                        Customer.Add(CVATNumbertxt);
                        XElement CAthoerSellerID = new XElement(cbcNs + "CustomerAthoerSellerID");
                        CAthoerSellerID.Value = "";
                        Customer.Add(CAthoerSellerID);






                        ////////////////////////////////////////////////////////////////////////       Invoice Details                /////////////////////  
                        /////
                        ///




                        foreach (var item in VoucherDetailsVM)
                        {

                            Counter = Counter + 1;
                            Diff = 0;
                            totalDetailsPrice = totalDetailsPrice + (decimal.Parse(item != null ? item.Amount.ToString() : "0"));
                            totalDetailsTax = Convert.ToDecimal(item.TaxAmount * item.Qty);
                            Diff = Convert.ToDecimal(item.Amount * item.Qty);


                            XElement InvoiceDetails = new XElement(cacNs + "InvoiceDetails");
                            invoice.Add(InvoiceDetails);


                            XElement Notxt = new XElement(cbcNs + "No");
                            Notxt.Value = Counter.ToString();
                            InvoiceDetails.Add(Notxt);
                            XElement ItemNametxt = new XElement(cbcNs + "ItemName");
                            ItemNametxt.Value = item.ServicesPriceName;
                            InvoiceDetails.Add(ItemNametxt);

                            XElement Unittxt = new XElement(cbcNs + "Unit");
                            Unittxt.Value = "خدمة";
                            InvoiceDetails.Add(Unittxt);

                            XElement Qtytxt = new XElement(cbcNs + "Qty");
                            Qtytxt.Value = item.Qty.ToString();
                            InvoiceDetails.Add(Qtytxt);
                            XElement Pricetxt = new XElement(cbcNs + "Price");
                            Pricetxt.Value = item.Amount.ToString();
                            InvoiceDetails.Add(Pricetxt);
                            XElement Nettxt = new XElement(cbcNs + "Net");
                            Nettxt.Value = Diff.ToString();
                            InvoiceDetails.Add(Nettxt);
                            XElement VATtxt = new XElement(cbcNs + "VAT");
                            VATtxt.Value = totalDetailsTax.ToString();
                            InvoiceDetails.Add(VATtxt);
                            XElement NetVATtxt = new XElement(cbcNs + "NetVAT");
                            NetVATtxt.Value = item.TotalAmount.ToString();
                            InvoiceDetails.Add(NetVATtxt);

                        }


                        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /////
                        ///


                        XElement InvoiceDetails2 = new XElement(cacNs + "InvoiceDetails2");
                        invoice.Add(InvoiceDetails2);


                        XElement Totaltxt = new XElement(cbcNs + "Total");
                        Totaltxt.Value = InvoicesVM.InvoiceValue.ToString();
                        InvoiceDetails2.Add(Totaltxt);
                        XElement Discountpercentagetxt = new XElement(cbcNs + "Discountpercentage");
                        Discountpercentagetxt.Value = String.Format("{0:0.00}", discountPercentage);
                        InvoiceDetails2.Add(Discountpercentagetxt);

                        XElement Discountvaltxt = new XElement(cbcNs + "Discounttxt");
                        Discountvaltxt.Value = String.Format("{0:0.00}", totalDiscount);
                        InvoiceDetails2.Add(Discountvaltxt);

                        XElement Nettxt2 = new XElement(cbcNs + "Net");
                        Nettxt2.Value = String.Format("{0:0.00}", netVal);
                        InvoiceDetails2.Add(Nettxt2);
                        XElement VATtxt2 = new XElement(cbcNs + "VAT");
                        VATtxt2.Value = InvoicesVM.TaxAmount.ToString();
                        InvoiceDetails2.Add(VATtxt2);
                        XElement Amount = new XElement(cbcNs + "Amount");
                        Amount.Value = _totalVal.ToString();
                        InvoiceDetails2.Add(Amount);
                        XElement Paid = new XElement(cbcNs + "Paid");
                        Paid.Value = _paidValud.ToString();
                        InvoiceDetails2.Add(Paid);
                        XElement Remaining = new XElement(cbcNs + "Remaining");
                        Remaining.Value = remainder.ToString();
                        InvoiceDetails2.Add(Remaining);

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        ///

                        XElement Banckaccount = new XElement(cacNs + "BankAccounts");
                        invoice.Add(Banckaccount);
                        XElement bank1 = new XElement(cbcNs + "BankAccounts1");
                        bank1.Value = BankAccount1;
                        Banckaccount.Add(bank1);

                        XElement bank2 = new XElement(cbcNs + "BankAccounts2");
                        bank2.Value = BankAccount2;
                        Banckaccount.Add(bank2);

                        //    XmlNodeList BankAccounts1txt = xdoc.GetElementsByTagName("cbc:BankAccounts1");
                        //XmlNodeList BankAccounts2ltxt = xdoc.GetElementsByTagName("cbc:BankAccounts2");


                        //foreach (XmlNode aDateNode in BankAccounts1txt)
                        //{
                        //    aDateNode.InnerText = BankAccount1;
                        //}
                        //foreach (XmlNode aDateNode in BankAccounts2ltxt)
                        //{
                        //    aDateNode.InnerText = BankAccount2;
                        //}


                        FileName = "Invoice_" + DateTime.Now.Ticks.ToString() + ".xml";
                        xmlFile2 = Path.Combine("~/Invoice/" + FileName + "");
                        doc.Save(xmlFile2);







                    }
                }


                return Ok("");
            }
            catch (Exception ex)
            {
                return Ok("");
            }



        }



        //Signing xml document with a installed X.509 certificate
        [HttpGet("SignedXML")]
        public void SignedXML(string OldFileName, string SignedXMLFileName)
        {
            try
            {
                // Generate a signing key.
                RSA Key = RSA.Create();


                // The URI to sign.
                string resourceToSign = "";

                // The name of the file to which to save the XML signature.
                string XmlFileName = "xmldsig" + DateTime.Now.Ticks + ".xml";

                // The name of the X509 certificate
                string Certificate = "microsoft.cer";

                //---------------------------------
                //SignDetachedResource(resourceToSign, XmlFileName, Key, Certificate);

                //---------------------------------------------

                string PfxPath = @"D:\TameerCert2.pfx";
                string CertPath = @"D:\TameerCert.cer";
                X509Certificate2 cert = new X509Certificate2(System.IO.File.ReadAllBytes(PfxPath), "000123");

                X509Certificate2 cert2 = new X509Certificate2(System.IO.File.ReadAllBytes(CertPath));
                SignXmlFile(resourceToSign, OldFileName, SignedXMLFileName, Key, cert);
                Console.WriteLine("XML file signed.");

                // Verify the signature of the signed XML.
                Console.WriteLine("Verifying signature...");
                bool result = VerifyXmlFile(SignedXMLFileName, Key);

                // Display the results of the signature verification to 
                // the console.
                if (result)
                {
                    Console.WriteLine("The XML signature is valid.");
                }
                else
                {
                    Console.WriteLine("The XML signature is not valid.");
                }
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // Sign an XML file and save the signature in a new file. This method does not  
        // save the public key within the XML file.  This file cannot be verified unless  
        // the verifying code has the key with which it was signed.
        [HttpGet("SignXmlFile")]
        public static void SignXmlFile(string URIString, string FileName, string SignedFileName, RSA Key, X509Certificate2 Certificate)
        {

            // Create a new XML document.
            XmlDocument doc = new XmlDocument();

            //test dawoud

            //doc.PreserveWhitespace = true;
            //doc.PreserveWhitespace = false;

            // Load the passed XML file using its name.
            doc.Load(new XmlTextReader(FileName));

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(doc);

            // Add the key to the SignedXml document. 
            signedXml.SigningKey = Certificate.PrivateKey;

            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";


            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = URIString;
            reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);


            //// Create a new KeyInfo object.
            KeyInfo keyInfo = new KeyInfo();

            keyInfo.AddClause(new KeyInfoX509Data(Certificate));

            signedXml.KeyInfo = keyInfo;

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));

            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }

            // Save the signed XML document to a file specified
            // using the passed string.
            XmlTextWriter xmltw = new XmlTextWriter(SignedFileName, new UTF8Encoding(false));
            doc.WriteTo(xmltw);
            xmltw.Close();
        }

        // Verify the signature of an XML file against an asymmetric 
        // algorithm and return the result.
        [HttpGet("VerifyXmlFile")]
        public static Boolean VerifyXmlFile(String Name, RSA Key)
        {
            // Create a new XML document.
            XmlDocument xmlDocument = new XmlDocument();

            // Load the passed XML file into the document. 
            xmlDocument.Load(Name);

            // Create a new SignedXml object and pass it
            // the XML document class.
            SignedXml signedXml = new SignedXml(xmlDocument);

            // Find the "Signature" node and create a new
            // XmlNodeList object.
            XmlNodeList nodeList = xmlDocument.GetElementsByTagName("Signature");

            // Load the signature node.
            signedXml.LoadXml((XmlElement)nodeList[0]);

            // Check the signature and return the result.
            return signedXml.CheckSignature(Key);
        }
        [HttpGet("TestSignXML")]
        public IActionResult TestSignXML(int InvoiceId)
        {
            var FileName = createxmlublstandard(InvoiceId);

            if (FileName != "")
            {
                var TempVal = Path.Combine("~/Invoice/" + FileName);
                var Signed_TempVal = Path.Combine("~/Invoice/" + "Signed_" + FileName);
                SignedXML(TempVal, Signed_TempVal);
            }
            return Ok("OK");
        }
        [HttpGet("FillFilteringSelect")]
        public IActionResult FillFilteringSelect(int Param)
        {
            if (Param == 2)
            {
                var Customers = _customerService.GetAllCustomerForDrop(_globalshared.Lang_G).Result.Select(s => new {
                    Id = s.Id,
                    Name = s.Name
                });
                return Ok(Customers);
            }
            else if (Param == 3)
            {
                var Suppliers = _acc_SuppliersService.GetAllSuppliers("").Result.Select(s => new {
                    Id = s.SupplierId,
                    Name = s.NameAr
                });
                return Ok(Suppliers);
            }
            else if (Param == 1)
            {
                var someProject = _projectservice.GetAllProjectAllBranches().Result.Select(s => new {
                    Id = s.ProjectId,
                    Name = s.ProjectNo
                });
                return Ok(someProject);
            }
            else if (Param == 5)
            {
                var Employees = _employesService.GetAllBranchEmployees(_globalshared.Lang_G).Result.Select(s => new {
                    Id = s.EmployeeId,
                    Name = s.EmployeeName
                });
                return Ok(Employees);
            }
            else if (Param == 4)
            {

                var Branches = _BranchesService.GetAllBranches(_globalshared.Lang_G).Result.Select(s => new {
                    Id = s.BranchId,
                    Name = s.BranchName
                });
                return Ok(Branches);

            }
            else if (Param == 6)
            {
                var ServicePrices = _servicesPriceService.GetAllServicesPrice().Result.Select(s => new {
                    Id = s.ServicesId,
                    Name = s.ServicesName
                });
                return Ok(ServicePrices);
            }
            else
            {
                List<Branch> branch = new List<Branch>();
                return Ok(branch);
            }

        }
        [HttpGet("FillFilteringIncomeStateSelect")]
        public IActionResult FillFilteringIncomeStateSelect(int Param)
        {
            if (Param > 0 && Param <= 10)
            {
                var PeriodCounterList = GetPeriodCounterList();
                return Ok(PeriodCounterList);
            }
            else if (Param == 12)
            {
                var Customers = _customerService.GetAllCustomerForDrop(_globalshared.Lang_G).Result.Select(s => new {
                    Id = s.Id,
                    Name = s.Name
                });
                return Ok(Customers);
            }
            else if (Param == 13)
            {
                var Suppliers = _acc_SuppliersService.GetAllSuppliers("").Result.Select(s => new {
                    Id = s.SupplierId,
                    Name = s.NameAr
                });
                return Ok(Suppliers);
            }
            else if (Param == 11)
            {
                var someProject = _projectservice.GetAllProjectAllBranches().Result.Select(s => new {
                    Id = s.ProjectId,
                    Name = s.ProjectNo
                });
                return Ok(someProject);
            }
            else if (Param == 15)
            {
                var Employees = _employesService.GetAllBranchEmployees(_globalshared.Lang_G).Result.Select(s => new {
                    Id = s.EmployeeId,
                    Name = s.EmployeeName
                });
                return Ok(Employees);
            }
            else if (Param == 14)
            {

                var Branches = _BranchesService.GetAllBranches(_globalshared.Lang_G).Result.Select(s => new {
                    Id = s.BranchId,
                    Name = s.BranchName
                });
                return Ok(Branches);

            }
            else if (Param == 16)
            {
                var ServicePrices = _servicesPriceService.GetAllServicesPrice().Result.Select(s => new {
                    Id = s.ServicesId,
                    Name = s.ServicesName
                });
                return Ok(ServicePrices);
            }
            else
            {
                List<PeriodCounter> PeriodCounterv = new List<PeriodCounter>();
                return Ok(PeriodCounterv);
            }

        }
        [HttpGet("GetPeriodCounterList")]
        public List<PeriodCounter> GetPeriodCounterList()
        {
            try
            {
                List<PeriodCounter> PeriodCounterList = new List<PeriodCounter>
                {
                    new PeriodCounter { Id = 0, Name = "--أختر--"},
                    new PeriodCounter { Id = 1, Name = "فترة"},
                    new PeriodCounter { Id = 2, Name = "فترتين"},
                    new PeriodCounter { Id = 3, Name = "3 فترات"},
                    new PeriodCounter { Id = 4, Name = "4 فترات"},
                    new PeriodCounter { Id = 5, Name = "5 فترات"},
                    new PeriodCounter { Id = 6, Name = "6 فترات"},
                    new PeriodCounter { Id = 7, Name = "7 فترات"},
                    new PeriodCounter { Id = 8, Name = "8 فترات"},
                    new PeriodCounter { Id = 9, Name = "9 فترات"},
                    new PeriodCounter { Id = 10, Name = "10 فترات"},
                    new PeriodCounter { Id = 11, Name = "11 فترة"},
                    new PeriodCounter { Id = 12, Name = "12 فترة"},
                    new PeriodCounter { Id = 13, Name = "13 فترة"}
                };
                return PeriodCounterList;
            }
            catch (Exception)
            {
                List<PeriodCounter> PeriodCounterv = new List<PeriodCounter>();
                return PeriodCounterv;
            }

        }


        [HttpPost("GetFinancialfollowup")]
        public IActionResult GetFinancialfollowup([FromForm] FinancialfollowupVM _financialfollowupVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            _financialfollowupVM.BranchId = _globalshared.BranchId_G;
            _financialfollowupVM.YearId = _globalshared.YearId_G;

            var AllInvoices = _voucherService.GetFinancialfollowup(Con ?? "", _financialfollowupVM).Result.ToList();
            return Ok(AllInvoices);
        }





        [HttpPost("GetFinancialfollowup_pageing")]
        public IActionResult GetFinancialfollowup_pageing([FromForm] FinancialfollowupVM _financialfollowupVM)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            _financialfollowupVM.BranchId = _globalshared.BranchId_G;
            _financialfollowupVM.YearId = _globalshared.YearId_G;
            if (_financialfollowupVM.SearchText == null || _financialfollowupVM.SearchText == "")
            {
                var AllInvoices = _voucherService.GetFinancialfollowup(Con ?? "", _financialfollowupVM).Result.ToList();
                var data = GeneratePagination<InvoicesVM>.ToPagedList(AllInvoices.ToList(), _financialfollowupVM.Page.Value, _financialfollowupVM.PageSize.Value);
                var result = new PagedLists<InvoicesVM>(data.MetaData, data);
                return Ok(result);
            }
            else
            {
                var AllInvoices = _voucherService.GetFinancialfollowup(Con ?? "", _financialfollowupVM).Result.ToList().Where(x => x.InvoiceNumber.Contains(_financialfollowupVM.SearchText) ||
           x.InvoiceValue.ToString().Contains(_financialfollowupVM.SearchText) || x.PayTypeName.Contains(_financialfollowupVM.SearchText) ||
            x.CustomerName.Contains(_financialfollowupVM.SearchText) || x.ProjectNo.Contains(_financialfollowupVM.SearchText) || _financialfollowupVM.SearchText == null || _financialfollowupVM.SearchText == "");
                var data = GeneratePagination<InvoicesVM>.ToPagedList(AllInvoices.ToList(), _financialfollowupVM.Page.Value, _financialfollowupVM.PageSize.Value);
                var result = new PagedLists<InvoicesVM>(data.MetaData, data);
                return Ok(result);
            }
        }

    }






    public class PeriodCounter
    {
        public int Id { get; set; }
        public string Name { get; set; }

    }
}
