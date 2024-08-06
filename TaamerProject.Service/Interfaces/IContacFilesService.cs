using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;

namespace TaamerProject.Service.Interfaces
{
    public interface IContacFilesService
    {
        Task<IEnumerable<ContacFilesVM>> GetAllFiles(int? OutInBoxId);
        Task<IEnumerable<ContacFilesVM>> GetAllContacFiles();
        GeneralMessage SaveContacFile(ContacFiles ContacFiles, int UserId, int BranchId);
        GeneralMessage DeleteContacFile(int FileId, int UserId, int BranchId);
        Task<IEnumerable<ContacFilesVM>> GetAllFilesByParams(int? ArchiveFileId, int? Type, int? OutInType, int BranchId);
    }
}
