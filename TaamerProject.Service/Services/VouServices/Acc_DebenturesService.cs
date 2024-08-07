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
    public class Acc_DebenturesService: IAcc_DebenturesService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;

        private readonly IAcc_DebenturesRepository _Acc_DebenturesRepository;

        public Acc_DebenturesService(IAcc_DebenturesRepository acc_DebenturesRepository
            , TaamerProjectContext dataContext
            , ISystemAction systemAction)
        {
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _Acc_DebenturesRepository = acc_DebenturesRepository;
        }
        public Task<IEnumerable<Acc_DebenturesVM>> GetAllDebentures(int Type, int YearId, int BranchId)
        {
            var Debentures = _Acc_DebenturesRepository.GetAllDebentures(Type, YearId, BranchId);
            return Debentures;
        }
        public async Task<int?> GenerateDebentureNumber(int Type, int BranchId, int? YearId)
        {
            return await _Acc_DebenturesRepository.GenerateNextDebentureNumber(Type, YearId, BranchId);
        }

        public GeneralMessage SaveDebenture(Acc_Debentures Debenture, int UserId, int BranchId, int? YearId)
        {
            try
            {

                if (Debenture.DebentureId == 0)
                {

                    var vouchercheck = _TaamerProContext.Acc_Debentures.Where(s => s.IsDeleted == false && s.YearId == YearId && s.Type == Debenture.Type && s.DebentureNumber == Debenture.DebentureNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        var NextInv = _Acc_DebenturesRepository.GenerateNextDebentureNumber(Debenture.Type??0, YearId, BranchId).Result;
                        var NewNextInv = string.Format("{0:000000}", NextInv);
                        Debenture.DebentureNumber = NewNextInv.ToString();
                    }

                    Debenture.BranchId = BranchId;
                    Debenture.YearId = YearId;
                    Debenture.AddUser = UserId;
                    Debenture.AddDate = DateTime.Now;
                    _TaamerProContext.Acc_Debentures.Add(Debenture);
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = Resources.addnewitem;
                    _SystemAction.SaveAction("SaveDebenture", "Acc_DebenturesService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };

                }
                else
                {
                    var DebentureUpdated = _TaamerProContext.Acc_Debentures.Where(s => s.DebentureId == Debenture.DebentureId).FirstOrDefault();

                    if (DebentureUpdated != null)
                    {
                        DebentureUpdated.Date = Debenture.Date;
                        DebentureUpdated.HijriDate = Debenture.HijriDate;
                        DebentureUpdated.Notes = Debenture.Notes;
                        DebentureUpdated.ServicesId = Debenture.ServicesId;
                        DebentureUpdated.FromStorehouseId = Debenture.FromStorehouseId;
                        DebentureUpdated.Qty = Debenture.Qty;
                        DebentureUpdated.QtyText = Debenture.QtyText;
                        DebentureUpdated.UpdateUser = UserId;
                        DebentureUpdated.UpdateDate = DateTime.Now;
                    }

                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل سند رقم " + Debenture.DebentureId;
                    _SystemAction.SaveAction("SaveDebenture", "Acc_DebenturesService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ السند";
                _SystemAction.SaveAction("SaveCategory", "Acc_DebenturesService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage DeleteDebenture(int DebentureId, int UserId, int BranchId)
        {
            try
            {
                Acc_Debentures? debenture = _TaamerProContext.Acc_Debentures.Where(s => s.DebentureId == DebentureId).FirstOrDefault();
                if (debenture != null)
                {
                    debenture.IsDeleted = true;
                    debenture.DeleteDate = DateTime.Now;
                    debenture.DeleteUser = UserId;
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " حذف سند رقم " + DebentureId;
                    _SystemAction.SaveAction("DeleteDebenture", "Acc_DebenturesService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف سند رقم " + DebentureId; ;
                _SystemAction.SaveAction("DeleteDebenture", "Acc_DebenturesService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }

    }
}
