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

namespace TaamerProject.Service.Services
{
    public class Acc_StorehouseService: IAcc_StorehouseService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;

        private readonly IAcc_StorehouseRepository _Acc_StorehouseRepository;

        public Acc_StorehouseService(IAcc_StorehouseRepository acc_StorehouseRepository
            , TaamerProjectContext dataContext
            , ISystemAction systemAction)
        {
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _Acc_StorehouseRepository = acc_StorehouseRepository;
        }
        public Task<IEnumerable<Acc_StorehouseVM>> GetAllStorehouses(string SearchText)
        {
            var Storehouses = _Acc_StorehouseRepository.GetAllStorehouses(SearchText);
            return Storehouses;
        }


        public GeneralMessage SaveStorehouse(Acc_Storehouse Storehouse, int UserId, int BranchId)
        {
            try
            {

                if (Storehouse.StorehouseId == 0)
                {
                    Storehouse.AddUser = UserId;
                    Storehouse.AddDate = DateTime.Now;
                    Storehouse.Code = "";
                    _TaamerProContext.Acc_Storehouse.Add(Storehouse);
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = Resources.addnewitem;
                    _SystemAction.SaveAction("SaveStorehouse", "Acc_StorehouseService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };

                }
                else
                {
                    var StorehouseUpdated = _TaamerProContext.Acc_Storehouse.Where(s => s.StorehouseId == Storehouse.StorehouseId).FirstOrDefault();

                    if (StorehouseUpdated != null)
                    {
                        StorehouseUpdated.NameAr = Storehouse.NameAr;
                        StorehouseUpdated.NameEn = Storehouse.NameEn;
                        StorehouseUpdated.Code = Storehouse.Code;
                        StorehouseUpdated.Notes = Storehouse.Notes;
                        StorehouseUpdated.BranchId = Storehouse.BranchId;
                        StorehouseUpdated.UpdateUser = UserId;
                        StorehouseUpdated.UpdateDate = DateTime.Now;

                    }

                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل مستودع رقم " + Storehouse.StorehouseId;
                    _SystemAction.SaveAction("SaveStorehouse", "Acc_StorehouseService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ المستودع";
                _SystemAction.SaveAction("SaveStorehouse", "Acc_StorehouseService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage DeleteStorehouse(int StorehouseId, int UserId, int BranchId)
        {
            try
            {
                Acc_Storehouse? storehouse = _TaamerProContext.Acc_Storehouse.Where(s => s.StorehouseId == StorehouseId).FirstOrDefault();
                if (storehouse != null)
                {
                    storehouse.IsDeleted = true;
                    storehouse.DeleteDate = DateTime.Now;
                    storehouse.DeleteUser = UserId;
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " حذف مستودع رقم " + StorehouseId;
                    _SystemAction.SaveAction("DeleteStorehouse", "Acc_StorehouseService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف مستودع رقم " + StorehouseId; ;
                _SystemAction.SaveAction("DeleteStorehouse", "Acc_StorehouseService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }
    }
}
