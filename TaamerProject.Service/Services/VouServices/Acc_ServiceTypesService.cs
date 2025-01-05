using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using TaamerProject.Models;
using TaamerProject.Models.Common.FIlterModels;
using TaamerProject.Models.Common;
using TaamerProject.Service.Interfaces;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Models.DBContext;
using TaamerProject.Service.Generic;
using TaamerProject.Service.IGeneric;
using Twilio.Base;
using Haseeb.Service.LocalResources;
using TaamerProject.Repository.Repositories;

namespace TaamerProject.Service.Services
{
    public class Acc_ServiceTypesService : IAcc_ServiceTypesService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly IAcc_ServiceTypesRepository _Acc_ServiceTypesRepository;
        public Acc_ServiceTypesService(IAcc_ServiceTypesRepository Acc_ServiceTypesRepository
            , TaamerProjectContext dataContext, ISystemAction systemAction)
        {
            _TaamerProContext = dataContext; _SystemAction = systemAction;
            _Acc_ServiceTypesRepository = Acc_ServiceTypesRepository;
        }

        public Task<IEnumerable<Acc_ServiceTypesVM>> GetAllServiceTypes()
        {
            var ServiceType = _Acc_ServiceTypesRepository.GetAllServiceTypes();
            return ServiceType;
        }
        public GeneralMessage SaveServiceType(Acc_ServiceTypes ServiceType, int UserId, int BranchId)
        {
            try
            {

                if (ServiceType.ServiceTypeId == 0)
                {
                    ServiceType.AddUser = UserId;
                    ServiceType.AddDate = DateTime.Now;
                    _TaamerProContext.Acc_ServiceTypes.Add(ServiceType);
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة نوع خدمة جديد";
                    _SystemAction.SaveAction("SaveServiceType", "Acc_ServiceTypesService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    var ServiceTypeUpdated = _TaamerProContext.Acc_ServiceTypes.Where(s => s.ServiceTypeId == ServiceType.ServiceTypeId).FirstOrDefault();

                    if (ServiceTypeUpdated != null)
                    {
                        ServiceTypeUpdated.NameAr = ServiceType.NameAr;
                        ServiceTypeUpdated.NameEn = ServiceType.NameEn;
                        ServiceTypeUpdated.UpdateUser = UserId;
                        ServiceTypeUpdated.UpdateDate = DateTime.Now;

                    }
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل نوع خدمة رقم " + ServiceType.ServiceTypeId;
                    _SystemAction.SaveAction("SaveServiceType", "Acc_ServiceTypesService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }

            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ النوع خدمة";
                _SystemAction.SaveAction("SaveServiceType", "Acc_ServiceTypesService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage DeleteServiceType(int ServiceTypeId, int UserId, int BranchId)
        {
            try
            {
                Acc_ServiceTypes? ServiceType = _TaamerProContext.Acc_ServiceTypes.Where(s => s.ServiceTypeId == ServiceTypeId).FirstOrDefault();
                if (ServiceType != null)
                {
                    ServiceType.IsDeleted = true;
                    ServiceType.DeleteDate = DateTime.Now;
                    ServiceType.DeleteUser = UserId;
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " حذف نوع خدمة رقم " + ServiceTypeId;
                    _SystemAction.SaveAction("DeleteServiceType", "Acc_ServiceTypesService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };

            }
            catch (Exception)
            {

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف نوع خدمة رقم " + ServiceTypeId; ;
                _SystemAction.SaveAction("DeleteServiceType", "Acc_ServiceTypesService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }
    }
}
