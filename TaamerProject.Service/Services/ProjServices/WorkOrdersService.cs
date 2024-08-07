using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models;
using TaamerProject.Models.Common.FIlterModels;
using TaamerProject.Models.Common;
using TaamerProject.Service.Interfaces;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Models.DBContext;
using TaamerProject.Service.Generic;
using TaamerProject.Repository.Repositories;
using System.Net;
using TaamerProject.Service.IGeneric;
using System.Net.Mail;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Haseeb.Service.LocalResources;

namespace TaamerProject.Service.Services
{
    public class WorkOrdersService :  IWorkOrdersService
    {
        private readonly IWorkOrdersRepository _workordersRepository;
        private readonly INotificationRepository _NotificationRepository;
        private readonly IEmailSettingRepository _EmailSettingRepository;
        private readonly IBranchesRepository _BranchesRepository;
        private readonly ICustomerRepository _CustomerRepository;
        private readonly ISystemAction _SystemAction;
        private readonly IProjectRepository _projectRepository;
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly IUsersRepository _UsersRepository;
        public WorkOrdersService(IWorkOrdersRepository workordersRepository, INotificationRepository notificationRepository,
           IEmailSettingRepository emailSettingRepository, IBranchesRepository branchesRepository,
           ICustomerRepository customerRepository, TaamerProjectContext dataContext
            , ISystemAction systemAction, IProjectRepository projectRepository, IUsersRepository usersRepository)
        {
            _CustomerRepository = customerRepository;
            _workordersRepository = workordersRepository;
            _NotificationRepository = notificationRepository;
            _EmailSettingRepository = emailSettingRepository;
            _BranchesRepository = branchesRepository;
            _projectRepository = projectRepository;
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _UsersRepository = usersRepository;
        }
        public async Task<IEnumerable<WorkOrdersVM>> GetAllWorkOrders(int BranchId, int UserId)
        {
            // var user = _UsersRepository.GetById(UserId);
            Users? user = await _TaamerProContext.Users.Where(s => s.UserId == UserId).FirstOrDefaultAsync();

            if (user !=null && user.UserName == "admin")
            {
                var workorders = await _workordersRepository.GetAllWorkOrdersForAdmin(BranchId);
                return workorders;
            }
            else
            {
                var workorders =await _workordersRepository.GetAllWorkOrders(BranchId);
                return workorders;
            }

        }

        public async Task<IEnumerable<WorkOrdersVM>> GetAllWorkOrdersFilterd(int BranchId, int UserId,int? CustomerId)
        {
            // var user = _UsersRepository.GetById(UserId);
            Users? user = await _TaamerProContext.Users.Where(s => s.UserId == UserId).FirstOrDefaultAsync();

            if (user != null && user.UserName == "admin")
            {
                var workorders = await _workordersRepository.GetAllWorkOrdersForAdminByCustomer(BranchId, CustomerId);
                return workorders;
            }
            else
            {
                var workorders = await _workordersRepository.GetAllWorkOrdersByCustomer(BranchId, CustomerId);
                return workorders;
            }

        }

        public async Task<IEnumerable<WorkOrdersVM>> GetAllWorkOrdersFilterd(int BranchId, int UserId, int? CustomerId,string? SearchText)
        {
            // var user = _UsersRepository.GetById(UserId);
            Users? user = await _TaamerProContext.Users.Where(s => s.UserId == UserId).FirstOrDefaultAsync();

            if (user != null && user.UserName == "admin")
            {
                var workorders = await _workordersRepository.GetAllWorkOrdersForAdminByCustomer(BranchId, CustomerId, SearchText);
                return workorders;
            }
            else
            {
                var workorders = await _workordersRepository.GetAllWorkOrdersByCustomer(BranchId, CustomerId, SearchText);
                return workorders;
            }

        }

        public Task<IEnumerable<WorkOrdersVM>> GetAllWorkOrdersyProjectId(int ProjectId)
        {

                var workorders = _workordersRepository.GetAllWorkOrdersyProjectId(ProjectId);
                return workorders;
           

        }
        public GeneralMessage SaveWorkOrders(WorkOrders workOrders, int UserId, int BranchId)
        {
            try
            {
                //var BranchIdOfUser = _UsersRepository.GetById(workOrders.ExecutiveEng);
                var BranchIdOfUser =  _TaamerProContext.Users.Where(s => s.UserId == workOrders.ExecutiveEng).FirstOrDefault()!.BranchId??0;

                // Users? BranchIdOfUser = _TaamerProContext.Users.Where(s => s.ExecutiveEng == ).FirstOrDefault();
                var totaldays = 0.0;
                if(workOrders.ProjectId!=null && workOrders.ProjectId != 0) {
                    //  var project = _projectRepository.GetById(workOrders.ProjectId);
                Project? project = _TaamerProContext.Project.Where(s => s.ProjectId == workOrders.ProjectId).FirstOrDefault();
                if (project != null)
                {
                    BranchIdOfUser = project.BranchId;
                }

                    
                if (project != null && project.StopProjectType == 1)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ أمر العمل";
                    _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.workOrderStoppedProject };

                }
               }



                DateTime resultEnd = DateTime.ParseExact(workOrders.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime resultStart = DateTime.ParseExact(workOrders.OrderDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                totaldays = (resultEnd - resultStart).TotalDays + 1;

                if (workOrders.WOStatus == null)
                    workOrders.WOStatus = 1;




                var ResultnowString = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                DateTime resultNow = DateTime.ParseExact(ResultnowString, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                if (resultStart == resultNow)
                {
                    workOrders.WOStatus = 2;
                }
                else
                {
                    workOrders.WOStatus = 1;
                }


                string MyWOStatus = "";
                if (workOrders.WOStatus == 1)
                    MyWOStatus = "لم تبدأ";
                else if (workOrders.WOStatus == 2)
                    MyWOStatus = "قيد التشغيل";
                else if (workOrders.WOStatus == 3)
                    MyWOStatus = "منتهية";
                else //if (workOrders.WOStatus == 4)
                    MyWOStatus = "ملغية";

                if (workOrders.WorkOrderId == 0)
                {
                    //workOrders.OrderNo = Convert.ToString(_workordersRepository.GetMaxOrderNumber(BranchId) + 1);
                    workOrders.UserId = UserId;
                    workOrders.BranchId = BranchIdOfUser;
                    workOrders.AddUser = UserId;
                    workOrders.AddDate = DateTime.Now;
                    workOrders.IsDeleted = false;
                    workOrders.WOStatustxt = MyWOStatus;
                    workOrders.NoOfDays = Convert.ToInt32(totaldays);
                    //  workOrders.WOStatus=
                    _TaamerProContext.WorkOrders.Add(workOrders);
                    try
                    {
                        SendMail(workOrders, BranchId, UserId);
                    }
                    catch (Exception ex)
                    {
                    }
                    //var UserNotification = new Notification();
                    //UserNotification.ReceiveUserId = workOrders.ExecutiveEng;
                    //UserNotification.Name = "work order";
                    //UserNotification.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                    //UserNotification.HijriDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CreateSpecificCulture("ar")); ;
                    //UserNotification.SendUserId = UserId;
                    //UserNotification.Type = 1; // notification
                    //UserNotification.Description = "لديك امر عمل : " + workOrders.Note;
                    //UserNotification.AllUsers = false;
                    //UserNotification.SendDate = DateTime.Now;
                    //UserNotification.AddUser = UserId;
                    //UserNotification.AddDate = DateTime.Now;
                    //UserNotification.BranchId = BranchIdOfUser.BranchId;
                    //UserNotification.IsHidden = false;
                    //_NotificationRepository.Add(UserNotification);

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافةأمر عمل جديد";
                    _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully,ReturnedParm= workOrders.WorkOrderId };
                }
                else
                {
                    //var workOrdersUpdated = _workordersRepository.GetById(workOrders.WorkOrderId);
                    WorkOrders? workOrdersUpdated = _TaamerProContext.WorkOrders.Where(s => s.WorkOrderId == workOrders.WorkOrderId).FirstOrDefault();
                    if (workOrdersUpdated != null)
                    {
                        workOrdersUpdated.WorkOrderId = workOrders.WorkOrderId;
                        workOrdersUpdated.OrderNo = workOrders.OrderNo;
                        workOrdersUpdated.UserId = UserId;
                        workOrdersUpdated.UpdateUser = UserId;
                        // workOrdersUpdated.UpdatedDate = DateTime.Now;
                        workOrdersUpdated.UpdateDate = DateTime.Now;

                        workOrdersUpdated.ExecutiveEng = workOrders.ExecutiveEng;
                        workOrdersUpdated.ResponsibleEng = workOrders.ResponsibleEng;
                        workOrdersUpdated.CustomerId = workOrders.CustomerId;

                        workOrdersUpdated.Note = workOrders.Note;

                        workOrdersUpdated.OrderDiscount = workOrders.OrderDiscount;
                        workOrdersUpdated.OrderPaid = workOrders.OrderPaid;
                        workOrdersUpdated.OrderTax = workOrders.OrderTax;
                        workOrdersUpdated.OrderValue = workOrders.OrderValue;
                        workOrdersUpdated.OrderRemaining = workOrders.OrderRemaining;
                        workOrdersUpdated.OrderValueAfterTax = workOrders.OrderValueAfterTax;
                        workOrdersUpdated.Sketch = workOrders.Sketch;
                        workOrdersUpdated.Location = workOrders.Location;
                        workOrdersUpdated.District = workOrders.District;
                        workOrdersUpdated.PieceNo = workOrders.PieceNo;
                        workOrdersUpdated.InstrumentNo = workOrders.InstrumentNo;
                        workOrdersUpdated.ExecutiveType = workOrders.ExecutiveType;
                        workOrdersUpdated.ContractNo = workOrders.ContractNo;
                        workOrdersUpdated.AgentId = workOrders.AgentId;
                        workOrdersUpdated.AgentMobile = workOrders.AgentMobile;
                        workOrdersUpdated.Social = workOrders.Social;

                        workOrdersUpdated.DiscountReason = workOrders.DiscountReason;
                        workOrdersUpdated.ProjectId = workOrders.ProjectId;

                        workOrdersUpdated.EndDate = workOrders.EndDate;
                        workOrdersUpdated.WOStatus = workOrders.WOStatus;
                        workOrdersUpdated.WOStatustxt = MyWOStatus;
                        workOrdersUpdated.BranchId = BranchIdOfUser;
                        workOrders.NoOfDays = Convert.ToInt32(totaldays);
                        workOrdersUpdated.Required = workOrders.Required;

                        _TaamerProContext.SaveChanges();
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote = " تعديل أمر عمل رقم " + workOrders.WorkOrderId;
                        _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                        //-----------------------------------------------------------------------------------------------------------------
                        return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_EditedSuccessfully, ReturnedParm = workOrders.WorkOrderId };
                    }
                    else
                    { 
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote = "فشل في حفظ أمر العمل";
                        _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 2, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ أمر العمل";
                _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage EditWorkOrdersFile(WorkOrders workOrders, int UserId, int BranchId)
        {
            try
            {
                WorkOrders? workOrdersUpdated = _TaamerProContext.WorkOrders.Where(s => s.WorkOrderId == workOrders.WorkOrderId).FirstOrDefault();
                if (workOrdersUpdated != null)
                {
                    workOrdersUpdated.AttatchmentUrl = workOrders.AttatchmentUrl;

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل ملف المهمة الادارية رقم " + workOrders.WorkOrderId;
                    _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_EditedSuccessfully, ReturnedParm = workOrders.WorkOrderId };
                }
                else
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ ملف المهمة الادارية";
                    _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 2, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ ملف المهمة الادارية ";
                _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        private bool SendMail(WorkOrders workOrders, int BranchId, int UserId)
        {
            try
            {
                DateTime date = new DateTime();
                //var branch = _BranchesRepository.GetById(BranchId).OrganizationId;
                Branch? branch = _TaamerProContext.Branch.Where(s => s.BranchId == BranchId).FirstOrDefault();

                //var DateOfTask = ProjectPhasesTasks.AddDate.Value.ToString("yyyy-MM-dd HH:MM");
                var DateOfTask = workOrders.AddDate.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CreateSpecificCulture("en"));
                var customerName = "";
                if (workOrders.CustomerId != null) {
                    //customerName = _CustomerRepository.GetById(workOrders.CustomerId).CustomerNameAr;
                    Customer? customer =   _TaamerProContext.Customer.Where(s=>s.CustomerId == workOrders.CustomerId).FirstOrDefault();
                    if (customer != null)
                    {
                        customerName = customer.CustomerNameAr;
                    }
                }
                string textBody = "   السيد/ة   " + workOrders?.ExecutiveEngineer?.FullName +  "   المحترم  <br/> السلام عليكم ورحمة الله وبركاتة <br/> لديك مهمة ادارية جديدة حسب البيانات الواردة ادناة  <br/> " + "<table border='1'style='text-align:center;padding:3px;'><tr><td style='border=1px solid #eee'>المطلوب تنفيذة</td><td>" + workOrders.Required + "</td></tr><tr><td>اسم العميل </td><td>" + customerName + "</td></tr><tr><td>تاريخ البداية</td><td>" + workOrders.OrderDate + "</td></tr><tr><td>المدة</td><td>" + workOrders.NoOfDays + " days </td></tr></table> <br/> مع تحيات الادارة";
                var mail = new MailMessage();
                
                var loginInfo = new NetworkCredential(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId??0).Result.SenderEmail, _EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.Password);

                if (_EmailSettingRepository.GetEmailSetting(BranchId).Result.DisplayName != null)
                {
                    mail.From = new MailAddress(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0 ).Result.SenderEmail, _EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.DisplayName);
                }
                else
                {
                    mail.From = new MailAddress(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.SenderEmail, "لديك اشعار من نظام تعمير السحابي");
                }

                // mail.From = new MailAddress(_EmailSettingRepository.GetEmailSetting(branch).SenderEmail);
                var Worker = _TaamerProContext.Users.Where(s => s.UserId == workOrders.ExecutiveEng).FirstOrDefault();
                mail.To.Add(new MailAddress(Worker?.Email ?? ""));
                //mail.Subject = "لديك مهمة جديده علي مشروع رقم " + project.ProjectNo + "";
                mail.Subject = "لديك مهمة ادارية جديدة  ";
                try
                {
                    mail.Body = textBody;// "لديك مهمه جديدة : " + ProjectPhasesTasks.DescriptionAr + ":" + ProjectPhasesTasks.Notes + " علي مشروع رقم " + ProjectPhasesTasks.Project.ProjectNo + " للعميل " + ProjectPhasesTasks.Project.customer.CustomerNameAr;
                    mail.IsBodyHtml = true;
                }
                catch (Exception)
                {
                    mail.Body = workOrders.Required + " : " + workOrders.Note ?? "";
                }
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var smtpClient = new SmtpClient(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.Host);
                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = loginInfo; 
                //smtpClient.Port = 587;
                smtpClient.Port = Convert.ToInt32(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.Port);

                smtpClient.Send(mail);
                return true;
            }
            catch (Exception wx)
            {
                return false;
            }
        }

        public GeneralMessage DeleteWorkOrders(int WorkOrderId, int UserId, int BranchId)
        {
            try
            {
               // WorkOrders constraint = _workordersRepository.GetById(WorkOrderId);
                WorkOrders? constraint = _TaamerProContext.WorkOrders.Where(s => s.WorkOrderId == WorkOrderId).FirstOrDefault();
                if (constraint != null)
                {
                    constraint.IsDeleted = true;
                    constraint.DeleteDate = DateTime.Now;
                    constraint.DeleteUser = UserId;
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " حذف أمر عمل رقم " + WorkOrderId;
                    _SystemAction.SaveAction("DeleteWorkOrders", "WorkOrdersService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حذف أمر العمل";
                _SystemAction.SaveAction("SaveWorkOrders", "WorkOrdersService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }
        public Task<IEnumerable<WorkOrdersVM>> SearchWorkOrders(WorkOrdersVM WorkOrdersSearch, string lang, int BranchId)
        {      
            var workorders =  _workordersRepository.SearchWorkOrders(WorkOrdersSearch, lang, BranchId);
                       
            return workorders;
        }
        public Task<int> GetMaxOrderNumber(int BranchId)
        {
            var workorders = _workordersRepository.GetMaxOrderNumber();
            return workorders;
        }
        public async Task<IEnumerable<WorkOrdersVM>> GetAllWorkOrdersByDateSearch(string DateFrom, string DateTo, int BranchId, int UserId)
        {
           // var user = _UsersRepository.GetById(UserId);
            Users? user = _TaamerProContext.Users.Where(s => s.UserId == UserId).FirstOrDefault();

            if (user != null && user.UserName == "admin")
            {
                var workorders = _workordersRepository.GetAllWorkOrdersByDateSearchForAdmin(DateFrom, DateTo, BranchId).Result;
                return workorders;
            }
            else
            {
                var workorders = _workordersRepository.GetAllWorkOrdersByDateSearch(DateFrom, DateTo, BranchId).Result;
                return workorders;
            }
        }
        public Task<WorkOrdersVM> GetWorkOrderById(int WorkOrderId, string lang)
        {
            return _workordersRepository.GetWorkOrderById(WorkOrderId, lang);
        }
        public Task<IEnumerable<WorkOrdersVM>>  GetLateWorkOrdersByUserId(string EndDateP, int? UserId, int BranchId)
        {
            var workorders = _workordersRepository.GetLateWorkOrdersByUserId(EndDateP, UserId, BranchId);
            return workorders;
        }

        public Task<IEnumerable<WorkOrdersVM>> GetLateWorkOrdersByUserIdFilterd(string EndDateP, int? UserId, int BranchId,int? customerid,int? ProjectId)
        {
            var workorders = _workordersRepository.GetLateWorkOrdersByUserIdFilterd(EndDateP, UserId, BranchId, customerid, ProjectId);
            return workorders;
        }

        public Task<IEnumerable<WorkOrdersVM>> GetLateWorkOrdersByUserIdFilterd(string EndDateP, int? UserId, int BranchId, int? customerid, int? ProjectId,string? Searchtext)
        {
            var workorders = _workordersRepository.GetLateWorkOrdersByUserIdFilterd(EndDateP, UserId, BranchId, customerid, ProjectId, Searchtext);
            return workorders;
        }
        public Task<IEnumerable<WorkOrdersVM>> GetNewWorkOrdersByUserId(string EndDateP, int? UserId, int BranchId)
        {
            var workorders = _workordersRepository.GetNewWorkOrdersByUserId(EndDateP, UserId, BranchId);
            return workorders;
        }

        public Task<IEnumerable<WorkOrdersVM>> GetNewWorkOrdersByUserIdFilterd(string EndDateP, int? UserId, int BranchId, int? customerid, int? ProjectId)
        {
            var workorders = _workordersRepository.GetNewWorkOrdersByUserIdFilterd(EndDateP, UserId, BranchId, customerid, ProjectId);
            return workorders;
        }

        public Task<IEnumerable<WorkOrdersVM>> GetNewWorkOrdersByUserIdFilterd(string EndDateP, int? UserId, int BranchId, int? customerid, int? ProjectId,string? Searchtext)
        {
            var workorders = _workordersRepository.GetNewWorkOrdersByUserIdFilterd(EndDateP, UserId, BranchId, customerid, ProjectId, Searchtext);
            return workorders;
        }
        public Task<IEnumerable<WorkOrdersVM>> GetWorkOrdersByUserId(int? UserId, int BranchId)
        {
            var workorders = _workordersRepository.GetWorkOrdersByUserId(UserId, BranchId);
            return workorders;
        }
        public Task<IEnumerable<WorkOrdersVM>> GetWorkOrdersByUserIdandtask(string task, int? UserId, int BranchId)
        {
            var workorders = _workordersRepository.GetWorkOrdersByUserIdandtask(task, UserId, BranchId);
            return workorders;
        }


        public Task<IEnumerable<WorkOrdersVM>> GetWorkOrdersBytask(string task, int BranchId)
        {
            var workorders = _workordersRepository.GetWorkOrdersBytask(task, BranchId);
            return workorders;
        }

        public Task<IEnumerable<WorkOrdersVM>> GetDayWeekMonth_Orders(int? UserId, int Status, int BranchId, int Flag, string StartDate, string EndDate)
        {
            var WorkOrders = _workordersRepository.GetDayWeekMonth_Orders(UserId, Status, BranchId, Flag, StartDate, EndDate);
            return WorkOrders;
        }

        public Task<IEnumerable<ProjectPhasesTasksVM>> GetWorkOrderReport(int? UserId, int Status, int BranchId, string Lang, string StartDate, string EndDate)
        {
            var WorkOrders = _workordersRepository.GetWorkOrderReport(UserId,  BranchId, Lang, Status, StartDate, EndDate);
            return WorkOrders;
        }
        public Task<IEnumerable<ProjectPhasesTasksVM>> GetWorkOrderReport(int? UserId, int Status, int BranchId, string Lang, string StartDate, string EndDate,string? Searchtext)
        {
            var WorkOrders = _workordersRepository.GetWorkOrderReport(UserId, BranchId, Lang, Status, StartDate, EndDate, Searchtext);
            return WorkOrders;
        }
        public Task<List<ProjectPhasesTasksVM>> GetWorkOrderReport_print(int? UserId, int Status, int BranchId, string Lang, string StartDate, string EndDate)
        {
            var WorkOrders = _workordersRepository.GetWorkOrderReport_print(UserId, BranchId, Lang, Status, StartDate, EndDate);
            return WorkOrders;
        }

        public Task<IEnumerable<ProjectPhasesTasksVM>> GetALlWorkOrderReport(string Lang, int BranchId)
        {
            var WorkOrders = _workordersRepository.GetALlWorkOrderReport(Lang,BranchId);
            return WorkOrders;
        }
        public Task<IEnumerable<ProjectPhasesTasksVM>> GetWorkOrderReport_ptoject(int? projectid,string Lang, string StartDate, string EndDate, int BranchId)
        {
            var WorkOrders = _workordersRepository.GetWorkOrderReport_ptoject(Lang,projectid,StartDate, EndDate,BranchId);
            return WorkOrders;
        }

        public Task<IEnumerable<ProjectPhasesTasksVM>> GetWorkOrderReport_ptoject(int? projectid, string Lang, string StartDate, string EndDate, int BranchId,string? SeachText)
        {
            var WorkOrders = _workordersRepository.GetWorkOrderReport_ptoject(Lang, projectid, StartDate, EndDate, BranchId, SeachText);
            return WorkOrders;
        }
        public GeneralMessage FinishOrder(WorkOrders workOrders, int UserId, int BranchId)
        {
            try
            {
              //  var ProTaskUpdated = _workordersRepository.GetById(workOrders.WorkOrderId);
                WorkOrders? ProTaskUpdated = _TaamerProContext.WorkOrders.Where(s => s.WorkOrderId == workOrders.WorkOrderId).FirstOrDefault();           
                if (ProTaskUpdated != null)
                {
                    ProTaskUpdated.WOStatus = workOrders.WOStatus;
                    ProTaskUpdated.WOStatustxt = workOrders.WOStatustxt;
                    ProTaskUpdated.PercentComplete = workOrders.PercentComplete;

                    //ProTaskUpdated.Active = false;
                    ProTaskUpdated.UpdateUser = UserId;
                    //ProTaskUpdated.UpdatedDate = DateTime.Now;
                    ProTaskUpdated.UpdateDate = DateTime.Now;
                }
                _TaamerProContext.SaveChanges(); 
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " تم انهاء أمر العمل رقم " + workOrders.WorkOrderId;
                _SystemAction.SaveAction("FinishOrder", "WorkOrdersService", 2, Resources.work_order_terminated, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.work_order_terminated };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = Resources.General_SavedFailed;
                _SystemAction.SaveAction("FinishOrder", "WorkOrdersService", 2, Resources.work_order_Faild, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.work_order_Faild };
            }
        }



       
    }


    }
