using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;

namespace TaamerProject.Service.Interfaces
{
    public interface IProSettingDetailsService  
    {
        Task<ProSettingDetailsVM> CheckProSettingData(int ProjectTypeId, int ProjectSubTypeId, int BranchId);
        Task<ProSettingDetailsVM> GetProjectSettingsDetailsIFExist(int ProjectId, int BranchId);

        Task<ProSettingDetailsVM> CheckProSettingData2( int? ProjectSubTypeId, int BranchId);

        GeneralMessage DeleteProjectSettingsDetails(int SettingId, int UserId, int BranchId);

        GeneralMessage SaveProSettingData(ProSettingDetails proSettingDetails, int BranchId, int UserId);
        GeneralMessage EditProSettingsDetails(ProSettingDetails proSettingDetails, int BranchId, int UserId);

        Task<IEnumerable<ProSettingDetailsVM>> FillProSettingNo();
        int GenerateNextProSettingNumber();
        ProSettingDetailsVM GetProSettingById(int ProSettingId);
    }
}
