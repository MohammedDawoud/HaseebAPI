using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;
using TaamerProject.Models.DBContext;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Service.IGeneric;
using System.Net;
using TaamerProject.Service.Interfaces;
using Haseeb.Service.LocalResources;

namespace TaamerProject.Service.Services
{
    public class ContacFilesService : IContacFilesService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly IContacFilesRepository _ContacFilesRepository;



        public ContacFilesService(TaamerProjectContext dataContext, ISystemAction systemAction, IContacFilesRepository contacFilesRepository)
        {
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _ContacFilesRepository = contacFilesRepository;
        }
        public async Task<IEnumerable<ContacFilesVM>> GetAllFiles(int? OutInBoxId)
        {
            var Files = await _ContacFilesRepository.GetAllFiles(OutInBoxId);
            return Files;
        }
        public async Task<IEnumerable<ContacFilesVM>> GetAllContacFiles()
        {
            var Files =await _ContacFilesRepository.GetAllContacFiles();
            return Files;
        }
        public GeneralMessage SaveContacFile(ContacFiles ContacFiles, int UserId, int BranchId)
        {
            try
            {
                if (ContacFiles.FileId == 0)
                {
                    ContacFiles.UploadDate = DateTime.Now;
                    ContacFiles.IsCertified = true;
                    ContacFiles.UserId = UserId;
                    ContacFiles.AddUser = UserId;
                    ContacFiles.BranchId = BranchId;
                    ContacFiles.AddDate = DateTime.Now;
                    _TaamerProContext.ContacFiles.Add(ContacFiles);
                }
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "اضافة ملف اتصال جديد";
                _SystemAction.SaveAction("SaveContacFile", "ContacFilesService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ ملف الاتصال";
                _SystemAction.SaveAction("SaveContacFile", "ContacFilesService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage {StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage DeleteContacFile(int FileId, int UserId, int BranchId)
        {
            try
            {
                ContacFiles ContacFiles = _TaamerProContext.ContacFiles.Where(x=>x.FileId==FileId).FirstOrDefault();
                ContacFiles.IsDeleted = true;
                ContacFiles.DeleteDate = DateTime.Now;
                ContacFiles.DeleteUser = UserId;
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " حذف ملف اتصال رقم " + FileId;
                _SystemAction.SaveAction("DeleteContacFile", "ContacFilesService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage {StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف ملف اتصال رقم " + FileId; ;
                _SystemAction.SaveAction("DeleteContacFile", "ContacFilesService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage {StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }
        public async Task<IEnumerable<ContacFilesVM>> GetAllFilesByParams(int? ArchiveFileId, int? Type, int? OutInType, int BranchId)
        {
            return await _ContacFilesRepository.GetAllFilesByParams(ArchiveFileId, Type, OutInType, BranchId);
        }
    }
}
