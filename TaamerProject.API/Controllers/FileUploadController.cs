using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Service.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

//using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Security.Cryptography;
using System.Text;
using TaamerProject.Models.Common;
using TaamerProject.Service.Services;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;


namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class FileUploadController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IProjectExtractsService _projectExtractsService;
        private readonly IContacFilesService _contacFilesService;
        private IBranchesService _BranchesService;
        private IOrganizationsService _organizationsservice;
        private IContractService _contractService;
        private IOutInBoxService _OutInBoxservice;
        private IProjectService _projectservice;
        FilesHelper filesHelper;
        String tempPath = "~/ProjectFiles/";
        String serverMapPath = "/Files/ProjectFiles/";
        private string StorageRoot
        {
            get { return Path.Combine(Path.Combine(serverMapPath)); }
        }
        private string UrlBase = "/Files/ProjectFiles/";
        String DeleteURL = "/FileUpload/DeleteFile/?file=";
        String DeleteType = "GET";

        private IConfiguration Configuration;
        public GlobalShared _globalshared;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private string Con;
        public FileUploadController(IFileService fileService, IProjectExtractsService projectExtractsService, IContacFilesService contacFilesService,
           IBranchesService branchesService, IOrganizationsService organizationsService, IContractService contractService, IOutInBoxService outInBoxService,
          IProjectService projectService, IConfiguration _configuration, IWebHostEnvironment webHostEnvironment)
        {
            _projectservice = projectService;
            _fileService = fileService;
            _projectExtractsService = projectExtractsService;
            this._BranchesService = branchesService;
            this._organizationsservice = organizationsService;
            _contractService = contractService;

            _contacFilesService = contacFilesService;
            filesHelper = new FilesHelper(DeleteURL, DeleteType, StorageRoot, UrlBase, tempPath, serverMapPath);
            this._OutInBoxservice = outInBoxService;


            Configuration = _configuration; Con = this.Configuration.GetConnectionString("DBConnection");
            HttpContext httpContext = HttpContext;

            _globalshared = new GlobalShared(httpContext);
            _hostingEnvironment = webHostEnvironment;
        }


        [HttpPost("UploadProjectFiles")]
        public IActionResult UploadProjectFiles([FromForm] List<IFormFile>? uploadesgiles, [FromForm] string? FileId
            , [FromForm] string? FileName, [FromForm] string? TypeId, [FromForm] string? ProjectId, [FromForm] string? Notes)
        {


            ProjectFiles file = new ProjectFiles();
            file.FileId = Convert.ToInt32(FileId);
            file.FileName = FileName == "null" ? "" : FileName;
            file.TypeId = Convert.ToInt32(TypeId);
            file.ProjectId = Convert.ToInt32(ProjectId); ;
            file.Notes = Notes=="null"?"":Notes;

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var FileResult2 = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "فشل في الحفظ تأكد من اختيار ملف", ReturnedStr = "" };

            try
            {
                var FileResult = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يوجد ملفات", ReturnedStr = "" };

                int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                string TaxCode = _organizationsservice.GetBranchOrganizationData(orgId).Result.TaxCode;
                string OrgaEnglishName = _organizationsservice.GetBranchOrganizationData(orgId).Result.NameAr ?? "";
                if (TaxCode == "")
                {
                    return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تأكد من الرقم الضريبي للمنشأة وذلك لاستخدامة في عملية الباركود" });

                }
                var project = _projectservice.GetProjectById(_globalshared.Lang_G, file.ProjectId ?? 0).Result;
                var CustomerName = _projectservice.GetProjectById(_globalshared.Lang_G, file.ProjectId ?? 0).Result.CustomerName;

                string serverMapPath2 = "";
                if (project != null)
                {
                    serverMapPath2 = Path.Combine("Files/ProjectFiles/" + project.ProjectNo);
                    if (!Directory.Exists(serverMapPath2))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(serverMapPath2);
                    }
                }
                UrlBase = UrlBase + project.ProjectNo; /// added recently
                FilesHelper filesHelpers = new FilesHelper(DeleteURL, DeleteType, serverMapPath2, UrlBase, tempPath, serverMapPath2);
                var resultList = new List<ViewDataUploadFilesResult>();
                var CurrentContext = HttpContext;
                //filesHelpers.UploadAndShowResults(CurrentContext, resultList);
                JsonFiles files = new JsonFiles(resultList);
                bool isEmpty = !resultList.Any();
                if (isEmpty)
                {
                    return Ok(FileResult2);
                }
                else
                {
                    for (int i = 0; i < uploadesgiles.Count; i++)
                    {

                        //string BarcodeHash = EncryptValue(TaxCode);
                        string BarcodeHash = "200010001000";


                        ProjectFiles filesN = new ProjectFiles();


                        string filename = files.files[i].name;
                        filesN.FileId = 0;
                        filesN.ProjectId = file.ProjectId;
                        filesN.FileName = file.FileName ?? resultList[i].name;
                        filesN.IsCertified = file.IsCertified;
                        filesN.Notes = file.Notes;
                        filesN.FileSize = resultList[i].size;
                        filesN.TypeId = file.TypeId;
                        string valrand = RandomNumber(1, 10000).ToString();
                        if (resultList[i].type == "application/pdf")
                        {
                            filesN.FileUrl = UrlBase + "/" + "_B_" + valrand + resultList[i].name;
                            filesN.FileUrlW = resultList[i].url;
                        }
                        else
                        {

                            filesN.FileUrl = resultList[i].url;
                            filesN.FileUrlW = resultList[i].url;

                        }
                        filesN.BarcodeFileNum = BarcodeHash;

                        filesN.Extension = resultList[i].type;
                        filesN.DeleteUrl = resultList[i].deleteUrl;
                        filesN.DeleteType = resultList[i].deleteType;
                        filesN.ThumbnailUrl = resultList[i].thumbnailUrl;
                        filesN.Type = resultList[i].type;
                        filesN.TaskId = file.TaskId;
                        filesN.CompanyTaxNo = TaxCode;

                        filesN.NotificationId = file.NotificationId;

                        //string ProjectNo = _projectservice.GetProjectById(file.ProjectId ??0).ProjectNo;


                        FileResult = _fileService.SaveFile_Bar(filesN, _globalshared.UserId_G, _globalshared.BranchId_G);
                        if (resultList[i].type == "application/pdf")
                        {
                            BarcodeFun_A3(serverMapPath2, filename, FileResult.ReturnedStr, valrand, project.ProjectNo, OrgaEnglishName, CustomerName);

                        }
                    }
                    return Ok(FileResult);

                }
            }
            catch (Exception ex)
            {
                return Ok(FileResult2);
            }

        }




        [HttpPost("UploadProjectFilesNew")]
        public IActionResult UploadProjectFilesNew([FromForm] List<IFormFile>? uploadesgiles, [FromForm] string? FileId, [FromForm] string? PageInsert
    , [FromForm] string? FileName, [FromForm] string? TypeId, [FromForm] string? ProjectId, [FromForm] string? Notes, [FromForm] bool? IsCertified)
        {


            ProjectFiles file = new ProjectFiles();
            file.FileId = Convert.ToInt32(FileId);
            file.FileName = FileName == "null" ? "" : FileName;
            file.TypeId = Convert.ToInt32(TypeId);
            file.ProjectId = Convert.ToInt32(ProjectId);
            file.Notes = Notes == "null" ? "" : Notes;
            file.IsCertified = IsCertified ?? false;
            if (PageInsert == "" || PageInsert == null)
            {
                file.PageInsert = 1;
            }
            else
            {
                file.PageInsert = Convert.ToInt32(PageInsert);
            }

            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var FileResult2 = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "فشل في الحفظ تأكد من اختيار ملف", ReturnedStr = "" };

            try
            {
                var FileResult = new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يوجد ملفات", ReturnedStr = "" };

                int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
                string TaxCode = _organizationsservice.GetBranchOrganizationData(orgId).Result.TaxCode;
                string OrgaEnglishName = _organizationsservice.GetBranchOrganizationData(orgId).Result.NameAr ?? "";
                if (TaxCode == "")
                {
                    return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تأكد من الرقم الضريبي للمنشأة وذلك لاستخدامة في عملية الباركود" });

                }
                var project = _projectservice.GetProjectById(_globalshared.Lang_G, file.ProjectId ?? 0).Result;
                var CustomerName = _projectservice.GetProjectById(_globalshared.Lang_G, file.ProjectId ?? 0).Result.CustomerName;

                string pathF = System.IO.Path.Combine("Files/", "ProjectFiles/");
                string serverMapPath2 = "";
                if (project != null)
                {
                    serverMapPath2 = Path.Combine("Files/ProjectFiles/" + project.ProjectNo);
                    pathF = System.IO.Path.Combine("Files/", "ProjectFiles/" + project.ProjectNo);
                    if (!Directory.Exists(pathF))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(pathF);
                    }
                }
                UrlBase = UrlBase + project.ProjectNo; /// added recently
                FilesHelper filesHelpers = new FilesHelper(DeleteURL, DeleteType, serverMapPath2, UrlBase, tempPath, serverMapPath2);
                for (int i = 0; i < uploadesgiles.Count; i++)
                {
                    string BarcodeHash = "200010001000";
                    ProjectFiles filesN = new ProjectFiles();
                    string filename = uploadesgiles[i].FileName;
                    filesN.FileId = 0;
                    filesN.ProjectId = file.ProjectId;
                    filesN.FileName = file.FileName ?? uploadesgiles[i].FileName;
                    filesN.IsCertified = file.IsCertified;
                    filesN.Notes = file.Notes;
                    filesN.FileSize = uploadesgiles[i].Length;
                    filesN.TypeId = file.TypeId;
                    string valrand = RandomNumber(1, 10000).ToString();
                    var Extension = System.IO.Path.GetExtension(uploadesgiles[i].FileName);
                    if (Extension == "application/pdf" || Extension == ".pdf")
                    {
                        filesN.FileUrl = UrlBase + "/" + "_B_" + valrand + uploadesgiles[i].FileName;
                        filesN.FileUrlW = UrlBase + "/" + uploadesgiles[i].FileName;
                    }
                    else
                    {

                        filesN.FileUrl = UrlBase + "/" + uploadesgiles[i].FileName;
                        filesN.FileUrlW = UrlBase + "/" + uploadesgiles[i].FileName;

                    }
                    filesN.BarcodeFileNum = BarcodeHash;

                    filesN.Extension = Extension;
                    filesN.DeleteUrl = null;
                    filesN.DeleteType = null;
                    filesN.ThumbnailUrl = null;
                    filesN.Type = uploadesgiles[i].ContentType;
                    filesN.TaskId = file.TaskId;
                    filesN.CompanyTaxNo = TaxCode;

                    filesN.NotificationId = file.NotificationId;
                    filesN.PageInsert = file.PageInsert;

                    //string ProjectNo = _projectservice.GetProjectById(file.ProjectId ??0).ProjectNo;

                    List<string> uploadedFiles = new List<string>();
                    var path2 = Path.Combine(pathF, uploadesgiles[i].FileName);
                    if (System.IO.File.Exists(path2))
                    {
                        System.IO.File.Delete(path2);
                    }
                    using (System.IO.FileStream stream = new System.IO.FileStream(System.IO.Path.Combine(pathF, uploadesgiles[i].FileName), System.IO.FileMode.Create))
                    {
                        uploadesgiles[i].CopyTo(stream);
                        uploadedFiles.Add(uploadesgiles[i].FileName);
                    }

                    FileResult = _fileService.SaveFile_Bar(filesN, _globalshared.UserId_G, _globalshared.BranchId_G);
                    if (Extension == "application/pdf" || Extension == ".pdf")
                    {
                        BarcodeFun_A3(pathF, filename, FileResult.ReturnedStr, valrand, project.ProjectNo, OrgaEnglishName, CustomerName);
                    }

                }
                return Ok(FileResult);
            }
            catch (Exception ex)
            {
                return Ok(FileResult2);
            }

        }


        //[HttpPost]
        [HttpPost("UploadProjectContract")]
        public IActionResult UploadProjectContract(IFormFile? file,[FromForm] int ContractId, [FromForm] int ProjectId, [FromForm] string? PageInsert)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            string fileLocation = "";
            string fileDir = "";
            string fileUrl = "";
            string fileDelete = "";
            //  HttpPostedFileBase file = Request.Files["UploadedFile"];
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            string TaxCode = _organizationsservice.GetBranchOrganizationData(orgId).Result.TaxCode;
            string vProjectNo = _projectservice.GetProjectById(_globalshared.Lang_G, ProjectId).Result.ProjectNo;
            string fileName1 = "";
            var pInsert = 1;
            if (PageInsert == "" || PageInsert == null)
            {
                pInsert = 1;
            }
            else
            {
                pInsert = Convert.ToInt32(PageInsert);
            }


            if ((file != null) && (file.ContentType.Length > 0) && !string.IsNullOrEmpty(file.FileName))
            {
                if (file.ContentType.Length > 0)
                {
                    fileName1 = System.IO.Path.GetFileName(GenerateRandomNo() + file.FileName);

                    fileUrl = "/Files/ProjectFiles/" + vProjectNo + "/" + fileName1;
                    fileLocation = Path.Combine("Files/ProjectFiles/" + vProjectNo) + "/" + fileName1;
                    fileDir = Path.Combine("Files/ProjectFiles/" + vProjectNo);
                    fileDelete = "/FileUpload/DeleteFile/" + fileName1;
                }

                try
                {
                    if (System.IO.Directory.Exists(fileDir))
                    {
                        if (System.IO.File.Exists(fileLocation))
                        {
                            System.IO.File.Delete(fileLocation);
                        }
                    }
                    else
                    {

                        System.IO.Directory.CreateDirectory(fileDir);
                        if (System.IO.File.Exists(fileLocation))
                        {
                            System.IO.File.Delete(fileLocation);
                        }
                    }

                    //Request.Files["UploadedFile"].SaveAs(fileLocation);


                    if (file != null)
                    {
                        System.Net.Http.HttpResponseMessage response = new System.Net.Http.HttpResponseMessage();


                        string path = Path.Combine("Files/ProjectFiles/" + vProjectNo) + "/";
                        string pathW = Path.Combine("Files/ProjectFiles/" + vProjectNo) + "/";

                        if (!System.IO.Directory.Exists(path))
                        {
                            System.IO.Directory.CreateDirectory(path);
                        }

                        List<string> uploadedFiles = new List<string>();
                        string pathes = "";
                        //foreach (IFormFile postedFile in postedFiles)
                        //{
                        string fileName = System.IO.Path.GetFileName(file.FileName);
                        //string fileName = System.IO.Path.GetFileName(postedFiles.FileName);

                        var path2 = Path.Combine(path, fileName1);
                        if (System.IO.File.Exists(path2))
                        {
                            System.IO.File.Delete(path2);
                        }
                        using (System.IO.FileStream stream = new System.IO.FileStream(System.IO.Path.Combine(path, fileName1), System.IO.FileMode.Create))
                        {


                            file.CopyTo(stream);
                            uploadedFiles.Add(fileName1);
                            // string returnpath = host + path + fileName;
                            //pathes.Add(pathW + fileName);
                            pathes = pathW + fileName1;
                        }



                    }
                    ////////////////////////////////////////////////////////////////////

                    ProjectFiles filesN = new ProjectFiles();

                    string filename = fileName1;
                    filesN.FileId = 0;
                    filesN.ProjectId = ProjectId;
                    filesN.FileName = fileName1;
                    filesN.IsCertified = true;
                    filesN.Notes = "";
                    filesN.FileSize = file.ContentType.Length;
                    filesN.TypeId = 1;

                    filesN.FileUrl = fileUrl;

                    filesN.BarcodeFileNum = "";

                    filesN.Extension = "." + fileName1.Split('.')[1];
                    filesN.DeleteUrl = fileDelete;
                    filesN.DeleteType = "Get";
                    filesN.ThumbnailUrl = "/Content/Free-file-icons/48px/pdf.png";
                    filesN.Type = "1";
                    filesN.TaskId = null;
                    filesN.CompanyTaxNo = TaxCode;

                    filesN.NotificationId = null;
                    filesN.PageInsert = pInsert;



                    //Save signed Contract
                    var contract = _contractService.GetAllContracts().Result.Where(x => x.ContractId == ContractId).FirstOrDefault();
                    var UrlS = _hostingEnvironment.ContentRootPath.Replace("/FileUpload/UploadProjectContract", "");

                    string Url = UrlS + fileUrl;
                    var result = _contractService.SaveContractFile(ContractId, _globalshared.UserId_G, _globalshared.BranchId_G, fileUrl);
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        var result1 = _fileService.SaveFile_Bar(filesN, _globalshared.UserId_G, _globalshared.BranchId_G);
                        return Ok(result1);
                    }
                    else
                        return Ok(result);
                }
                catch (Exception ex)
                {
                    var massage = "";
                    if (_globalshared.Lang_G == "rtl")
                    {
                        massage = "فشل في رفع العقد";
                    }
                    else
                    {
                        massage = "Failed To Upload Contract Files";
                    }
                    return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = massage });
                }
            }
            else
            {
                var massage = "";
                if (_globalshared.Lang_G == "rtl")
                {
                    massage = "فشل في رفع العقد";
                }
                else
                {
                    massage = "Failed To Upload Contract Files";
                }
                return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = massage });
            }
        }
        [HttpPost("UploadProjectContractExtra")]
        public IActionResult UploadProjectContractExtra(IFormFile? file, [FromForm] int ContractId, [FromForm] int ProjectId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            string fileLocation = "";
            string fileDir = "";
            string fileUrl = "";
            string fileDelete = "";
            // HttpPostedFileBase file = Request.Files["UploadedFile"];
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            string TaxCode = _organizationsservice.GetBranchOrganizationData(orgId).Result.TaxCode;
            string vProjectNo = _projectservice.GetProjectById(_globalshared.Lang_G, ProjectId).Result.ProjectNo;
            string fileName1 = "";
            if ((file != null) && (file.ContentType.Length > 0) && !string.IsNullOrEmpty(file.FileName))
            {
                if (file.ContentType.Length > 0)
                {

                    fileName1 = System.IO.Path.GetFileName(GenerateRandomNo() + file.FileName);

                    fileUrl = "/Uploads/ContractFileExtra/" + vProjectNo + "/" + fileName1;
                    fileLocation = Path.Combine("Uploads/ContractFileExtra/" + vProjectNo) + "/" + fileName1;
                    fileDir = Path.Combine("Uploads/ContractFileExtra/" + vProjectNo);
                    fileDelete = "/FileUpload/DeleteFile/" + fileName1;
                }

                try
                {
                    if (System.IO.Directory.Exists(fileDir))
                    {
                        if (System.IO.File.Exists(fileLocation))
                        {
                            System.IO.File.Delete(fileLocation);
                        }
                    }
                    else
                    {

                        System.IO.Directory.CreateDirectory(fileDir);
                        if (System.IO.File.Exists(fileLocation))
                        {
                            System.IO.File.Delete(fileLocation);
                        }
                    }




                    //Request.Files["UploadedFile"].SaveAs(fileLocation);


                    if (file != null)
                    {
                        System.Net.Http.HttpResponseMessage response = new System.Net.Http.HttpResponseMessage();


                        string path = Path.Combine("Uploads/ContractFileExtra/" + vProjectNo) + "/";
                        string pathW = Path.Combine("Uploads/ContractFileExtra/" + vProjectNo) + "/";

                        if (!System.IO.Directory.Exists(path))
                        {
                            System.IO.Directory.CreateDirectory(path);
                        }

                        List<string> uploadedFiles = new List<string>();
                        string pathes = "";
                        //foreach (IFormFile postedFile in postedFiles)
                        //{
                        string fileName = System.IO.Path.GetFileName(file.FileName);
                        //string fileName = System.IO.Path.GetFileName(postedFiles.FileName);

                        var path2 = Path.Combine(path, fileName1);
                        if (System.IO.File.Exists(path2))
                        {
                            System.IO.File.Delete(path2);
                        }
                        using (System.IO.FileStream stream = new System.IO.FileStream(path2, System.IO.FileMode.Create))
                        {


                            file.CopyTo(stream);
                            uploadedFiles.Add(fileName);
                            // string returnpath = host + path + fileName;
                            //pathes.Add(pathW + fileName);
                            pathes = pathW + fileName1;
                        }



                    }
                    ////////////////////////////////////////////////////////////////////
                    ProjectFiles filesN = new ProjectFiles();
                    string filename = file.FileName;
                    filesN.FileId = 0;
                    filesN.ProjectId = ProjectId;
                    filesN.FileName = fileName1;
                    filesN.IsCertified = true;
                    filesN.Notes = "";
                    filesN.FileSize = file.ContentType.Length;
                    filesN.TypeId = 1;

                    filesN.FileUrl = fileUrl;

                    filesN.BarcodeFileNum = "";

                    filesN.Extension = "." + fileName1.Split('.')[1];
                    filesN.DeleteUrl = fileDelete;
                    filesN.DeleteType = "Get";
                    filesN.ThumbnailUrl = "/Content/Free-file-icons/48px/pdf.png";
                    filesN.Type = "1";
                    filesN.TaskId = null;
                    filesN.CompanyTaxNo = TaxCode;

                    filesN.NotificationId = null;

                    //Save signed Contract
                    var contract = _contractService.GetAllContracts().Result.Where(x => x.ContractId == ContractId).FirstOrDefault();
                    var UrlS = _hostingEnvironment.ContentRootPath.Replace("/FileUpload/UploadProjectContractExtra", "");

                    string Url = UrlS + fileUrl;
                    var result = _contractService.SaveContractFileExtra(ContractId, _globalshared.UserId_G, _globalshared.BranchId_G, fileUrl);
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        var result1 = _fileService.SaveFile_Bar(filesN, _globalshared.UserId_G, _globalshared.BranchId_G);
                        return Ok(result1);
                    }
                    else
                        return Ok(result);
                }
                catch (Exception ex)
                {
                    var massage = "";
                    if (_globalshared.Lang_G == "rtl")
                    {
                        massage = "فشل في رفع العقد";
                    }
                    else
                    {
                        massage = "Failed To Upload Contract Files";
                    }
                    return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = massage });
                }
            }
            else
            {
                var massage = "";
                if (_globalshared.Lang_G == "rtl")
                {
                    massage = "فشل في رفع العقد";
                }
                else
                {
                    massage = "Failed To Upload Contract Files";
                }
                return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = massage });
            }
        }



        private readonly Random _random = new Random();

        // Generates a random number within a range.
        [HttpPost("RandomNumber")]
        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        [HttpPost("EncryptValue")]
        private string EncryptValue(string value)
        {
            string hash = "f0xle@rn";
            byte[] data = Encoding.UTF8.GetBytes(value);
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] keys = md5.ComputeHash(Encoding.UTF8.GetBytes(hash));
                using (TripleDESCryptoServiceProvider tripDesc = new TripleDESCryptoServiceProvider() { Key = keys, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7 })
                {
                    ICryptoTransform cryptoTransform = tripDesc.CreateEncryptor();
                    byte[] result = cryptoTransform.TransformFinalBlock(data, 0, data.Length);
                    return Convert.ToBase64String(result, 0, result.Length);
                }
            }
        }
        [HttpPost("BarcodeFun")]
        public IActionResult BarcodeFun(string URL, string Filename, string NumberCode, string Rand, string ProjectNo, string OrgEngName, string CustomerName)
        {
            var result = _projectservice.BarcodePDF(1, _globalshared.UserId_G);
            string File = Path.Combine(URL, Filename);
            string newFile = Path.Combine(URL, "_B_" + Rand + Filename);


            // open the reader
            PdfReader reader = new PdfReader(File);
            iTextSharp.text.Rectangle size = reader.GetPageSizeWithRotation(1);
            Document document = new Document(size);

            // open the writer
            FileStream fs = new FileStream(newFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            PdfWriter writer = PdfWriter.GetInstance(document, fs);
            document.Open();


            for (var i = 1; i <= reader.NumberOfPages; i++)
            {
                document.NewPage();


                // the pdf content
                //PdfContentByte cb = writer.DirectContent;
                PdfContentByte cb = writer.DirectContent;
                var bc = new Barcode128
                {
                    Code = NumberCode,
                    TextAlignment = Element.ALIGN_CENTER,
                    StartStopText = true,
                    CodeType = Barcode.CODE128,
                    ChecksumText = true,
                    GenerateChecksum = true,
                    Extended = false
                };



                //iTextSharp.text.pdf.draw.VerticalPositionMark x = new iTextSharp.text.pdf.draw.VerticalPositionMark();

                var xx = 0;
                string fontpath = Environment.GetEnvironmentVariable("SystemRoot") +
                 "\\fonts\\tahoma.ttf";
                BaseFont bf = BaseFont.CreateFont(fontpath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font tahomaFont = new Font(bf, 8, Font.NORMAL, BaseColor.DARK_GRAY);

                cb.SetColorFill(BaseColor.DARK_GRAY);
                cb.SetFontAndSize(bf, 8);

                // write the text in the pdf content
                //cb.BeginText();
                //string text = " ProjectNo : " + ProjectNo;

                //cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, 30, 110, 0);
                //cb.EndText();


                //cb.BeginText();
                //text = " ProjectNo : " + ProjectNo;
                //cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, text, 30, 90, 0);
                //cb.EndText();

                ColumnText ct = new ColumnText(writer.DirectContent);
                ct.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                ct.SetSimpleColumn(0, 0, 170, 120, 8, Element.ALIGN_LEFT);

                var chunk = new Chunk(OrgEngName, tahomaFont);
                var chunk2 = new Chunk(ProjectNo + " - " + CustomerName, tahomaFont);


                ct.AddElement(chunk2);

                ct.AddElement(chunk);

                ct.Go();

                iTextSharp.text.Image img = bc.CreateImageWithBarcode(cb, BaseColor.BLACK, BaseColor.BLACK);
                var barCodeRect = new iTextSharp.text.Rectangle(bc.BarcodeSize);
                iTextSharp.text.Rectangle tempRect;
                tempRect = new iTextSharp.text.Rectangle(0, 0, 140, 40);//(,,3rd,toool)

                img.ScaleAbsolute(tempRect);
                img.SetAbsolutePosition(30, 30);
                cb.AddImage(img);

                var bc2 = new BarcodeQRCode(NumberCode, 50, 50, null);
                iTextSharp.text.Image img1 = bc2.GetImage();
                iTextSharp.text.Rectangle tempRect2;
                tempRect2 = new iTextSharp.text.Rectangle(0, 0, 120, 120);//(,,3rd,toool)

                img1.ScaleAbsolute(tempRect2);
                img1.SetAbsolutePosition((size.Width - 150), 0);
                cb.AddImage(img1);


                PdfImportedPage page = writer.GetImportedPage(reader, i);
                cb.AddTemplate(page, 0, 0);
            }


            // close the streams and voilá the file should be changed :)
            document.Close();
            fs.Close();
            writer.Close();
            reader.Close();

            return Ok(result);

        }
        [HttpPost("BarcodeFun_A3")]
        public IActionResult BarcodeFun_A3(string URL, string Filename, string NumberCode, string Rand, string ProjectNo, string OrgEngName, string CustomerName)
        {
            var result = _projectservice.BarcodePDF(1, _globalshared.UserId_G);
            string File = Path.Combine(URL, Filename);
            string newFile = Path.Combine(URL, "_B_" + Rand + Filename);
            float LAST_CELL_HEIGHT = 50f;


            // open the reader
            PdfReader reader = new PdfReader(File);
            iTextSharp.text.Rectangle size = reader.GetPageSizeWithRotation(1);
            var _pdfNewSizeW = size.Width;
            var _pdfNewSizeH = size.Height + 100;
            Rectangle newRect = new Rectangle(0, 0, Convert.ToSingle(_pdfNewSizeW), Convert.ToSingle(_pdfNewSizeH));
            //Document document = new Document(size);
            Document document = new Document(newRect);
            Document.Compress = true;
            document.SetMargins(0, 0, 0, 0);

            // open the writer
            FileStream fs = new FileStream(newFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            PdfWriter writer = PdfWriter.GetInstance(document, fs);
            document.Open();


            for (var i = 1; i <= reader.NumberOfPages; i++)
            {
                document.NewPage();
                // the pdf content

                PdfContentByte cb = writer.DirectContent;

                var bc = new Barcode128
                {
                    Code = NumberCode,
                    TextAlignment = Element.ALIGN_CENTER,
                    StartStopText = true,
                    CodeType = Barcode.CODE128,
                    ChecksumText = true,
                    GenerateChecksum = true,
                    Extended = false
                };

                var xx = 0;
                string fontpath = Environment.GetEnvironmentVariable("SystemRoot") +
                 "\\fonts\\tahoma.ttf";
                BaseFont bf = BaseFont.CreateFont(fontpath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font tahomaFont = new Font(bf, 8, Font.NORMAL, BaseColor.DARK_GRAY);

                cb.SetColorFill(BaseColor.DARK_GRAY);
                cb.SetFontAndSize(bf, 8);


                ColumnText ct = new ColumnText(writer.DirectContent);
                ct.RunDirection = PdfWriter.RUN_DIRECTION_RTL;

                ct.SetSimpleColumn(0, (_pdfNewSizeH - 20), 170, 120, 8, Element.ALIGN_LEFT);

                var chunk = new Chunk(OrgEngName, tahomaFont);
                var chunk2 = new Chunk(ProjectNo + " - " + CustomerName, tahomaFont);


                ct.AddElement(chunk2);

                ct.AddElement(chunk);

                ct.Go();

                iTextSharp.text.Image img = bc.CreateImageWithBarcode(cb, BaseColor.BLACK, BaseColor.BLACK);
                var barCodeRect = new iTextSharp.text.Rectangle(bc.BarcodeSize);
                iTextSharp.text.Rectangle tempRect;
                tempRect = new iTextSharp.text.Rectangle(0, 0, 140, 40);//(,,3rd,toool)

                img.ScaleAbsolute(tempRect);
                img.SetAbsolutePosition(30, (_pdfNewSizeH - 90));
                cb.AddImage(img);

                var bc2 = new BarcodeQRCode(NumberCode, 50, 50, null);
                iTextSharp.text.Image img1 = bc2.GetImage();
                iTextSharp.text.Rectangle tempRect2;
                tempRect2 = new iTextSharp.text.Rectangle(0, 0, 120, 120);//(,,3rd,toool)

                img1.ScaleAbsolute(tempRect2);
                img1.SetAbsolutePosition((size.Width - 150), (_pdfNewSizeH - 120));

                cb.AddImage(img1);


                PdfImportedPage page = writer.GetImportedPage(reader, i);
                cb.AddTemplate(page, 0, 0);

            }


            // close the streams and voilá the file should be changed :)
            document.Close();
            fs.Close();
            writer.Close();
            reader.Close();

            return Ok(result);

        }




        //[HttpPost("UploadProjectExtractFiles")]
        //public IActionResult UploadProjectExtractFiles(ProjectExtracts projectExtracts, bool UploadFlagType)
        //{
        //    HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
        //    var resultList = new List<ViewDataUploadFilesResult>();
        //    var CurrentContext = HttpContext;
        //    filesHelper.UploadAndShowResults(CurrentContext, resultList);
        //    JsonFiles files = new JsonFiles(resultList);
        //    bool isEmpty = !resultList.Any();
        //    if (isEmpty)
        //    {
        //        return Ok("Error");
        //    }
        //    else
        //    {
        //        if (UploadFlagType)
        //        {
        //            projectExtracts.AttachmentUrl = resultList[0].url;
        //            _projectExtractsService.UpdateExtractAttachment(projectExtracts, _globalshared.UserId_G, _globalshared.BranchId_G);
        //        }
        //        else
        //        {
        //            projectExtracts.SignatureUrl = resultList[0].url;
        //            _projectExtractsService.UpdateExtractSignature(projectExtracts, _globalshared.UserId_G, _globalshared.BranchId_G);
        //        }
        //        return Ok(files);
        //    }
        //}

        [HttpPost("UploadOutInBoxFiles")]
        public IActionResult UploadOutInBoxFiles([FromForm] ContacFilesDTO contacFile)
        {
            var resultList = new List<ViewDataUploadFilesResult>();
            ContacFiles contacFiles = new ContacFiles();
            contacFiles.OutInBoxId = contacFile.OutInBoxId;
            var fullpath = Path.Combine(StorageRoot);
            string path = System.IO.Path.Combine("Files/", "ProjectFiles/");

            filesHelper.UploadAndShowResults(HttpContext, resultList, contacFile.files);

            if (!resultList.Any())
            {
                return Ok("Error");
            }

            var firstResult = resultList.First();
            contacFiles.FileName = firstResult.name;
            contacFiles.FileSize = firstResult.size;
            contacFiles.FileUrl = firstResult.url;
            contacFiles.Extension = firstResult.type;
            contacFiles.DeleteUrl = firstResult.deleteUrl;
            contacFiles.DeleteType = firstResult.deleteType;
            contacFiles.ThumbnailUrl = firstResult.thumbnailUrl;

            _contacFilesService.SaveContacFile(contacFiles, _globalshared.UserId_G, _globalshared.BranchId_G);
            _OutInBoxservice.SaveOutInboxattach((int)contacFiles.OutInBoxId, contacFiles.FileUrl);

            return Ok(new JsonFiles(resultList));
        }

        //[HttpPost("UploadOutInBoxFiles")]
        //public IActionResult UploadOutInBoxFiles(ContacFiles ContacFiles)
        //{
        //    HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
        //    var resultList = new List<ViewDataUploadFilesResult>();
        //    var CurrentContext = HttpContext;
        //    filesHelper.UploadAndShowResults(CurrentContext, resultList);
        //    JsonFiles files = new JsonFiles(resultList);
        //    bool isEmpty = !resultList.Any();
        //    if (isEmpty)
        //    {
        //        return Ok("Error");
        //    }
        //    else
        //    {
        //        ContacFiles.OutInBoxId = ContacFiles.OutInBoxId;
        //        ContacFiles.FileName = resultList[0].name;
        //        ContacFiles.FileSize = resultList[0].size;
        //        ContacFiles.FileUrl = resultList[0].url;
        //        ContacFiles.Extension = resultList[0].type;
        //        ContacFiles.DeleteUrl = resultList[0].deleteUrl;
        //        ContacFiles.DeleteType = resultList[0].deleteType;
        //        ContacFiles.ThumbnailUrl = resultList[0].thumbnailUrl;
        //        _contacFilesService.SaveContacFile(ContacFiles, _globalshared.UserId_G, _globalshared.BranchId_G);
        //        _OutInBoxservice.SaveOutInboxattach((int)ContacFiles.OutInBoxId, ContacFiles.FileUrl);

        //        return Ok(files);
        //    }
        //}
        [HttpPost("UploadPayVoucherFiles")]
        public IActionResult UploadPayVoucherFiles(ContacFiles ContacFiles)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var resultList = new List<ViewDataUploadFilesResult>();
            var CurrentContext = HttpContext;
           // filesHelper.UploadAndShowResults(CurrentContext, resultList);
            JsonFiles files = new JsonFiles(resultList);
            bool isEmpty = !resultList.Any();
            if (isEmpty)
            {
                return Ok("Error");
            }
            else
            {
                ContacFiles.OutInBoxId = ContacFiles.OutInBoxId;
                ContacFiles.FileName = resultList[0].name;
                ContacFiles.FileSize = resultList[0].size;
                ContacFiles.FileUrl = resultList[0].url;
                ContacFiles.Extension = resultList[0].type;
                ContacFiles.DeleteUrl = resultList[0].deleteUrl;
                ContacFiles.DeleteType = resultList[0].deleteType;
                ContacFiles.ThumbnailUrl = resultList[0].thumbnailUrl;
                _contacFilesService.SaveContacFile(ContacFiles, _globalshared.UserId_G, _globalshared.BranchId_G);
                return Ok(files);
            }
        }
        [HttpPost("GetFileList")]
        public IActionResult GetFileList()
        {
            var list = filesHelper.GetFileList();
            return Ok(list);
        }
        [HttpPost("DeleteFile")]
        public IActionResult DeleteFile(string file)
        {
            filesHelper.DeleteFile(file);
            return Ok("OK");
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
    public class ContacFilesDTO
    {
        public int? OutInBoxId { get; set; }
        public List<IFormFile>? files { get; set; }


    }
}
