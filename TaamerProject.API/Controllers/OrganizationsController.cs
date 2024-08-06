using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
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

    public class OrganizationsController : ControllerBase
    {
        private IOrganizationsService _organizationsservice;
        private IBranchesService _BranchesService;
        private IEmailSettingService _EmailSettingservice;
        private ISMSSettingsService _SMSSettingsService;
        private IWhatsAppSettingsService _WhatsAppSettingsService;
        private IAttDeviceService _attDeviceService;
        private IUsersService _userService;
        private IConfiguration Configuration;
        public GlobalShared _globalshared;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private string Con;
        public OrganizationsController(IOrganizationsService organizationsService, IBranchesService branchesService, IEmailSettingService emailSettingService, ISMSSettingsService sMSSettingsService,
                IWhatsAppSettingsService whatsAppSettingsService, IAttDeviceService attDeviceService, IUsersService usersService, IConfiguration _configuration, IWebHostEnvironment webHostEnvironment)
        {
            this._organizationsservice = organizationsService;
            this._BranchesService = branchesService;
            this._EmailSettingservice = emailSettingService;
            this._SMSSettingsService = sMSSettingsService;
            this._WhatsAppSettingsService = whatsAppSettingsService;

            this._attDeviceService = attDeviceService;
            this._userService = usersService;
            Configuration = _configuration; Con = this.Configuration.GetConnectionString("DBConnection");
            HttpContext httpContext = HttpContext;

            _globalshared = new GlobalShared(httpContext);
            _hostingEnvironment = webHostEnvironment;
        }
        [HttpGet("GetBranchOrganization")]
        public ActionResult GetBranchOrganization()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            return Ok(_organizationsservice.GetBranchOrganizationData(orgId));
        }
        [HttpGet("GetComDomainLink_Org")]
        public ActionResult GetComDomainLink_Org()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            return Ok(_organizationsservice.GetComDomainLink_Org(orgId));
        }
        [HttpGet("GetApplicationVersion_Org")]
        public ActionResult GetApplicationVersion_Org()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            return Ok(_organizationsservice.GetApplicationVersion_Org(orgId));
        }
        [HttpGet("GetEmailOrganization")]
        public ActionResult GetEmailOrganization()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            //var result = _EmailSettingservice.GetEmailSetting(orgId);
            var result = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            return Ok(result);
        }
        [HttpPost("SavepartialOrganizations")]
        public ActionResult SavepartialOrganizations(Organizations organizations)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _organizationsservice.SavepartialOrganizations(organizations, _globalshared.UserId_G, _globalshared.BranchId_G, organizations.VAT??0, organizations.VATSetting??0);
            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {
                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }
        //Edit by Mohamed Nasser
        [HttpPost("SaveOrganizations")]
        public ActionResult SaveOrganizations(IFormFile? UploadedFile, [FromForm] Organizations organizations)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            //HttpPostedFileBase file = Request.Files["UploadedImage"];
            //    if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
            //    {
            //        if (Request.Files["UploadedImage"].ContentLength > 0)
            //        {

            //            string fileName = System.IO.Path.GetFileName(GenerateRandomNo() + Request.Files["UploadedImage"].FileName);

            //            string fileLocation = Server.MapPath("~/Uploads/Organizations/pictures/") + fileName;
            //            try
            //            {
            //                if (System.IO.File.Exists(fileLocation))
            //                {
            //                    System.IO.File.Delete(fileLocation);
            //                }
            //                string width = Request.Form["imgwidth"];
            //                string hight = Request.Form["imghight"];

            //                double intwidth = Convert.ToDouble(width);// int.Parse(width);// Convert.ToInt32(width);
            //                double inthight = Convert.ToDouble(hight);// Convert.ToInt32(hight);

            //                //edit by M.Salah
            //                var scaleImage = ScaleImage(Bitmap.FromStream(Request.Files["UploadedImage"].InputStream), (int)inthight, (int)intwidth);

            //                scaleImage.Save(fileLocation);


            //                //WebImage img = new WebImage(Request.Files["UploadedImage"].InputStream);

            //                //    img.Resize(100, 100);

            //                //img.Save(fileLocation);
            //                //Request.Files["UploadedImage"].SaveAs(fileLocation);
            //                organizations.LogoUrl = "/Uploads/Organizations/pictures/" + fileName;
            //            }
            //            catch (Exception ex)
            //            {
            //                var massage = "";
            //                if (_globalshared.Lang_G == "rtl")
            //                {
            //                    massage = "فشل في رفع الصورة";
            //                }
            //                else
            //                {
            //                    massage = "Failed To Upload Image";
            //                }
            //                return Ok(new GeneralMessage { Result = false, Message = massage } );
            //            }
            //        }
            //    }
            if (UploadedFile != null)
            {
                System.Net.Http.HttpResponseMessage response = new System.Net.Http.HttpResponseMessage();


                string path = System.IO.Path.Combine("Uploads/", "Organizations/pictures/");
                string pathW = System.IO.Path.Combine("/Uploads/", "Organizations/pictures/");

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


                if (pathes != null)
                {
                    organizations.LogoUrl = pathes;
                }
            }
            var result = _organizationsservice.SaveOrganizations(organizations, _globalshared.UserId_G, _globalshared.BranchId_G);
            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {
                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }
        [HttpPost("SaveOrganizationSettings")]
        public ActionResult SaveOrganizationSettings(Organizations organizations)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _organizationsservice.SaveOrganizationSettings(organizations, _globalshared.UserId_G, _globalshared.BranchId_G);

            return Ok(result);
        }

        [HttpPost("DeleteOrganizations")]
        public ActionResult DeleteOrganizations(int OrganizationId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _organizationsservice.DeleteOrganizations(OrganizationId, _globalshared.UserId_G, _globalshared.BranchId_G);
            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {
                result.ReasonPhrase = "Deleted Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Deleted Falied";
            }
            return Ok(result);
        }
        [HttpPost("SaveEmailSetting")]
        public ActionResult SaveEmailSetting(EmailSetting EmailSetting)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _EmailSettingservice.SaveEmailSetting(EmailSetting, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("SaveComDomainLink")]
        public ActionResult SaveComDomainLink(Organizations organizations)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _organizationsservice.SaveComDomainLink(organizations, _globalshared.UserId_G, _globalshared.BranchId_G);

            if (result.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    string fileName = "ComDomainLink.Jpeg";
                    string fileLocation = Path.Combine("Uploads/Organizations/DomainLink/") + fileName;

                    string ImgReturn = "";
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(organizations.ComDomainLink, QRCodeGenerator.ECCLevel.Q, true);
                    QRCode qrCode = new QRCode(qrCodeData);
                    using (Bitmap bitMap = qrCode.GetGraphic(20))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] byteImage = ms.ToArray();
                            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);

                            ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                            if (System.IO.File.Exists(fileLocation))
                            {
                                System.IO.File.Delete(fileLocation);
                            }
                            img.Save(fileLocation, System.Drawing.Imaging.ImageFormat.Jpeg);

                        }

                    }
                }
                catch (Exception ex)
                {

                }

            }

            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {

                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }

        [HttpPost("SaveAppVersion")]
        public ActionResult SaveAppVersion(Organizations organizations)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _organizationsservice.SaveAppVersion(organizations, _globalshared.UserId_G, _globalshared.BranchId_G);



            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {

                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }

        [HttpPost("SaveSupport")]
        public ActionResult SaveSupport(Organizations organizations)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _organizationsservice.SaveSupport(organizations, _globalshared.UserId_G, _globalshared.BranchId_G);



            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {

                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }
        [HttpPost("SavesmsSetting")]
        public ActionResult SavesmsSetting(SMSSettings sMSSettings)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _SMSSettingsService.SavesmsSetting(sMSSettings, _globalshared.UserId_G, _globalshared.BranchId_G);
            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {
                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }
        [HttpPost("SaveWhatsAppSetting")]
        public ActionResult SaveWhatsAppSetting(WhatsAppSettings whatsAppSettings)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _WhatsAppSettingsService.SaveWhatsAppSetting(whatsAppSettings, _globalshared.UserId_G, _globalshared.BranchId_G);
            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {
                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }
        [HttpPost("GetEmailSetting")]
        public ActionResult GetEmailSetting()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_EmailSettingservice.GetEmailSetting(_globalshared.BranchId_G));
        }
        [HttpGet("GetAttDeviceSetting")]

        public ActionResult GetAttDeviceSetting()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_attDeviceService.GetDevicesetting(_globalshared.BranchId_G));
        }
        [HttpPost("SaveAttDeviceSetting")]

        public ActionResult SaveAttDeviceSetting(AttDeviceSitting attDeviceSitting)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _attDeviceService.SaveAttdeviceSetting(attDeviceSitting, _globalshared.UserId_G, _globalshared.BranchId_G);
            if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.OK)
            {
                result.ReasonPhrase = "Saved Successfully";
            }
            else if (_globalshared.Lang_G == "ltr" && result.StatusCode == HttpStatusCode.BadRequest)
            {
                result.ReasonPhrase = "Saved Falied";
            }
            return Ok(result);
        }
        [HttpGet("GetsmsSetting")]
        public ActionResult GetsmsSetting()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_SMSSettingsService.GetsmsSetting(_globalshared.BranchId_G));
        }
        [HttpGet("GetWhatsAppSetting")]
        public ActionResult GetWhatsAppSetting()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_WhatsAppSettingsService.GetWhatsAppSetting(_globalshared.BranchId_G));
        }
        [HttpGet("CheckEmailOrganization")]
        public ActionResult CheckEmailOrganization(int? OrganizationId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_organizationsservice.CheckEmailOrganization(OrganizationId));
        }


        [HttpGet("GenerateRandomNo")]
        public int GenerateRandomNo()
        {
            int _min = 1000;
            int _max = 9999;
            Random _rdm = new Random();
            return _rdm.Next(_min, _max);
        }

        [HttpGet("GetOrganizationDataLogin")]
        public ActionResult GetOrganizationDataLogin()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_organizationsservice.GetOrganizationDataLogin(_globalshared.Lang_G));
        }
        [HttpGet("GenerateUserQR")]
        public ActionResult GenerateUserQR()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var org = _organizationsservice.GetOrganizationData(_globalshared.BranchId_G).Result;
            var user = _userService.GetUserById(_globalshared.UserId_G, _globalshared.Lang_G).Result;
            var qrstring = org.ComDomainLink + "/" + user.Email;


            try
            {
                string fileName = "EmpQrCodeImg.Jpeg";
                string fileLocation = Path.Combine("~/Uploads/Organizations/DomainLink/") + fileName;

                string ImgReturn = "";
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrstring, QRCodeGenerator.ECCLevel.Q, true);
                QRCode qrCode = new QRCode(qrCodeData);
                using (Bitmap bitMap = qrCode.GetGraphic(20))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] byteImage = ms.ToArray();
                        System.Drawing.Image img = System.Drawing.Image.FromStream(ms);

                        ImgReturn = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                        if (System.IO.File.Exists(fileLocation))
                        {
                            System.IO.File.Delete(fileLocation);
                        }
                        img.Save(fileLocation, System.Drawing.Imaging.ImageFormat.Jpeg);

                    }

                }
                return Ok(new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = "تم الحفظ" });


            }
            catch (Exception ex)
            {
                return Ok(new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "فشل في الحفظ" });

            }




        }
        [HttpGet("GetOrganizationData")]
        public IActionResult GetOrganizationData()
        {            
            int orgId = _BranchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            var objOrganization = _organizationsservice.GetBranchOrganizationData(orgId).Result;
            return Ok(objOrganization);

        }

        [HttpGet("SendMail_test")]
        public IActionResult SendMail_test(string Email)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);

            var objOrganization = _organizationsservice.SendMail_test(_globalshared.BranchId_G,Email, "اميل تجريبي", "اذا استطعت قراءة هذا البريد، فإن اعدادات البريد لديك صحيحة", true);
            return Ok(objOrganization);

        }

        [HttpGet("SendSMS_test")]
        public ActionResult SendSMS_test(string Mobile)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_SMSSettingsService.SendSMS_Test(_globalshared.UserId_G,_globalshared.BranchId_G, Mobile,"test"));
        }
        [HttpGet("SendWhatsApp_test")]
        public ActionResult SendWhatsApp_test(string Mobile, string? environmentURL)
        {
            string PDFURL = "";
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_WhatsAppSettingsService.SendWhatsApp_Test(_globalshared.UserId_G, _globalshared.BranchId_G, Mobile, "test", environmentURL??"", PDFURL??""));
        }
        public static Image ScaleImage(Image image, int height, int width)
        {
            //double ratio = (double)height / image.Height;
            //int newWidth = (int)(image.Width * ratio);
            //int newHeight = (int)(image.Height * ratio);
            int newWidth = width;
            int newHeight = height;
            Bitmap newImage = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(newImage))
            {
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            image.Dispose();
            return newImage;
        }
        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }
    }
}
