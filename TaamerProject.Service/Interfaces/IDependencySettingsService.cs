using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;

namespace TaamerProject.Service.Interfaces
{
    public interface IDependencySettingsService  
    {
        Task<IEnumerable<DependencySettingsVM>> GetAllDependencySettings(int? SuccessorId, int BranchId);
        GeneralMessage SaveDependencySettings(int ProjectSubTypeId, List<DependencySettings> TaskLink, List<NodeLocations> NodeLocList, int UserId, int BranchId);

        GeneralMessage DeleteDependencySettings(int DependencyId,int UserId,int BranchId);
        TasksNodeVM GetTasksNodeByProSubTypeId(int ProjSubTypeId, int BranchId, int UserId);
        List<AccountTreeVM> GetProjSubTypeIdSettingTree(int ProjectSubTypeId, int BranchId);
        GeneralMessage TransferSetting(int ProjSubTypeFromId, int ProjSubTypeToId, int BranchId, int UserId);
        GeneralMessage TransferSettingNEW(int ProjSubTypeFromId, int ProjSubTypeToId, int BranchId, int UserId); 

    }
}
